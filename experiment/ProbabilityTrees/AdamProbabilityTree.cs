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
        if(isLeaf) return 1.0f / (1.0f + float.Exp(-theta));
        else return childNodes[getChildIdx(point)].GetProbability(point);
    }

    public void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimateCosine) {
        if (!isLeaf) {
            ((AdamProbabilityTree) childNodes[getChildIdx(position)])
                .AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimateCosine);
            return;
        }
        sampleCount++;

        lock (this)
        {
            this.avgColor = this.avgColor * (1.0f - (1.0f / sampleCount)) + radianceEstimateCosine * (1.0f / sampleCount);
            if (sampleCount > splitMargin) {
                Vector3 lower, upper;
                for (int idx = 0; idx < 8; idx++) {
                    (lower, upper) = GetChildBoundingBox(idx);    

                    childNodes[idx] = new AdamProbabilityTree(
                        lower, upper, 
                        splitMargin,
                        t, m, v, theta);
                } 

                this.isLeaf = false;
                ((AdamProbabilityTree) childNodes[getChildIdx(position)])
                    .AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimateCosine);                
            } else {
                float alpha = 1.0f / (1.0f + float.Exp(-theta));
                float combinedPdf = alpha * guidePdf + (1.0f - alpha) * bsdfPdf;
                float gradient = - radianceEstimateCosine.Average * (guidePdf - bsdfPdf) / (samplePdf * combinedPdf);
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