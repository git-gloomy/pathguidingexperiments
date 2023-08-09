using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

// Based on Efficiency-aware multiple importance sampling for bidirectional rendering algorithms
// by Grittmann et al. (see: https://dl.acm.org/doi/abs/10.1145/3528223.3530126)
public class SecondMomentProbabilityTree : GuidingProbabilityTree {
    class SecondMomentSampleData {
        public Vector3 Position { get; set; }
        public float GuidePdf { get; set; }
        public float BsdfPdf { get; set; }
        public float SamplePdf { get; set; }
        public RgbColor RadianceEstimate { get; set; }
    }

    /// <summary>
    /// Possible values of the guiding probability to consider during learning
    /// </summary>
    static readonly float[] strategies = {.1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .9f};

    float guidingProbability;
    List<SecondMomentSampleData> samples;

    public SecondMomentProbabilityTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int expectedSamples) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
        this.samples = new(expectedSamples);
    }

    void AddSampleData(SecondMomentSampleData sample) {
        if (this.isLeaf) {
            lock(samples) {
                samples.Add(sample);
            }
        } else {
            ((SecondMomentProbabilityTree) childNodes[GetChildIdx(sample.Position)])
                .AddSampleData(sample);
        }
    }

    public override void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        AddSampleData(new SecondMomentSampleData() {
                    Position = position,
                    GuidePdf = guidePdf,
                    BsdfPdf = bsdfPdf,
                    SamplePdf = samplePdf,
                    RadianceEstimate = radianceEstimate,
                });
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[GetChildIdx(point)].GetProbability(point);
    }

    public void LearnProbabilities() {
        if (isLeaf && samples.Count > SplitMargin) {
            Vector3 lower, upper;
            for (int idx = 0; idx < 8; idx++) {
                (lower, upper) = GetChildBoundingBox(idx);    
                childNodes[idx] = new SecondMomentProbabilityTree(
                    guidingProbability, 
                    lower, upper, 
                    SplitMargin,
                    samples.Count / 8);
            } 

            // Distribute data to the correct child nodes
            foreach (var sample in samples) {
                int idx = GetChildIdx(sample.Position);
                ((SecondMomentProbabilityTree) childNodes[idx]).AddSampleData(sample);
            }

            // Remove leaf properties from current node
            samples = null;
            isLeaf = false;
        }
        
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((SecondMomentProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            // Implementation of Algorithm 1 from the paper
            // Some computations are reordered to allow reusage of interim results
            float count = samples.Count;
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

                // Balance heuristic for both methods
                float balanceBsdf = bsdfProbability * sample.BsdfPdf / sample.SamplePdf;
                float balanceGuide = guidingProbability * sample.GuidePdf / sample.SamplePdf;

                // Resulting estimates when using either BSDF sampling vs. path guiding
                float estimateBsdf = balanceBsdf * estimate / (count * sample.BsdfPdf);
                float estimateGuide = balanceGuide * estimate / (count * sample.GuidePdf);

                for (int i = 0; i < strategies.Length; i++) {
                    float correction = correctionNumerator / ((1.0f - strategies[i]) * weightProxyBsdf + strategies[i] * weightProxyGuide);
                    secondMoments[i] += estimateBsdf * estimateBsdf * correction;
                    secondMoments[i] += estimateGuide * estimateGuide * correction;
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
}