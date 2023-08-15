using SeeSharp.Experiments;

SceneRegistry.AddSource("../Scenes");
Benchmark benchmark = new(new GuidedPathTracerExperiments.PrelearningExperiment(25, 50, int.MaxValue), new() {
    SceneRegistry.LoadScene("HomeOffice"),
    SceneRegistry.LoadScene("RoughGlassesIndirect", maxDepth: 10),
    SceneRegistry.LoadScene("Pool", maxDepth: 5),
    SceneRegistry.LoadScene("VeachAjar"),
    SceneRegistry.LoadScene("ModernHall"),
    SceneRegistry.LoadScene("VeachBidir"),
}, "Results", 640, 480, computeErrorMetrics: true);
benchmark.Run(skipReference: false);
