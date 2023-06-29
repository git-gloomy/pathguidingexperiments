using System;
using System.Collections.Concurrent;
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
    ConcurrentBag<RootAdaptiveSampleData> sampleData = new ConcurrentBag<RootAdaptiveSampleData>();

    public RootAdaptiveProbabilityTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
    }

    public void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, RgbColor radianceEstimate) {
        if (this.isLeaf) {
            float estimate = radianceEstimate.Average;
            float estimateSquared = estimate * estimate;
            float bsdfPdfMinusGuidePdf = bsdfPdf - guidePdf;
            float divisor = guidingProbability * guidePdf + (1.0f - guidingProbability) * bsdfPdf;

            float firstDeriv = estimateSquared * bsdfPdfMinusGuidePdf;
            firstDeriv /= MathF.Pow(divisor, 3.0f);
            
            float secondDeriv = estimateSquared * MathF.Pow(bsdfPdfMinusGuidePdf, 2.0f);
            secondDeriv /= MathF.Pow(divisor, 4.0f);
            
            sampleData.Add(new RootAdaptiveSampleData() {
                Position = position,
                FirstDeriv = firstDeriv,
                SecondDeriv = secondDeriv,
                RadianceEstimate = radianceEstimate
            });
        } else {
            ((RootAdaptiveProbabilityTree) childNodes[getChildIdx(position)])
                .AddSampleData(position, guidePdf, bsdfPdf, radianceEstimate);
        }
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[getChildIdx(point)].GetProbability(point);
    }

    void AddSampleData(RootAdaptiveSampleData sample) {
        if (this.isLeaf) {
            sampleData.Add(sample);
        } else {
            ((RootAdaptiveProbabilityTree) childNodes[getChildIdx(sample.Position)])
                .AddSampleData(sample);
        }
    }

    public void LearnProbabilities() {
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((RootAdaptiveProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else if (sampleData.Count > splitMargin) {
            for (int idx = 0; idx < 8; idx++) {
                // Calculate bounding box for each child node
                Vector3 lower = new Vector3(
                    idx < 4 ? this.lowerBounds.X : splitCoordinates.X,
                    idx % 4 < 2 ? this.lowerBounds.Y : splitCoordinates.Y,
                    idx % 2 < 1 ? this.lowerBounds.Z : splitCoordinates.Z
                );
                
                Vector3 upper = new Vector3(
                    idx < 4 ? splitCoordinates.X : this.upperBounds.X,
                    idx % 4 < 2 ? splitCoordinates.Y : this.upperBounds.Y,
                    idx % 2 < 1 ? splitCoordinates.Z : this.upperBounds.Z
                );

                Vector3 diagonal = (upper - lower) * 0.01f;
                lower -= diagonal;
                upper += diagonal;
                childNodes[idx] = new RootAdaptiveProbabilityTree(
                    guidingProbability, 
                    lower, upper, 
                    splitMargin);
            } 

            // Distribute data to the correct child nodes
            foreach (var sample in sampleData) {
                int idx = getChildIdx(sample.Position);
                ((RootAdaptiveProbabilityTree) childNodes[idx]).AddSampleData(sample);
            }

            // Remove leaf properties from current node
            sampleData.Clear();
            isLeaf = false;

            // Calculate probabilities for each child node
            Parallel.For(0, 8, idx => {
                ((RootAdaptiveProbabilityTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            float firstDeriv = 0.0f;
            float secondDeriv = 0.0f;
            
            float div = (1.0f / (float) sampleData.Count);
            foreach (var sample in sampleData) {
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
            sampleData.Clear();
        }
    }
}