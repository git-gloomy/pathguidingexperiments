using System.Numerics;
using SimpleImageIO;

namespace GuidedPathTracerExperiments.ProbabilityTrees;

// Based on Path guiding in production by Vorba et al. (see: https://dl.acm.org/doi/10.1145/3305366.3328091)
// This class implements Algorithm 3 from the paper by Vorba et al.
public class KullbackLeiblerTree : GuidingProbabilityTree {
    private static readonly float learningRate = 0.01f, regularization = 0.01f, beta1 = 0.9f, beta2 = 0.999f;
    private float sampleCount = 0, t = 0, m = 0, v = 0, theta = 0;

    public KullbackLeiblerTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin) 
        : base(lowerBounds, upperBounds, splitMargin) {
        // Nothing to do here
    }

    KullbackLeiblerTree(Vector3 lowerBounds, Vector3 upperBounds, int splitMargin,
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
        else return childNodes[GetChildIdx(point)].GetProbability(point);
    }

    // An Adam step is executed for each sample, minimizing the Kullback-Leibler divergence
    public override void AddSampleData(Vector3 position, float guidePdf, float bsdfPdf, float samplePdf, RgbColor radianceEstimate) {
        if (!isLeaf) {
            ((KullbackLeiblerTree) childNodes[GetChildIdx(position)])
                .AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimate);
            return;
        }
        sampleCount++;

        lock (this)
        {
            if (sampleCount > splitMargin) {
                Vector3 lower, upper;
                for (int idx = 0; idx < 8; idx++) {
                    (lower, upper) = GetChildBoundingBox(idx);    

                    childNodes[idx] = new KullbackLeiblerTree(
                        lower, upper, 
                        splitMargin,
                        t, m, v, theta);
                } 

                this.isLeaf = false;
                ((KullbackLeiblerTree) childNodes[GetChildIdx(position)])
                    .AddSampleData(position, guidePdf, bsdfPdf, samplePdf, radianceEstimate);                
            } else {
                float alpha = 1.0f / (1.0f + float.Exp(-theta));
                float combinedPdf = alpha * guidePdf + (1.0f - alpha) * bsdfPdf;
                float gradient = - radianceEstimate.Average * (guidePdf - bsdfPdf) / (samplePdf * combinedPdf);
                gradient *= alpha * (1.0f - alpha);
                gradient += regularization * theta;
                
                // Perform Adam step
                this.t++;
                float l = learningRate * float.Sqrt(1.0f - float.Pow(beta2, t))/(1.0f - float.Pow(beta1, t));
                this.m = beta1 * m + (1.0f - beta1) * gradient;
                this.v = beta2 * v + (1.0f - beta2) * gradient * gradient;
                this.theta -= l * m / (float.Sqrt(v) + float.Epsilon);
            }
        }
    }
}