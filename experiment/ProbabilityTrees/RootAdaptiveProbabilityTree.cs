using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

public class RootAdaptiveProbabilityTree : GuidingProbabilityTree {
    class RootAdaptiveSampleData {
        public Vector3 Position { get; set; }
        public float FirstDeriv { get; set; }
        public float SecondDeriv { get; set; }
        public RgbColor RadianceEstimate { get; set; }
    }
    
    float guidingProbability;
    List<RootAdaptiveSampleData> samples = new();

    public RootAdaptiveProbabilityTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
    }

    void AddSampleData(RootAdaptiveSampleData sample) {
        if (this.isLeaf) {
            lock(samples) {
                samples.Add(sample);
            }
        } else {
            ((RootAdaptiveProbabilityTree) childNodes[getChildIdx(sample.Position)])
                .AddSampleData(sample);
        }
    }

    public override void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        _ = samplePdf;
        if (this.isLeaf) {
            float estimate = radianceEstimate.Average;
            float estimateSquared = estimate * estimate;
            float diff = bsdfPdf - guidePdf;

            float firstDeriv = estimateSquared * diff;
            firstDeriv /= MathF.Pow(samplePdf, 3.0f);
            
            float secondDeriv = estimateSquared * MathF.Pow(diff, 2.0f);
            secondDeriv /= MathF.Pow(samplePdf, 4.0f);
            
            lock(samples) {
                samples.Add(new RootAdaptiveSampleData() {
                    Position = position,
                    FirstDeriv = firstDeriv,
                    SecondDeriv = secondDeriv,
                    RadianceEstimate = radianceEstimate
                });
            }
        } else {
            ((RootAdaptiveProbabilityTree) childNodes[getChildIdx(position)])
                .AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimate);
        }
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[getChildIdx(point)].GetProbability(point);
    }

    public void LearnProbabilities() {
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((RootAdaptiveProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else if (samples.Count > splitMargin) {
            Vector3 lower, upper;
            for (int idx = 0; idx < 8; idx++) {
                (lower, upper) = GetChildBoundingBox(idx);    
                childNodes[idx] = new RootAdaptiveProbabilityTree(
                    guidingProbability, 
                    lower, upper, 
                    splitMargin);
            } 

            // Distribute data to the correct child nodes
            foreach (var sample in samples) {
                int idx = getChildIdx(sample.Position);
                ((RootAdaptiveProbabilityTree) childNodes[idx]).AddSampleData(sample);
            }

            // Remove leaf properties from current node
            samples.Clear();
            isLeaf = false;

            // Calculate probabilities for each child node
            Parallel.For(0, 8, idx => {
                ((RootAdaptiveProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            float firstDeriv = 0.0f;
            float secondDeriv = 0.0f;
            
            float div = (1.0f / (float) samples.Count);
            avgColor = new(0.0f);
            foreach (var sample in samples) {
                firstDeriv += sample.FirstDeriv;
                secondDeriv += sample.SecondDeriv;
                avgColor += sample.RadianceEstimate;
            }            
            
            firstDeriv *= div;
            secondDeriv *= div;
            avgColor *= div;
            
            if(!(secondDeriv == 0 || float.IsNaN(firstDeriv) || float.IsNaN(secondDeriv))) {
                guidingProbability = float.Clamp(guidingProbability - (firstDeriv / secondDeriv), 0.1f, 0.9f);
            }
            samples.Clear();
        }
    }
}