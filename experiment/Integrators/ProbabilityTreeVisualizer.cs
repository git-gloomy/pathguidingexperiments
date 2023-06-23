using System.Numerics;
using GuidedPathTracerExperiments.ProbabilityTrees;
using SeeSharp;
using SeeSharp.Images;
using SeeSharp.Integrators;
using SeeSharp.Sampling;
using SimpleImageIO;
using TinyEmbree;

namespace GuidedPathTracerExperiments.Integrators;

public class ProbabilityTreeVisualizer : Integrator {

    public GuidingProbabilityTree probabilityTree { get; set; }
    string outputPath;

    public ProbabilityTreeVisualizer(GuidingProbabilityTree probabilityTree, string outputPath) {
        this.probabilityTree = probabilityTree;
        this.outputPath = outputPath;
    }

    RgbLayer visualization = new RgbLayer();

    public override void Render(Scene scene) {
        visualization.Init(scene.FrameBuffer.Width, scene.FrameBuffer.Height);
        System.Threading.Tasks.Parallel.For(0, scene.FrameBuffer.Height,
            row => {
                for (uint col = 0; col < scene.FrameBuffer.Width; ++col) {
                    RenderPixel(scene, (uint)row, col, 1);
                }
            }
        );
        visualization.Image.WriteToFile(outputPath);
    }

    private void RenderPixel(Scene scene, uint row, uint col, uint sampleIndex) {
        var rng = new RNG();
        Ray primaryRay = scene.Camera.GenerateRay(new Vector2(col, row), rng).Ray;
        var hit = scene.Raytracer.Trace(primaryRay);

        float p = probabilityTree.GetProbability(hit.Position);
        float[] value = new [] {hit ? p : 0, hit ? 0.5f - float.Abs(p - 0.5f) : 0, hit ? 1.0f - p : 0};
        //RgbColor color = probabilityTree.GetAvgColor(hit.Position);
        //float[] value = { color.R, color.G, color.B };
        
        visualization.Image.SetPixelChannels((int)col, (int)row, value);
    }
}