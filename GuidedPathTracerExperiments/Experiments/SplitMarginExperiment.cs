using System.Collections.Generic;
using GuidedPathTracerExperiments.Integrators;

namespace GuidedPathTracerExperiments;

public class SplitMarginExperiment : SeeSharp.Experiments.Experiment {
    int numSamples = 16;

    public SplitMarginExperiment(int numSamples) {
        this.numSamples = numSamples;
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
            
            methods.Add(new Method("SplitMargin/RootAdaptive" + margin, new RootAdaptiveGuidedPathTracer() {
                TotalSpp = numSamples,
                Settings = settings
            }));
            methods.Add(new Method("SplitMargin/KullbackLeibler" + margin, new KullbackLeiblerGuidedPathTracer() {
                TotalSpp = numSamples,
                Settings = settings
            }));
            methods.Add(new Method("SplitMargin/SecondMoment" + margin, new SecondMomentGuidedPathTracer() {
                TotalSpp = numSamples,
                Settings = settings
            }));
        }

        return methods;
    }
}
