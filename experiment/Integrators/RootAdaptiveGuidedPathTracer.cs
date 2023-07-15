using System.Numerics;
using SimpleImageIO;
using TinyEmbree;
using GuidedPathTracerExperiments.ProbabilityTrees;
using SeeSharp.Sampling;
using OpenPGL.NET;

namespace GuidedPathTracerExperiments.Integrators {

    // Based on Optimal Deterministic Mixture Sampling by Sbert et al.
    // (see: https://diglib.eg.org/handle/10.2312/egs20191018)
    public class RootAdaptiveGuidedPathTracer : LearningGuidedPathTracer {
        protected override void OnPrepareRender() {
            Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
            Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
            probabilityTree = new RootAdaptiveProbabilityTree(
                Settings.InitialGuidingProbability, 
                lower, upper, 
                Settings.TreeSplitMargin,
                scene.FrameBuffer.Width * scene.FrameBuffer.Height * (MaxDepth + 1)
            );

            base.OnPrepareRender();
        }

        protected override void OnPreIteration(uint iterIdx)
        {
            int iterationsSinceUpdate = ((int) iterIdx + 1) % Settings.LearnInterval;
            if(iterationsSinceUpdate == 0 || !Settings.SingleIterationLearning) {
                enableProbabilityLearning = true;
            } else {
                enableProbabilityLearning = false;
            }
            base.OnPreIteration(iterIdx);
        }

        protected override void OnPostIteration(uint iterIdx) {
            GuidingField.Update(sampleStorage, 1);
            sampleStorage.Clear();

            // Update mixture ratio every ProbabilityLearningInterval iterations
            int iterationsSinceUpdate = ((int) iterIdx + 1) % Settings.LearnInterval;
            if(iterationsSinceUpdate == 0 && iterIdx + 1 != TotalSpp && enableProbabilityLearning) {
                ((RootAdaptiveProbabilityTree) probabilityTree).LearnProbabilities();
            }

            GuidingEnabled = true;
        }
    }
}