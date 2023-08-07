namespace GuidedPathTracerExperiments.Integrators {
    public class IntegratorSettings {
        /// <summary>
        /// If set to true, each iteration will be rendered as an individual layer in the .exr called
        /// "iter0001" etc. Also, the guiding caches of each iteration will be visualized in false color
        /// images in layers called "caches0001", "caches0002", ...
        /// </summary>
        public bool WriteIterationsAsLayers = false;

        /// <summary>
        /// If set to true, the probabilities and incident radiance stored in the probabilityTree
        /// will be rendered as layers in the .exr.
        /// </summary>
        public bool IncludeDebugVisualizations = false;
        
        public int debugVisualizationInterval = 32;

        /// <summary>
        /// Determines how many samples have to be gathered in a leaf of the probabilityTree before
        /// it is split up.
        /// </summary>
        public int TreeSplitMargin = 10000;

        public float InitialGuidingProbability = 0.5f;

        // The following 3 parameters are used in PrelearningExperiment
        public bool EnableGuidingFieldLearning = true;
        public int FixProbabilityUntil = -1;
        public int LearnUntil = int.MaxValue;

        /// <summary>
        /// Determines after how many iterations the guiding probabilities are reevaluated
        /// </summary>
        public int LearnInterval = 1;

        /// <summary>
        /// If true, discards all samples for learning except the ones in the learning iteration
        /// </summary>
        public bool SingleIterationLearning = false;

        public float FixedProbability = 0.5f;
    }
}