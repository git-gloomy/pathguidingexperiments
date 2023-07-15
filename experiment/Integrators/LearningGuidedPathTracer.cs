using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using OpenPGL.NET;
using SeeSharp.Geometry;
using SeeSharp.Integrators;
using SeeSharp.Images;
using SimpleImageIO;
using TinyEmbree;
using GuidedPathTracerExperiments.ProbabilityTrees;

namespace GuidedPathTracerExperiments.Integrators {

    public abstract class LearningGuidedPathTracer : PathTracer {
        // Utility used to keep track of all data needed to build SampleData during path generation
        protected ThreadLocal<PathSegmentStorage> pathSegmentStorage = new();
        // Contains all SampleData generated by all threads during rendering, used to train the guiding field
        protected SampleStorage sampleStorage;
        // Represents guiding distribution
        protected ThreadLocal<SurfaceSamplingDistribution> distributionBuffer;

        // Used to calculate probabilities of guided vs bsdf sampling
        protected GuidingProbabilityTree probabilityTree;
        // Utility used to keep track of all data needed to learn guiding probabilities during path generation
        protected ThreadLocal<GuidingProbabilityPathSegmentStorage> probPathSegmentStorage = new();

        public SpatialSettings SpatialSettings = new KdTreeSettings() { KnnLookup = true };

        public IntegratorSettings Settings = new IntegratorSettings();
        public Field GuidingField;
        public bool GuidingEnabled { get; protected set; }

        protected List<SingleIterationLayer> iterationRenderings = new();
        protected List<SingleIterationLayer> iterationCacheVisualizations = new();

        //protected List<SingleIterationLayer> incidentRadianceVisualizations = new();
        protected List<SingleIterationLayer> guidingProbabilityVisualizations = new();

        protected bool enableProbabilityLearning = true;
        protected bool useLearnedProbabilities = false;

        public override void RegisterSample(Pixel pixel, RgbColor weight, float misWeight, uint depth,
                                            bool isNextEvent) {
            base.RegisterSample(pixel, weight, misWeight, depth, isNextEvent);

            if (Settings.WriteIterationsAsLayers) {
                var render = iterationRenderings[scene.FrameBuffer.CurIteration - 1];
                render.Splat(pixel.Row, pixel.Col, weight * misWeight);
            }
        }

        protected override void OnPrepareRender() {
            if(GuidingField == null) {
                GuidingField = new(new() {
                    SpatialSettings = SpatialSettings
                });
                Vector3 lower = scene.Bounds.Min - scene.Bounds.Diagonal * 0.01f;
                Vector3 upper = scene.Bounds.Max + scene.Bounds.Diagonal * 0.01f;
                GuidingField.SceneBounds = new() {
                    Lower = lower,
                    Upper = upper
                };
            }
            

            sampleStorage = new();
            int numPixels = scene.FrameBuffer.Width * scene.FrameBuffer.Height;
            sampleStorage.Reserve((uint)(MaxDepth * numPixels), 0);

            distributionBuffer = new(() => new(GuidingField));

            GuidingEnabled = false;

            if (Settings.WriteIterationsAsLayers) {
                for (int i = 0; i < TotalSpp; ++i) {
                    iterationRenderings.Add(new());
                    iterationCacheVisualizations.Add(new());
                    scene.FrameBuffer.AddLayer($"iter{i:0000}", iterationRenderings[^1]);
                    scene.FrameBuffer.AddLayer($"caches{i:0000}", iterationCacheVisualizations[^1]);
                }
            }

            
            if (Settings.IncludeDebugVisualizations) {
                for (int i = Settings.debugVisualizationInterval; i < TotalSpp; i += Settings.debugVisualizationInterval) {
                    //incidentRadianceVisualizations.Add(new());
                    guidingProbabilityVisualizations.Add(new());
                    //scene.FrameBuffer.AddLayer($"learnedRadiance{i:0000}", incidentRadianceVisualizations[^1]);
                    scene.FrameBuffer.AddLayer($"guidingProbability{i:0000}", guidingProbabilityVisualizations[^1]);
                }
            }

            base.OnPrepareRender();
        }

        protected override void OnAfterRender()
        {
            base.OnAfterRender();
            probabilityTree = null;
        }

        protected override void OnPreIteration(uint iterIdx)
        {            
            if (iterIdx + 1 > Settings.LearnUntil) enableProbabilityLearning = false;
            if (iterIdx + 2 == Settings.FixProbabilityUntil) scene.FrameBuffer.Reset();
            if (iterIdx + 1 > Settings.FixProbabilityUntil) useLearnedProbabilities = true;
        }

        protected override void OnPostIteration(uint iterIdx) {
            GuidingField.Update(sampleStorage, 1);
            sampleStorage.Clear();

            GuidingEnabled = true;
        }

        protected override void OnStartPath(PathState state) {
            // Reserve memory for the path segments in our thread-local storage
            if (!pathSegmentStorage.IsValueCreated)
                pathSegmentStorage.Value = new();

            if (!probPathSegmentStorage.IsValueCreated)
                probPathSegmentStorage.Value = new();

            pathSegmentStorage.Value.Reserve((uint)MaxDepth + 1);
            pathSegmentStorage.Value.Clear();

            probPathSegmentStorage.Value.Reserve((uint)MaxDepth + 1);
        }

        protected override void OnHit(in Ray ray, in Hit hit, PathState state) {
            // Prepare the next path segment: set all the info we already have
            var segment = pathSegmentStorage.Value.NextSegment();
            var probSegment = probPathSegmentStorage.Value.NextSegment();

            // Geometry
            segment.Position = hit.Position;
            segment.DirectionOut = -Vector3.Normalize(ray.Direction);
            segment.Normal = hit.ShadingNormal;
            probSegment.Position = hit.Position;

            if (Settings.WriteIterationsAsLayers && state.Depth == 1 && GuidingEnabled) {
                var distrib = GetDistribution(-ray.Direction, hit, state);
                if (distrib != null) {
                    var region = distrib.Region;

                    // Assign a color to this region based on its hash code
                    int hash = region.GetHashCode();
                    System.Random colorRng = new(hash);
                    float hue = (float)colorRng.Next(360);
                    float saturation = (float)colorRng.NextDouble() * 0.8f + 0.2f;

                    var color = RegionVisualizer.HsvToRgb(hue, saturation, 1.0f);
                    iterationCacheVisualizations[scene.FrameBuffer.CurIteration - 1]
                        .Splat(state.Pixel.Row, state.Pixel.Col, color);
                }
            }

            int curIteration = scene.FrameBuffer.CurIteration - 1;
            if (Settings.IncludeDebugVisualizations && state.Depth == 1) {
                int iterationsSinceUpdate = curIteration % Settings.debugVisualizationInterval;
                if(iterationsSinceUpdate == 0 && curIteration != 0) {
                    float p = probabilityTree.GetProbability(hit.Position);
                    RgbColor probabilityColor = new(
                        hit ? p : 0, 
                        hit ? 0.5f - float.Abs(p - 0.5f) : 0, 
                        hit ? 1.0f - p : 0
                    );
                    guidingProbabilityVisualizations[(int) (curIteration / Settings.debugVisualizationInterval) - 1]
                        .Splat(state.Pixel.Col, state.Pixel.Row, probabilityColor);

                    //RgbColor incidentRadiance = probabilityTree.GetAvgColor(hit.Position);
                    //incidentRadianceVisualizations[(int) (curIteration / debugVisualizationInterval) - 1]
                    //    .Splat(state.Pixel.Col, state.Pixel.Row, incidentRadiance);
                }                
            }
        }

        protected virtual float ComputeGuidingSelectProbability(Vector3 outDir, in SurfacePoint hit, in PathState state) {
            float roughness = hit.Material.GetRoughness(hit);
            if (roughness < 0.1f) return 0;
            if (hit.Material.IsTransmissive(hit)) return 0;
            if (!useLearnedProbabilities) return 0.5f;
            return probabilityTree.GetProbability(hit.Position);
        }

        protected SurfaceSamplingDistribution GetDistribution(Vector3 outDir, in SurfacePoint hit, in PathState state) {
            SurfaceSamplingDistribution distribution = null;

            if (GuidingEnabled) {
                distribution = distributionBuffer.Value;
                float u = state.Rng.NextFloat();
                distribution.Init(hit.Position, ref u, useParallaxCompensation: true);
                distribution.ApplyCosineProduct(hit.ShadingNormal);
            }
            return distribution;
        }

        protected override (Ray, float, RgbColor) SampleDirection(in Ray ray, in SurfacePoint hit, PathState state) {
            Vector3 outDir = Vector3.Normalize(-ray.Direction);
            float selectGuideProb = GuidingEnabled ?
                                    ComputeGuidingSelectProbability(outDir, hit, state) : 0;

            SurfaceSamplingDistribution distribution = null;
            if(GuidingEnabled) {
                distribution = GetDistribution(outDir, hit, state);
                Debug.Assert(distribution != null);
            }
            
            var probSegment = probPathSegmentStorage.Value.LastSegment;
            Ray nextRay;
            float guidePdf = 0, bsdfPdf;
            RgbColor contrib;
            if (state.Rng.NextFloat() < selectGuideProb) { // sample guided
                var sampledDir = distribution.Sample(state.Rng.NextFloat2D());
                guidePdf = distribution.PDF(sampledDir);           
                probSegment.GuidePdf = guidePdf;
                guidePdf *= selectGuideProb; 

                bsdfPdf = hit.Material.Pdf(hit, outDir, sampledDir, false).Item1;
                probSegment.BsdfPdf = bsdfPdf;
                bsdfPdf *= (1 - selectGuideProb);    

                contrib = hit.Material.EvaluateWithCosine(hit, outDir, sampledDir, false);
                contrib /= guidePdf + bsdfPdf;
                
                nextRay = Raytracer.SpawnRay(hit, sampledDir);
            } else { // Sample the BSDF (default)
                (nextRay, bsdfPdf, contrib) = base.SampleDirection(ray, hit, state);
                probSegment.BsdfPdf = bsdfPdf;
                bsdfPdf *= (1 - selectGuideProb);
                if(!(MathF.Abs(nextRay.Direction.LengthSquared() - 1.0f) < 0.001f)) return (new(), 0, RgbColor.Black);

                if (bsdfPdf == 0) { // prevent NaNs / Infs
                    return (new(), 0, RgbColor.Black);
                }

                if (selectGuideProb > 0) {
                    Debug.Assert(MathF.Abs(nextRay.Direction.LengthSquared() - 1) < 0.001f);
                    guidePdf = distribution.PDF(nextRay.Direction);
                    probSegment.GuidePdf = guidePdf;
                    guidePdf *= selectGuideProb;

                    // Apply balance heuristic
                    contrib *= bsdfPdf / (1 - selectGuideProb) / (guidePdf + bsdfPdf);
                }
            }            

            distribution?.Clear();

            float pdf = guidePdf + bsdfPdf;
            if (pdf == 0) { // prevent NaNs / Infs
                return (new(), 0, RgbColor.Black);
            }

            // Update the incident direction and PDF in the current path segment
            var segment = pathSegmentStorage.Value.LastSegment;
            var inDir = Vector3.Normalize(nextRay.Direction);
            segment.DirectionIn = inDir;
            segment.PDFDirectionIn = pdf;
            segment.ScatteringWeight = contrib;

            probSegment.SamplePdf = pdf;
            probSegment.ScatteringWeight = contrib;
            probSegment.BsdfCosine = hit.Material.EvaluateWithCosine(hit, outDir, inDir, false);

            // Material data
            segment.Roughness = hit.Material.GetRoughness(hit);
            if (Vector3.Dot(inDir, -ray.Direction) < 0) {
                float ior = hit.Material.GetIndexOfRefractionRatio(hit);
                if (Vector3.Dot(inDir, hit.ShadingNormal) < 0) {
                    ior = 1 / ior;
                }
                segment.Eta = ior;
            } else {
                segment.Eta = 1;
            }

            return (nextRay, pdf, contrib);
        }

        protected override float DirectionPdf(in SurfacePoint hit, Vector3 outDir, Vector3 sampledDir,
                                              PathState state) {
            float selectGuideProb = GuidingEnabled ? ComputeGuidingSelectProbability(outDir, hit, state) : 0;

            if (!GuidingEnabled || selectGuideProb <= 0)
                return base.DirectionPdf(hit, outDir, sampledDir, state);

            SurfaceSamplingDistribution distribution = GetDistribution(outDir, hit, state);

            float bsdfPdf = base.DirectionPdf(hit, outDir, sampledDir, state) * (1 - selectGuideProb);
            float guidePdf = distribution.PDF(Vector3.Normalize(sampledDir)) * selectGuideProb;
            distribution?.Clear();
            return bsdfPdf + guidePdf;
        }

        protected override void OnNextEventResult(in Ray ray, in SurfacePoint point, PathState state,
                                                  float misWeight, RgbColor estimate) {
            var segment = pathSegmentStorage.Value.LastSegment;

            var contrib = misWeight * estimate;
            segment.ScatteredContribution += (Vector3) contrib;

            var probSegment = probPathSegmentStorage.Value.LastSegment;
            probSegment.ScatteredContribution += contrib;
        }

        protected override void OnHitLightResult(in Ray ray, PathState state, float misWeight, RgbColor emission,
                                                 bool isBackground) {
            if (isBackground) {
                // We need to create the path segment first as there was no actual intersection
                var newSegment = pathSegmentStorage.Value.NextSegment();
                newSegment.DirectionOut = -Vector3.Normalize(ray.Direction);

                // We move the point far away enough for parallax compensation to no longer make a difference
                newSegment.Position = ray.Origin + ray.Direction * scene.Radius * 1337;

                var newProbSegment = probPathSegmentStorage.Value.NextSegment();
                newProbSegment.Position = ray.Origin + ray.Direction * scene.Radius * 1337;
            }

            var segment = pathSegmentStorage.Value.LastSegment;
            segment.MiWeight = misWeight;
            segment.DirectContribution = emission;

            var probSegment = probPathSegmentStorage.Value.LastSegment;
            probSegment.MisWeight = misWeight;
            probSegment.DirectContribution = emission;
        }

        protected override void OnFinishedPath(in RadianceEstimate estimate, PathState state) {
            base.OnFinishedPath(estimate, state);

            // Also accounts for probPathSegmentStorage
            if (!pathSegmentStorage.IsValueCreated) {
                return; // The path never hit anything.
            }

            // Generate the samples and add them to the global cache
            // TODO provide sampler (with more efficient wrapper)
            SamplerWrapper sampler = new(null, null);
            uint num = pathSegmentStorage.Value.PrepareSamples(
                sampler: sampler,
                splatSamples: false,
                useNEEMiWeights: false,
                guideDirectLight: false,
                rrAffectsDirectContribution: true);
            
            if (Settings.EnableGuidingFieldLearning) 
                sampleStorage.AddSamples(pathSegmentStorage.Value.SamplesRawPointer, num);

            if (enableProbabilityLearning) probPathSegmentStorage.Value.EvaluatePath(probabilityTree);
            else probPathSegmentStorage.Value.Clear();
        }
    }
}