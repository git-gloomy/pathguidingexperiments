using System.Numerics;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

public abstract class GuidingProbabilityTree {
    protected int splitMargin { get; set; }
    protected GuidingProbabilityTree[] childNodes = new GuidingProbabilityTree[8];
    protected Vector3 splitCoordinates, lowerBounds, upperBounds;
    protected bool isLeaf = true;

    public GuidingProbabilityTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) {
        this.lowerBounds = lowerBounds;
        this.upperBounds = upperBounds;
        this.splitCoordinates = lowerBounds + 0.5f * (upperBounds - lowerBounds);
        this.splitMargin = splitMargin;
    }

    public abstract float GetProbability(Vector3 point);

    public abstract void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate);

    protected int getChildIdx(Vector3 point) {
        if (point.X > splitCoordinates.X) {
            if (point.Y > splitCoordinates.Y) {
                if (point.Z > splitCoordinates.Z) return 7;
                else return 6;
            } else {
                if (point.Z > splitCoordinates.Z) return 5;
                else return 4;
            }
        } else {
            if (point.Y > splitCoordinates.Y) {
                if (point.Z > splitCoordinates.Z) return 3;
                else return 2;
            } else {
                if (point.Z > splitCoordinates.Z) return 1;
                else return 0;
            }
        }
    }

    protected (Vector3, Vector3) GetChildBoundingBox(int childIdx) {
        Vector3 lower = new(
            childIdx < 4 ? this.lowerBounds.X : splitCoordinates.X,
            childIdx % 4 < 2 ? this.lowerBounds.Y : splitCoordinates.Y,
            childIdx % 2 < 1 ? this.lowerBounds.Z : splitCoordinates.Z
        );

        Vector3 upper = new(
            childIdx < 4 ? splitCoordinates.X : this.upperBounds.X,
            childIdx % 4 < 2 ? splitCoordinates.Y : this.upperBounds.Y,
            childIdx % 2 < 1 ? splitCoordinates.Z : this.upperBounds.Z
        );

        Vector3 diagonal = (upper - lower) * 0.01f;
        lower -= diagonal;
        upper += diagonal;
        return (lower, upper);
    }
}