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
    protected int sampleCount = 0;

    public AccumulatingTree(float probability, Vector3 lowerBounds, Vector3 upperBounds, int splitMargin, int expectedSamples) 
        : base(lowerBounds, upperBounds, splitMargin) {
        this.guidingProbability = probability;
        this.samples = new(expectedSamples);
    }

    public override float GetProbability(Vector3 point) {
        if(isLeaf) return guidingProbability;
        else return childNodes[GetChildIdx(point)].GetProbability(point);
    }

    private void AddSampleData(SampleData sample) {
        if(IsFrozen) return;
        if (this.isLeaf) {
            lock(this) {
                if (!this.isLeaf) {
                    // Another method has already split the node
                    int idx = GetChildIdx(sample.Position);
                    ((AccumulatingTree) childNodes[idx]).AddSampleData(sample);  
                    return;
                }
                sampleCount++;
                samples.Add(sample);
                if (sampleCount >= splitMargin) {
                    // Has to rely on the constructor of the child class
                    InitializeChildren();

                    // Distribute data to the correct child nodes
                    foreach (var s in samples) {
                        int idx = GetChildIdx(s.Position);
                        ((AccumulatingTree) childNodes[idx]).AddSampleData(s);
                    }
                    
                    foreach (var c in childNodes) {
                        ((AccumulatingTree) c).sampleCount = 0;
                    }

                    // Remove leaf properties from current node
                    samples = null;
                    isLeaf = false;
                }  
            }
        } else {
            int idx = GetChildIdx(sample.Position);
            ((AccumulatingTree) childNodes[idx]).AddSampleData(sample);  
        }
    }

    public override void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        if(IsFrozen) return;
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
        if(IsFrozen) return;        
        if (!isLeaf) {
            Parallel.For(0, 8, idx => {
                ((AccumulatingTree) childNodes[idx]).LearnProbabilities();
            });
        } else {
            LearnProbability();
        }
    }
}