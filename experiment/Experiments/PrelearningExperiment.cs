using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using OpenPGL.NET;
using SeeSharp;

namespace GuidedPathTracerExperiments;

public class PrelearningExperiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;
    int maxTime = int.MaxValue;
    Field guidingField;

    public PrelearningExperiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
    }

    public override void OnStartScene(Scene scene, string dir, int minDepth, int maxDepth)
    {
        GuidedPathTracer pt = new() {
            TotalSpp = 128,
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
        new Method("PrelearnedRootAdaptive", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            IncludeDebugVisualizations = true,
            ProbabilityLearningInterval = 32,
            ProbabilityTreeSplitMargin = 10000,
            EnableGuidingFieldLearning = false,
            LearnUntil = 32,
            FixProbabilityUntil = 32,
        }),
        new Method("PrelearnedKullbackLeibler", new KullbackLeiblerGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            IncludeDebugVisualizations = true,
            ProbabilityTreeSplitMargin = 10000,
            EnableGuidingFieldLearning = false,
            LearnUntil = 32,
            FixProbabilityUntil = 32,
        }),
        new Method("PrelearnedSecondMoment", new SecondMomentGuidedPathTracer() {
            TotalSpp = numSamples,
            MaximumRenderTimeMs = maxTime,
            NumShadowRays = 1,
            GuidingField = guidingField,
            IncludeDebugVisualizations = true,
            ProbabilityLearningInterval = 32,
            ProbabilityTreeSplitMargin = 10000,
            EnableGuidingFieldLearning = false,
            LearnUntil = 32,
            FixProbabilityUntil = 32,
        })
    };
}
