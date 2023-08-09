using System;
using System.Numerics;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

// Based on Optimal Deterministic Mixture Sampling by Sbert et al. (see: https://diglib.eg.org/handle/10.2312/egs20191018)
public class RootAdaptiveTree : AccumulatingTree {

    public RootAdaptiveTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int expectedSamples) 
        : base(probability, lowerBounds, upperBounds, splitMargin, expectedSamples) {
        // Nothing to do here
    }

    protected override void InitializeChildren() {
        Vector3 lower, upper;
        for (int idx = 0; idx < 8; idx++) {
            (lower, upper) = GetChildBoundingBox(idx);    
            childNodes[idx] = new RootAdaptiveTree(
                guidingProbability, 
                lower, upper, 
                splitMargin,
                samples.Count / 8);
        } 
    }

    protected override void LearnProbability() {
        // Minimizes the variance by using a Newton algorithm
        float firstDeriv = 0.0f;
        float secondDeriv = 0.0f;
        
        foreach (var sample in samples) {
            float estimate = sample.RadianceEstimate.Average;
            if (estimate == 0.0f || sample.GuidePdf == 0.0f) continue;

            // Compute first and second derivative of a single sample
            float estimateSquared = estimate * estimate;
            float diff = sample.BsdfPdf - sample.GuidePdf;
            float samplePdfCube = MathF.Pow(sample.SamplePdf, 3.0f);

            float firstDerivSample = estimateSquared * diff;
            float secondDerivSample = firstDerivSample * diff;

            firstDeriv += firstDerivSample / samplePdfCube;
            secondDeriv += secondDerivSample / (samplePdfCube * sample.SamplePdf);
        }            
        
        // Average over all samples
        float div = 1.0f / samples.Count;
        firstDeriv *= div;
        secondDeriv *= div;
        
        if(!(secondDeriv == 0 || float.IsNaN(firstDeriv) || float.IsNaN(secondDeriv))) {
            // As suggested by the paper, we clamp the probability between 0.1 and 0.9
            guidingProbability = float.Clamp(guidingProbability - (firstDeriv / secondDeriv), 0.1f, 0.9f);
        }
        samples.Clear();
    }
}