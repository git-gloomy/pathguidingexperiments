using SeeSharp.Images;
using SimpleImageIO;

namespace GuidedPathTracerExperiments;

// Used by the root adaptive guided path tracer, as described by Sbert et al.
// (see: https://diglib.eg.org/handle/10.2312/egs20191018)
class SquarerootWeightedLayer : RgbLayer {
    int learningInterval, intIteration;
    RgbImage intervalImage;
    public override void Init(int width, int height) {
        Image = new RgbImage(width, height);
        intervalImage = new RgbImage(width, height);
    }

    public SquarerootWeightedLayer(int learningInterval) {
        this.learningInterval = learningInterval;
    }

    public override void OnStartIteration(int curIteration) {
        this.intIteration = ((curIteration - 1) % learningInterval) + 1;
        this.curIteration = curIteration;

        if (intIteration > 1)
            intervalImage.Scale(1.0f - (1.0f / intIteration));
    }

    public override void OnEndIteration(int curIteration) {
        if (intIteration == learningInterval) {
            int curInterval = (curIteration / learningInterval);
            Image = (Image as RgbImage) * (1.0f - (1.0f / float.Sqrt(curInterval))) + intervalImage * (1.0f / float.Sqrt(curInterval));
            intervalImage.Fill(new float[] {0,0,0});
        }
    }

    public override void Splat(int x, int y, RgbColor value) =>
        (intervalImage as RgbImage).AtomicAdd((int)x, (int)y, value / intIteration);
}

