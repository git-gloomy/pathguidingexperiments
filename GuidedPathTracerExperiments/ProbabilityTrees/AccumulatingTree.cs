using System.Numerics;
using System.Collections.Generic;
using SimpleImageIO;
using System.Threading.Tasks;


namespace GuidedPathTracerExperiments.ProbabilityTrees;

// Used as a parent class for all kinds of trees which accumulate samples and use them to learn at a later point.
public abstract class AccumulatingTree : GuidingProbabilityTree {
    protected class SampleData {
        public Vector3 Position { get; set; }
        public float GuidePdf { get; set; }
        public float BsdfPdf { get; set; }
        public float SamplePdf { get; set; }
        public RgbColor RadianceEstimate { get; set; }
    }

    protected float guidingProbability;
    protected List<SampleData> samples;

    public AccumulatingTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int expectedSamples) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
        this.samples = new(expectedSamples);
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[GetChildIdx(point)].GetProbability(point);
    }

    protected void AddSampleData(SampleData sample) {
        if (this.isLeaf) {
            lock(samples) {
                samples.Add(sample);
            }
        } else {
            ((SecondMomentTree) childNodes[GetChildIdx(sample.Position)])
                .AddSampleData(sample);
        }
    }

    public override void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        AddSampleData(new SampleData() {
                    Position = position,
                    GuidePdf = guidePdf,
                    BsdfPdf = bsdfPdf,
                    SamplePdf = samplePdf,
                    RadianceEstimate = radianceEstimate,
                });
    }

    /// <summary>
    /// Populates the <see cref="childNodes"/> with objects of the correct type of tree.
    /// </summary>
    protected abstract void InitializeChildren();

    /// <summary>
    /// Learns guiding probability for the current node using the samples stored in <see cref="samples"/> 
    /// </summary>
    protected abstract void LearnProbability();

    public void LearnProbabilities() {
        if (isLeaf && samples.Count > splitMargin) {
            // Has to rely on the constructor of the child class
            InitializeChildren();

            // Distribute data to the correct child nodes
            foreach (var sample in samples) {
                int idx = GetChildIdx(sample.Position);
                ((AccumulatingTree) childNodes[idx]).AddSampleData(sample);
            }

            // Remove leaf properties from current node
            samples = null;
            isLeaf = false;
        } 
        
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((AccumulatingTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            LearnProbability();
        }
    }
}