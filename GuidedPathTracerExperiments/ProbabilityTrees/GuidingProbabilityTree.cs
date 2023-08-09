using System.Numerics;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

public abstract class GuidingProbabilityTree {
    /// <summary>
    /// If more than <see cref="SplitMargin"/> samples are splatted into a leaf, it is split into 8 child nodes
    /// </summary>
    protected int SplitMargin { get; set; }
    /// <summary>
    /// Contains references to all child nodes of the current node
    /// </summary>
    protected GuidingProbabilityTree[] childNodes = new GuidingProbabilityTree[8];

    protected Vector3 splitCoordinates, lowerBounds, upperBounds;
    protected bool isLeaf = true;

    public GuidingProbabilityTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) {
        this.lowerBounds = lowerBounds;
        this.upperBounds = upperBounds;
        this.splitCoordinates = lowerBounds + 0.5f * (upperBounds - lowerBounds);
        this.SplitMargin = splitMargin;
    }

    /// <summary>
    /// Traverses the tree and returns the guiding probability associated with the given <paramref name="point"/>.
    /// </summary>
    public abstract float GetProbability(Vector3 point);

    /// <summary>
    /// Traverses the tree and adds the given data to a leaf node, which processes or stores it for learning.
    /// </summary>
    public abstract void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate);

    /// <summary>
    /// Gets the index of the child node corresponding to the <paramref name="point"/> in <see cref="childNodes"/>.
    /// </summary>
    protected int GetChildIdx(Vector3 point) {
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

    /// <summary>
    /// Computes the bounding box of a child node from the bounding box of its parent node.
    /// </summary>
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