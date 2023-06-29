using System.Numerics;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

public abstract class GuidingProbabilityTree {
    protected int splitMargin { get; set; }
    protected GuidingProbabilityTree[] childNodes = new GuidingProbabilityTree[8];
    protected Vector3 splitCoordinates, lowerBounds, upperBounds;
    protected bool isLeaf = true;

    public RgbColor avgColor = new RgbColor(0.0f, 0.0f, 0.0f);

    public GuidingProbabilityTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) {
        this.lowerBounds = lowerBounds;
        this.upperBounds = upperBounds;
        this.splitCoordinates = lowerBounds + 0.5f * (upperBounds - lowerBounds);
        this.splitMargin = splitMargin;
    }

    public abstract float GetProbability(Vector3 point);

    public RgbColor GetAvgColor(Vector3 point) {
        if(isLeaf) return avgColor;
        else return childNodes[getChildIdx(point)].GetAvgColor(point);
    }

    protected int getChildIdx(Vector3 point) {
        int idx = 0;
        if(point.X > splitCoordinates.X) idx += 4;
        if(point.Y > splitCoordinates.Y) idx += 2;
        if(point.Z > splitCoordinates.Z) idx += 1;

        return idx;
    }
}