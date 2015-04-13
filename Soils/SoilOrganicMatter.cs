using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Soils
{
    /// <summary>
    /// A soil organic matter class.
    /// </summary>
    public class SoilOrganicMatter
    {
        /// <summary>
        /// Gets or sets the root cn.
        /// </summary>
        /// <value>
        /// The root cn.
        /// </value>
        [Description("Root C:N ratio")]
        public double RootCN { get; set; }
        /// <summary>
        /// Gets or sets the root wt.
        /// </summary>
        /// <value>
        /// The root wt.
        /// </value>
        [Description("Root Weight (kg/ha)")]
        public double RootWt { get; set; }
        /// <summary>
        /// Gets or sets the soil cn.
        /// </summary>
        /// <value>
        /// The soil cn.
        /// </value>
        [Description("Soil C:N ratio")]
        public double SoilCN { get; set; }
        /// <summary>
        /// Gets or sets the enr a coeff.
        /// </summary>
        /// <value>
        /// The enr a coeff.
        /// </value>
        [Description("Erosion enrichment coefficient A")]
        public double EnrACoeff { get; set; }
        /// <summary>
        /// Gets or sets the enr b coeff.
        /// </summary>
        /// <value>
        /// The enr b coeff.
        /// </value>
        [Description("Erosion enrichment coefficient B")]
        public double EnrBCoeff { get; set; }
        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        /// <value>
        /// The thickness.
        /// </value>
        public double[] Thickness { get; set; }
        /// <summary>
        /// Gets or sets the oc.
        /// </summary>
        /// <value>
        /// The oc.
        /// </value>
        public double[] OC { get; set; }
        /// <summary>
        /// Gets or sets the oc metadata.
        /// </summary>
        /// <value>
        /// The oc metadata.
        /// </value>
        public string[] OCMetadata { get; set; }
        /// <summary>
        /// Gets or sets the f biom.
        /// </summary>
        /// <value>
        /// The f biom.
        /// </value>
        [Units("0-1")]
        public double[] FBiom { get; set; }
        /// <summary>
        /// Gets or sets the f inert.
        /// </summary>
        /// <value>
        /// The f inert.
        /// </value>
        [Units("0-1")]
        public double[] FInert { get; set; }

        /// <summary>
        /// The PPM
        /// </summary>
        private const double ppm = 1000000.0;

        // Support for OC units.
        /// <summary>
        /// 
        /// </summary>
        public enum OCUnitsEnum 
        {
            /// <summary>
            /// total (%)
            /// </summary>
            Total,

            /// <summary>
            /// walkley black (%)
            /// </summary>
            WalkleyBlack 
        }
        /// <summary>
        /// Gets or sets the oc units.
        /// </summary>
        /// <value>
        /// The oc units.
        /// </value>
        public OCUnitsEnum OCUnits { get; set; }
    }
}
