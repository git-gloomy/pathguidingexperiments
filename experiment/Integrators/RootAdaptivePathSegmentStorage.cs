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

        
        Queue<RootAdaptivePathSegment> segments = new Queue<RootAdaptivePathSegment>();
        public RootAdaptivePathSegment LastSegment { get; set; }

        public RootAdaptivePathSegment NextSegment() {
            var segment = new RootAdaptivePathSegment();
            segments.Enqueue(segment);
            lastSegment = segment;
            return segment;
        }

        public void Clear() {
            lastSegment = null;
            segments.Clear();
        }

        public void EvaluatePath(RootAdaptiveProbabilityTree tree, RgbColor outgoing) {
            lastSegment = null;
            RgbColor avoidNaN = new(
                outgoing.R == 0.0f ? 1.0f : 0.0f,
                outgoing.G == 0.0f ? 1.0f : 0.0f,
                outgoing.B == 0.0f ? 1.0f : 0.0f
            );
            for (int i = 0; i < int.Min(segments.Count, 1); i++) {
                var segment = segments.Dequeue();

                if(segment.UseForLearning && segment.GuidePdf != 0 && segment.BsdfPdf != 0)                
                    tree.AddSampleData(
                        segment.Position, 
                        segment.GuidePdf,
                        segment.BsdfPdf, 
                        outgoing
                    );
    
                // If outgoing has a zero, that means that at least one of the contrib values has to
                // be zero. We add one to the respective before dividing in that case, so that we do
                // not end up with NaN and get zero instead
                outgoing /= (segment.Contrib + avoidNaN);  
            }
        }
    }
}