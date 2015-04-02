using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Soils
{
    public class SoilOrganicMatter
    {
        [Description("Root C:N ratio")]
        public double RootCN { get; set; }
        [Description("Root Weight (kg/ha)")]
        public double RootWt { get; set; }
        [Description("Soil C:N ratio")]
        public double SoilCN { get; set; }
        [Description("Erosion enrichment coefficient A")]
        public double EnrACoeff { get; set; }
        [Description("Erosion enrichment coefficient B")]
        public double EnrBCoeff { get; set; }
        public double[] Thickness { get; set; }
        public double[] OC { get; set; }
        public string[] OCMetadata { get; set; }
        [Units("0-1")]
        public double[] FBiom { get; set; }
        [Units("0-1")]
        public double[] FInert { get; set; }

        private const double ppm = 1000000.0;

        // Support for OC units.
        public enum OCUnitsEnum { Total, WalkleyBlack }
        public OCUnitsEnum OCUnits { get; set; }
    }
}
