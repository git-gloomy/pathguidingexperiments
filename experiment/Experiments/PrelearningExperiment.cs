using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using OpenPGL.NET;
using SeeSharp;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class PrelearningExperiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;
    int maxTime = int.MaxValue;
    Field guidingField;
    IntegratorSettings settings = new() {
        IncludeDebugVisualizations = true,
        LearnInterval = 32,
        EnableGuidingFieldLearning = false,
        LearnUntil = 32,
        FixProbabilityUntil = 32,
    };

    public PrelearningExperiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
    }

    public override void OnStartScene(Scene scene, string dir, int minDepth, int maxDepth)
    {
        GuidedPathTracer pt = new() {
            TotalSpp = 100,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            MinDepth = minDepth,
            MaxDepth = maxDepth
        };
        pt.Render(scene);
        this.guidingField = pt.GuidingField;
    }

    // Only one method should be commented in at a time, as all use the same guiding field
    public override List<Method> MakeMethods() => new() {
        new Method("PathTracingPL", new PathTracer() {
            TotalSpp = numSamples - 32,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            BaseSeed = 12345,
        }),
        new Method("PathGuidingPL", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = numSamples - 32,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            Settings = new() { // Settings are configured in a way to disable learning
                IncludeDebugVisualizations = true,
                LearnInterval = 32,
                EnableGuidingFieldLearning = false,
                LearnUntil = 0,
                FixProbabilityUntil = numSamples,
            },
            BaseSeed = 12345,
        }),
        new Method("PrelearnedRootAdaptive", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            Settings = settings,
        }),
        new Method("PrelearnedKullbackLeibler", new KullbackLeiblerGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            Settings = settings,
        }),
        new Method("PrelearnedSecondMoment", new SecondMomentGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            Settings = settings,
            BaseSeed = 12345,
        })
    };
}
