using Prism.Mvvm;

namespace GPM.CustomAnalysis.IsopositionFiltering
{
    public class IsopositionFilteringOptions : BindableBase
    {
        private const string DefaultElementOfInterest = "AM";
        private const double DefaultGridSize = 1d;
        private const double DefaultGridDelocalization = 0.5d;
        private const double DefaultXThreshold = 1d;
        private const double DefaultXStep = 0.1d;
        private const int DefaultDispStep = 20;


        private string elementOfInterest = DefaultElementOfInterest;
        /// <summary>
        /// Name of the element of interest
        /// </summary>
        public string ElementOfInterest
        {
            get => elementOfInterest;
            set => SetProperty(ref elementOfInterest, value);
        }

        private double _gridSize = DefaultGridSize;
        /// <summary>
        /// Grid size in nm
        /// </summary>
        public double GridSize
        {
            get => _gridSize;
            set => SetProperty(ref _gridSize, value);
        }

        private double gridDelocalization = DefaultGridDelocalization;
        /// <summary>
        /// Delocalization in nm
        /// </summary>
        public double GridDelocalization
        {
            get => gridDelocalization;
            set => SetProperty(ref gridDelocalization, value);
        }

        private double xThreshold = DefaultXThreshold;
        /// <summary>
        /// Concentration threshold : Min
        /// </summary>
        public double XThreshold
        {
            get => xThreshold;
            set => SetProperty(ref xThreshold, value);
        }

        private double xStep = DefaultXStep;
        /// <summary>
        /// Concentration distribution visualization : bin size
        /// </summary>
        public double XStep
        {
            get => xStep;
            set => SetProperty(ref xStep, value);
        }

        private int dispStep = DefaultDispStep;
        /// <summary>
        /// Number of Iteration for the progress "bar"
        /// </summary>
        public int DispStep
        {
            get => dispStep;
            set => SetProperty(ref dispStep, value);
        }
	}
}
