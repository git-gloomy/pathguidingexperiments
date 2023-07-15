using SeeSharp.Experiments;

SceneRegistry.AddSource("../Scenes");
Benchmark benchmark = new(new GuidedPathTracerExperiments.Experiment(256, int.MaxValue), new() {
    //SceneRegistry.LoadScene("CornellBox", maxDepth: 5),
    SceneRegistry.LoadScene("HomeOffice"),
    SceneRegistry.LoadScene("RoughGlassesIndirect", maxDepth: 10),
    //SceneRegistry.LoadScene("RoughGlasses", maxDepth: 10),
    //SceneRegistry.LoadScene("LampCaustic", maxDepth: 10),
    //SceneRegistry.LoadScene("TargetPractice"),
    //SceneRegistry.LoadScene("ModernHall"),
    SceneRegistry.LoadScene("CountryKitchen"),
    SceneRegistry.LoadScene("Pool", maxDepth: 5),
    //SceneRegistry.LoadScene("CornellBoxGlossyWalls", maxDepth: 5),
    SceneRegistry.LoadScene("VeachMIS"),
}, "Results", 640, 480, computeErrorMetrics: true);
benchmark.Run(skipReference: false);
