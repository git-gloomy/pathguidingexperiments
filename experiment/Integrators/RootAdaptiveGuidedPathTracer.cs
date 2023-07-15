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
        /// <summary>
        /// Determines after how many iterations the guiding probabilities are reevaluated
        /// </summary>
        public int ProbabilityLearningInterval { get; set; }

        /// <summary>
        /// If true, discards all samples for learning except the ones in the iteration used for
        /// learning
        /// </summary>
        public bool SingleIterationLearning { get; set; }

        //SquarerootWeightedLayer sqrtWeightedRender;

        protected override void OnPrepareRender() {
            Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
            Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
            probabilityTree = new RootAdaptiveProbabilityTree(
                0.5f, 
                lower, upper, 
                ProbabilityTreeSplitMargin,
                scene.FrameBuffer.Width * scene.FrameBuffer.Height * (MaxDepth + 1)
            );

            //sqrtWeightedRender = new(ProbabilityLearningInterval);
            //scene.FrameBuffer.AddLayer("sqrtWeightedSamples", sqrtWeightedRender);

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
                ((RootAdaptiveProbabilityTree) probabilityTree).LearnProbabilities();
            }

            GuidingEnabled = true;
        }

        protected override void RenderPixel(uint row, uint col, RNG rng) {
            // Sample a ray from the camera
            var offset = rng.NextFloat2D();
            var pixel = new Vector2(col, row) + offset;
            Ray primaryRay = scene.Camera.GenerateRay(pixel, rng).Ray;

            var state = MakePathState();
            state.Pixel = new((int)col, (int)row);
            state.Rng = rng;
            state.Throughput = RgbColor.White;
            state.Depth = 1;

            OnStartPath(state);
            var estimate = EstimateIncidentRadiance(primaryRay, state);
            OnFinishedPath(estimate, state);

            scene.FrameBuffer.Splat(state.Pixel, estimate.Outgoing);
            //sqrtWeightedRender.Splat(state.Pixel, estimate.Outgoing);
        }
    }
}