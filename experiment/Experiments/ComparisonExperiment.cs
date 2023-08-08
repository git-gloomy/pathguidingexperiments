using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class ComparisonExperiment : SeeSharp.Experiments.Experiment {
    int numSamples = 128;
    int maxTime = int.MaxValue;
    IntegratorSettings settings = new() {
        IncludeDebugVisualizations = true,
        LearnInterval = 1,
        TreeSplitMargin = 2000,
    };


    public Experiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
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
