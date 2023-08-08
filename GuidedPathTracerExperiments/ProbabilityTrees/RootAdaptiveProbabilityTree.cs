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
    List<RootAdaptiveSampleData> samples;

    public RootAdaptiveProbabilityTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int estimatedSamples) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
        this.samples = new(estimatedSamples);
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
        float estimate = radianceEstimate.Average;
        if (estimate == 0.0f) {
            AddSampleData(new RootAdaptiveSampleData() {
                Position = position,
                FirstDeriv = 0.0f,
                SecondDeriv = 0.0f,
                RadianceEstimate = radianceEstimate
            });
            return;
        }

        float estimateSquared = estimate * estimate;
        float diff = bsdfPdf - guidePdf;
        float samplePdfCube = MathF.Pow(samplePdf, 3.0f);

        float firstDeriv = estimateSquared * diff;
        float secondDeriv = firstDeriv * diff;
        firstDeriv /= samplePdfCube;
        secondDeriv /= samplePdfCube * samplePdf;
        
        AddSampleData(new RootAdaptiveSampleData() {
            Position = position,
            FirstDeriv = firstDeriv,
            SecondDeriv = secondDeriv,
            RadianceEstimate = radianceEstimate
        });
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[getChildIdx(point)].GetProbability(point);
    }

    public void LearnProbabilities() {
        if (isLeaf && samples.Count > splitMargin) {
            Vector3 lower, upper;
            for (int idx = 0; idx < 8; idx++) {
                (lower, upper) = GetChildBoundingBox(idx);    
                childNodes[idx] = new RootAdaptiveProbabilityTree(
                    guidingProbability, 
                    lower, upper, 
                    splitMargin,
                    samples.Count / 8);
            } 

            // Distribute data to the correct child nodes
            foreach (var sample in samples) {
                int idx = getChildIdx(sample.Position);
                ((RootAdaptiveProbabilityTree) childNodes[idx]).AddSampleData(sample);
            }

            // Remove leaf properties from current node
            samples = null;
            isLeaf = false;
        } 
        
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((RootAdaptiveProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            float firstDeriv = 0.0f;
            float secondDeriv = 0.0f;
            
            float div = (1.0f / (float) samples.Count);
            foreach (var sample in samples) {
                firstDeriv += sample.FirstDeriv;
                secondDeriv += sample.SecondDeriv;
            }            
            
            firstDeriv *= div;
            secondDeriv *= div;
            
            if(!(secondDeriv == 0 || float.IsNaN(firstDeriv) || float.IsNaN(secondDeriv))) {
                guidingProbability = float.Clamp(guidingProbability - (firstDeriv / secondDeriv), 0.1f, 0.9f);
            }
            samples.Clear();
        }
    }
}