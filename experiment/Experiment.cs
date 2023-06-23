using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class Experiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;
    int maxTime = int.MaxValue;

    public Experiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
    }

    public override List<Method> MakeMethods() => new() {
        //new("PathTracer", new PathTracer() {
        //    TotalSpp = numSamples,
        //    MaximumRenderTimeMs = maxTime,
        //    NumShadowRays = 1,
        //}),
        //new("GuidedPathTracer", new GuidedPathTracer() {
        //    TotalSpp = numSamples,
        //    MaximumRenderTimeMs = maxTime,
        //    NumShadowRays = 1,
        //}),
        new("RootAdaptivePathTracer", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            ProbabilityLearningInterval = 32,
            InitialGuidingProbability = 0.5f,
            ProbabilityTreeSplitMargin = 10000,
        })
    };
}
