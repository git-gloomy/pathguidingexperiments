namespace GuidedPathTracerExperiments.Integrators {
    public class IntegratorSettings {
        /// <summary>
        /// If set to true, each iteration will be rendered as an individual layer in the .exr called
        /// "iter0001" etc. Also, the guiding caches of each iteration will be visualized in false color
        /// images in layers called "caches0001", "caches0002", ...
        /// </summary>
        public bool WriteIterationsAsLayers = false;

        /// <summary>
        /// Enables/disables splatting of sample data into the guiding field.
        /// </summary>
        public bool GuidingFieldLearningEnabled = true;
        


        /// <summary>
        /// If set to true, the probabilities and incident radiance stored in the probabilityTree
        /// will be rendered as layers in the .exr.
        /// </summary>
        public bool IncludeDebugVisualizations = false;
        
        /// <summary>
        /// Determines how often the debug visualization of the guiding probabilities is rendered.
        /// </summary>
        public int DebugVisualizationInterval = 32;

        /// <summary>
        /// Determines how many samples have to be gathered in a leaf of the probabilityTree before
        /// it is split up.
        /// </summary>
        public int TreeSplitMargin = 20000;

        /// <summary>
        /// Probability used to initialize the probability tree. It is also used as a fixed
        /// guiding probability if the iteration count is less than <see cref="FixProbabilityUntil"/>.
        /// </summary>
        public float InitialGuidingProbability = 0.5f;
        


        /// <summary>
        /// After <see cref="LearnUntil"/> iterations, no further sample data is splatted into the
        /// guiding probability tree.
        /// </summary>
        public int LearnUntil = int.MaxValue;

        /// <summary>
        /// Each <see cref="LearnInterval"/> iterations, the guiding probabilities are reevaluated
        /// (if learning is enabled). Only used by <see cref="RootAdaptiveGuidedPathTracer"/> and
        /// <see cref="SecondMomentGuidedPathTracer"/>.
        /// </summary>
        public int LearnInterval = 32;
        
        /// <summary>
        /// The integrator uses the <see cref="InitialGuidingProbability"/> until <see
        /// cref="FixProbabilityUntil"/> iterations have passed.
        /// </summary>
        public int FixProbabilityUntil = -1;
        
        /// <summary>
        /// Added to the current iteration index before computing the hash used for the RNG.
        /// </summary>
        public uint RNGOffset = 0;


        public IntegratorSettings() {}

        /// <summary>
        /// Returns a new instances that copies the properties of <paramref name="settings"/>.
        /// </summary>
        public IntegratorSettings(IntegratorSettings settings) {
            WriteIterationsAsLayers = settings.WriteIterationsAsLayers;
            IncludeDebugVisualizations = settings.IncludeDebugVisualizations;
            DebugVisualizationInterval = settings.DebugVisualizationInterval;
            TreeSplitMargin = settings.TreeSplitMargin;
            InitialGuidingProbability = settings.InitialGuidingProbability;
            GuidingFieldLearningEnabled = settings.GuidingFieldLearningEnabled;
            LearnUntil = settings.LearnUntil;
            FixProbabilityUntil = settings.FixProbabilityUntil;
            RNGOffset = settings.RNGOffset;
        }
    }
}