using System.Numerics;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

// Based on Efficiency-aware multiple importance sampling for bidirectional rendering algorithms
// by Grittmann et al. (see: https://dl.acm.org/doi/abs/10.1145/3528223.3530126)
public class SecondMomentTree : AccumulatingTree {
    /// <summary>
    /// Possible values of the guiding probability to consider during learning
    /// </summary>
    static readonly float[] strategies = {.1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .9f};

    public SecondMomentTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int expectedSamples) 
        : base(probability, lowerBounds, upperBounds, splitMargin, expectedSamples) {
        // Nothing to do here
    }

    protected override void InitializeChildren() {
        Vector3 lower, upper;
        for (int idx = 0; idx < 8; idx++) {
            (lower, upper) = GetChildBoundingBox(idx);    
            childNodes[idx] = new SecondMomentTree(
                guidingProbability, 
                lower, upper, 
                splitMargin,
                samples.Count / 8);
        } 
    }

    protected override void LearnProbability() {
        // Implementation of Algorithm 1 from the paper
        // Some computations are reordered to allow reusage of interim results
        if (samples.Count == 0) return;
        float[] secondMoments = new float[strategies.Length];
        float bsdfProbability = 1.0f - guidingProbability;

        foreach (var sample in samples) {
            float estimate = sample.RadianceEstimate.Average;
            if (estimate == 0.0f || sample.GuidePdf == 0.0f) continue;

            // We use a guiding probability of 0.5 as our proxy
            float combinedPdfs = sample.GuidePdf * 0.5f + sample.BsdfPdf * 0.5f;
            float weightProxyBsdf = 0.5f * sample.BsdfPdf / combinedPdfs;
            float weightProxyGuide = 0.5f * sample.GuidePdf / combinedPdfs;

            float correctionNumerator = bsdfProbability * weightProxyBsdf + guidingProbability * weightProxyGuide;
            float estimateWeighted = estimate / sample.SamplePdf;

            for (int i = 0; i < strategies.Length; i++) {
                float correction = correctionNumerator / ((1.0f - strategies[i]) * weightProxyBsdf + strategies[i] * weightProxyGuide);
                secondMoments[i] += estimateWeighted * estimateWeighted * correction;
            }
        }
            
        // Select the guiding probability that minimizes the second moment
        int selectedStrategy = 0;
        float minimumSecondMoment = float.PositiveInfinity;
        for (int i = 0; i < strategies.Length; i++) {
            if (secondMoments[i] < minimumSecondMoment) {
                minimumSecondMoment = secondMoments[i];
                selectedStrategy = i;
            }
        } 

        guidingProbability = strategies[selectedStrategy];
        samples.Clear();
    }
}