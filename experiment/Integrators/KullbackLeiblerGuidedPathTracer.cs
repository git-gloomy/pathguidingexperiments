using System.Numerics;
using GuidedPathTracerExperiments.ProbabilityTrees;

namespace GuidedPathTracerExperiments.Integrators {

    // Based on Path guiding in production by Vorba et al.
    // (see: https://dl.acm.org/doi/10.1145/3305366.3328091)
    public class KullbackLeiblerGuidedPathTracer : LearningGuidedPathTracer {
        protected override void OnPrepareRender() {
            Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
            Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
            probabilityTree = new KullbackLeiblerProbabilityTree(
                lower, upper, 
                Settings.TreeSplitMargin
            );

            base.OnPrepareRender();
        }
    }
}