using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;
using OpenPGL.NET;
using SeeSharp;
using SeeSharp.Integrators;

namespace GuidedPathTracerExperiments;

public class PrelearningExperiment : SeeSharp.Experiments.Experiment {
    readonly int renderSamples = 50;
    readonly int learningSamples = 25;
    readonly int maxTime = int.MaxValue;
    
    Field guidingField;
    readonly IntegratorSettings settingsLearningEnabled;
    readonly IntegratorSettings settingsLearningDisabled;

    public PrelearningExperiment(int learningSamples, int renderSamples, int maxTime = int.MaxValue) {
        this.learningSamples = learningSamples;
        this.renderSamples = renderSamples;
        this.maxTime = maxTime;

        this.settingsLearningEnabled = new() {
            IncludeDebugVisualizations = true,
            DebugVisualizationInterval = learningSamples + renderSamples - 1,   // Only visualize once
            LearnInterval = learningSamples,
            GuidingFieldLearningEnabled = false,
            LearnUntil = learningSamples,
            FixProbabilityUntil = learningSamples,
            RNGOffset = 12345,
        };

        this.settingsLearningDisabled = new(settingsLearningEnabled) { 
            DebugVisualizationInterval = renderSamples - 1,
            LearnUntil = 0,
            FixProbabilityUntil = renderSamples,
            RNGOffset = (uint) (12345 + learningSamples),
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

    public override List<Method> MakeMethods() => new() {
        new Method("Prelearned/PathTracing", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new(settingsLearningDisabled) { 
                InitialGuidingProbability = 0,
            },
        }),
        new Method("Prelearned/PathGuiding02", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new(settingsLearningDisabled) { 
                InitialGuidingProbability = 0.2f,
            },
        }),
        new Method("Prelearned/PathGuiding05", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new(settingsLearningDisabled) { 
                InitialGuidingProbability = 0.5f,
            },
        }),
        new Method("Prelearned/PathGuiding08", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = renderSamples,
            GuidingField = guidingField,
            Settings = new(settingsLearningDisabled) { 
                InitialGuidingProbability = 0.8f,
            },
        }),
        new Method("Prelearned/RootAdaptive", new RootAdaptiveGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settingsLearningEnabled,
        }),
        new Method("Prelearned/SecondMoment", new SecondMomentGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settingsLearningEnabled,
        }),
        new Method("Prelearned/KullbackLeibler", new KullbackLeiblerGuidedPathTracer() {
            TotalSpp = learningSamples + renderSamples,
            GuidingField = guidingField,
            Settings = settingsLearningEnabled,
        }),
    };
}
