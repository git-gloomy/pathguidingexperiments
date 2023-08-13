using System.Collections.Generic;
using System.Numerics;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees {

    public class GuidingProbabilityPathSegment {
        public Vector3 Position; 
        public float BsdfPdf, GuidePdf, MisWeight, SamplePdf;
        public RgbColor ScatteredContribution, ScatteringWeight, DirectContribution, BsdfCosine;
    }

    public class GuidingProbabilityPathSegmentStorage {
        int idx = 0;
        GuidingProbabilityPathSegment[] segments = new GuidingProbabilityPathSegment[5];
        public GuidingProbabilityPathSegment LastSegment { get; set; }

        public GuidingProbabilityPathSegment NextSegment() {
            GuidingProbabilityPathSegment segment = new();
            segments[idx] = segment;
            LastSegment = segment;
            idx++;
            return segment;
        }

        public void Reserve(uint size) {
            if(segments.Length == size) return;
            segments = new GuidingProbabilityPathSegment[size];
            this.idx = 0;
            LastSegment = null;
        }

        public void Clear() {
            LastSegment = null;
            this.idx = 0;
        }

        public void EvaluatePath(GuidingProbabilityTree tree) {
            if(tree.IsFrozen) {
                LastSegment = null;
                this.idx = 0;
                return;
            }
            // The following way of computing the contribution of individual path segments is a
            // simplified version of the logic used by OpenPGL's PathSegmentStorage
            for (int i = idx - 2; i >= 0; i--) {
                var segment = segments[i];

                if(segment.SamplePdf != 0) {
                    RgbColor throughput = new(1.0f);
                    RgbColor contrib = new(0.0f);

                    for (int j = i+1; j < idx; j++) {
                        var nextSegment = segments[j];

                        contrib += throughput * nextSegment.ScatteredContribution;
                        
                        if(j == i+1) contrib += throughput * nextSegment.DirectContribution;
                        else contrib += throughput * nextSegment.MisWeight * nextSegment.DirectContribution;

                        throughput *= nextSegment.ScatteringWeight;
                    }
                    
                    // We add sample data to the tree even if there is no contribution and let the
                    // tree handle that case
                    tree.AddSampleData(
                        segment.Position, 
                        segment.GuidePdf,
                        segment.BsdfPdf, 
                        segment.SamplePdf,
                        contrib * segment.BsdfCosine
                    );
                }
            }
            LastSegment = null;
            this.idx = 0;
        }
    }
}