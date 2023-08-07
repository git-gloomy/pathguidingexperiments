using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using OpenPGL.NET;
using SeeSharp;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class PrelearningExperiment : SeeSharp.Experiments.Experiment {
    int renderSamples = 50;
    int learningSamples = 25;
    int maxTime = int.MaxValue;
    Field guidingField;
    IntegratorSettings settings;

    public PrelearningExperiment(int learningSamples, int renderSamples, int maxTime = int.MaxValue) {
        this.learningSamples = learningSamples;
        this.renderSamples = renderSamples;
        this.maxTime = maxTime;
        this.settings = new() {
            IncludeDebugVisualizations = true,
            LearnInterval = learningSamples,
            EnableGuidingFieldLearning = false,
            LearnUntil = learningSamples,
            FixProbabilityUntil = learningSamples,
        };
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
        new Method("Prelearned/PathTracing", new PathTracer() {
            TotalSpp = renderSamples,
        }),
        new Method("Prelearned/PathGuiding", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new() { // Settings are configured in a way to disable learning
                IncludeDebugVisualizations = true,
                LearnInterval = learningSamples,
                EnableGuidingFieldLearning = false,
                LearnUntil = 0,
                FixProbabilityUntil = renderSamples,
            },
        }),
        new Method("Prelearned/PathGuiding02", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new() { // Settings are configured in a way to disable learning
                IncludeDebugVisualizations = true,
                LearnInterval = learningSamples,
                EnableGuidingFieldLearning = false,
                LearnUntil = 0,
                FixProbabilityUntil = renderSamples,
                FixedProbability = 0.2f,
            },
        }),
        new Method("Prelearned/PathGuiding08", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new() { // Settings are configured in a way to disable learning
                IncludeDebugVisualizations = true,
                LearnInterval = learningSamples,
                EnableGuidingFieldLearning = false,
                LearnUntil = 0,
                FixProbabilityUntil = renderSamples,
                FixedProbability = 0.8f,
            },
        }),
        new Method("Prelearned/RootAdaptive", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settings,
        }),
        new Method("Prelearned/SecondMoment", new SecondMomentGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settings,
        }),
        new Method("Prelearned/KullbackLeibler", new KullbackLeiblerGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settings,
        }),
    };
}
