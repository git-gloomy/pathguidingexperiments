using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class ComparisonExperiment : SeeSharp.Experiments.Experiment {
    readonly int numSamples = 128;
    readonly IntegratorSettings settings = new() {
        IncludeDebugVisualizations = true,
        LearnInterval = 1,
        TreeSplitMargin = 2000,
    };


    public ComparisonExperiment(int numSamples) {
        this.numSamples = numSamples;
    }

    public override List<Method> MakeMethods() => new() {
        new("Comparison/PathTracer", new PathTracer() {
            TotalSpp = numSamples,
        }),
        new("Comparison/GuidedPathTracer", new GuidedPathTracer() {
            TotalSpp = numSamples,
        }),
        new("Comparison/RootAdaptiveGuidedPathTracer", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = numSamples,
            Settings = settings,
        }),
        new("Comparison/KullbackLeiblerGuidedPathTracer", new KullbackLeiblerGuidedPathTracer() {
            TotalSpp = numSamples,
            Settings = settings,
        }),
        new("Comparison/SecondMomentGuidedPathTracer", new SecondMomentGuidedPathTracer() {
            TotalSpp = numSamples,
            Settings = settings,
        })
    };
}
