using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Tasks;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

public class AdamProbabilityTree : GuidingProbabilityTree {
    static float learningRate = 0.01f, regularization = 0.01f, beta1 = 0.9f, beta2 = 0.999f;
    float sampleCount = 0, t = 0, m = 0, v = 0, theta = 0;

    public AdamProbabilityTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) 
        : base(lowerBounds, upperBounds, splitMargin) {
        // Nothing to do here
    }

    AdamProbabilityTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin,
                        float t, float m, float v, float theta)
                        : base(lowerBounds, upperBounds, splitMargin) {
        this.t = t;
        this.m = m;
        this.v = v;
        this.theta = theta;
    }

    public override float GetProbability(Vector3 point)
    {
        return 1.0f / (1.0f + float.Exp(-theta));
    }

    public void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        if (!this.isLeaf) {
            ((AdamProbabilityTree) childNodes[getChildIdx(position)]).AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimate);
            return;
        }
        sampleCount++;

        lock (this)
        {
            if (this.sampleCount > splitMargin) {
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
                    childNodes[idx] = new AdamProbabilityTree(
                        lower, upper, 
                        splitMargin,
                        t, m, v, theta);
                } 

                this.isLeaf = false;
                ((AdamProbabilityTree) childNodes[getChildIdx(position)]).AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimate);                
            } else {
                float alpha = 1.0f / (1.0f + float.Exp(-theta));
                float combinedPdf = alpha * guidePdf + (1.0f - alpha) * bsdfPdf;
                float gradient = - radianceEstimate.Average * (guidePdf - bsdfPdf) / (samplePdf * combinedPdf);
                gradient *= alpha * (1.0f - alpha);
                gradient += regularization * theta;
                
                // Perform adamStep
                this.t++;
                float l = learningRate * float.Sqrt(1.0f - float.Pow(beta2, t))/(1.0f - float.Pow(beta1, t));
                this.m = beta1 * m + (1.0f - beta1) * gradient;
                this.v = beta2 * v + (1.0f - beta2) * gradient * gradient;
                this.theta -= l * m / (float.Sqrt(v) + float.Epsilon);
            }
        }
    }
}