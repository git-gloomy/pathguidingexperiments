using System.Numerics;
using SimpleImageIO;
using TinyEmbree;
using GuidedPathTracerExperiments.ProbabilityTrees;
using SeeSharp.Sampling;

namespace GuidedPathTracerExperiments.Integrators {

    // Based on Efficiency-aware multiple importance sampling for bidirectional rendering algorithms
    // by Grittmann et al. (see: https://dl.acm.org/doi/abs/10.1145/3528223.3530126)
    public class SecondMomentGuidedPathTracer : LearningGuidedPathTracer {
        protected override void OnPrepareRender() {
            Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
            Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
            
            if (probabilityTree == null)
                probabilityTree = new SecondMomentProbabilityTree(
                    Settings.InitialGuidingProbability, 
                    lower, upper, 
                    Settings.TreeSplitMargin,
                    scene.FrameBuffer.Width * scene.FrameBuffer.Height * (MaxDepth + 1)
                );

            base.OnPrepareRender();
        }

        protected override void OnPostIteration(uint iterIdx) {
            // Update mixture ratio every ProbabilityLearningInterval iterations
            int iterationsSinceUpdate = ((int) iterIdx + 1) % Settings.LearnInterval;
            if(iterationsSinceUpdate == 0 && iterIdx + 1 != TotalSpp && enableProbabilityLearning) {
                ((SecondMomentProbabilityTree) probabilityTree).LearnProbabilities();
            }
            base.OnPostIteration(iterIdx);
        }
    }
}