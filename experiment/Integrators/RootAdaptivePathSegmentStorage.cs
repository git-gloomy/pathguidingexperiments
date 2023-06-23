using System.Collections.Generic;
using System.Numerics;
using SimpleImageIO;
using GuidedPathTracerExperiments.ProbabilityTrees;
using System;

namespace GuidedPathTracerExperiments.Integrators {

    public class RootAdaptivePathSegment {
        public bool UseForLearning;
        public Vector3 Position; 
        public float BsdfPdf, GuidePdf, MisWeight;
        public RgbColor ScatteredContribution, ScatteringWeight, DirectContribution;
    }

    public class RootAdaptivePathSegmentStorage {

        
        List<RootAdaptivePathSegment> segments = new List<RootAdaptivePathSegment>();
        public RootAdaptivePathSegment LastSegment { get; set; }

        public RootAdaptivePathSegment NextSegment() {
            var segment = new RootAdaptivePathSegment();
            segments.Add(segment);
            LastSegment = segment;
            return segment;
        }

        public void Clear() {
            LastSegment = null;
            segments.Clear();
        }

        public void EvaluatePath(RootAdaptiveProbabilityTree tree) {
            for (int i = segments.Count - 2; i >= 0; i--) {
                var segment = segments[i];

                if(segment.UseForLearning && segment.GuidePdf != 0 && segment.BsdfPdf != 0) {
                    RgbColor throughput = new RgbColor(1.0f);
                    RgbColor contrib = new RgbColor(0.0f);

                    for (int j = i+1; j < segments.Count; j++) {
                        var nextSegment = segments[j];

                        contrib += throughput * nextSegment.ScatteredContribution;
                        
                        if(j == i+1) contrib += throughput * nextSegment.DirectContribution;
                        else contrib += throughput * nextSegment.MisWeight * nextSegment.DirectContribution;

                        throughput = throughput * nextSegment.ScatteringWeight;
                    }

                    if (contrib.R > 0.0f || contrib.G > 0.0f || contrib.B > 0.0f) {
                        tree.AddSampleData(
                            segment.Position, 
                            segment.GuidePdf,
                            segment.BsdfPdf, 
                            contrib
                        );
                    }
                }
            }
            Clear();
        }
    }
}