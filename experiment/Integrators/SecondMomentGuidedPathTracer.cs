using System.Numerics;
using SimpleImageIO;
using TinyEmbree;
using GuidedPathTracerExperiments.ProbabilityTrees;
using SeeSharp.Sampling;

namespace GuidedPathTracerExperiments.Integrators {

    // Based on Efficiency-aware multiple importance sampling for bidirectional rendering algorithms
    // by Grittmann et al. (see: https://dl.acm.org/doi/abs/10.1145/3528223.3530126)
    public class SecondMomentGuidedPathTracer : LearningGuidedPathTracer {
        /// <summary>
        /// Determines after how many iterations the guiding probabilities are reevaluated
        /// </summary>
        public int ProbabilityLearningInterval { get; set; }

        /// <summary>
        /// If true, discards all samples for learning except the ones in the iteration used for
        /// learning
        /// </summary>
        public bool SingleIterationLearning { get; set; }

        protected override void OnPrepareRender() {
            Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
            Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
            probabilityTree = new SecondMomentProbabilityTree(
                0.5f, 
                lower, upper, 
                ProbabilityTreeSplitMargin,
                scene.FrameBuffer.Width * scene.FrameBuffer.Height * (MaxDepth + 1)
            );

            base.OnPrepareRender();
        }

        protected override void OnPreIteration(uint iterIdx)
        {
            int iterationsSinceUpdate = ((int) iterIdx + 1) % ProbabilityLearningInterval;
            if(iterationsSinceUpdate == 0 || !SingleIterationLearning) {
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
            int iterationsSinceUpdate = ((int) iterIdx + 1) % ProbabilityLearningInterval;
            if(iterationsSinceUpdate == 0 && iterIdx + 1 != TotalSpp && enableProbabilityLearning) {
                ((SecondMomentProbabilityTree) probabilityTree).LearnProbabilities();
            }

            GuidingEnabled = true;
        }
    }
}