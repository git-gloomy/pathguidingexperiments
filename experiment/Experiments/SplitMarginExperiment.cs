using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;

namespace GuidedPathTracerExperiments;

public class SplitMarginExperiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;
    int maxTime = int.MaxValue;

    public SplitMarginExperiment(int numSamples, int maxTime = int.MaxValue) {
        this.numSamples = numSamples;
        this.maxTime = maxTime;
    }

    public override List<Method> MakeMethods() {
        List<Method> methods = new();
        int[] splitMargins = { 250, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000 };

        

        foreach (int margin in splitMargins) {
            IntegratorSettings settings = new() {
                IncludeDebugVisualizations = true,
                LearnInterval = 32,
                TreeSplitMargin = margin
            };
            
            methods.Add(new Method("RootAdaptive" + margin, new RootAdaptiveGuidedPathTracer() {
                TotalSpp = numSamples,
                MaximumRenderTimeMs = maxTime,
                NumShadowRays = 1,
                Settings = settings
            }));
            methods.Add(new Method("KullbackLeibler" + margin, new KullbackLeiblerGuidedPathTracer() {
                TotalSpp = numSamples,
                MaximumRenderTimeMs = maxTime,
                NumShadowRays = 1,
                Settings = settings
            }));
            methods.Add(new Method("SecondMoment" + margin, new SecondMomentGuidedPathTracer() {
                TotalSpp = numSamples,
                MaximumRenderTimeMs = maxTime,
                NumShadowRays = 1,
                Settings = settings
            }));
        }

        return methods;
    }
}
