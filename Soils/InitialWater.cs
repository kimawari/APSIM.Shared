using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace APSIM.Shared.Soils
{
    /// <summary>
    /// Class for holding information about the initial water state for a soil.
    /// </summary>
    public class InitialWater
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Two different mechanisms for specifying percent of water.
        /// </summary>
        public enum PercentMethodEnum 
        {
            /// <summary>
            /// Water should fill the profile from the top.
            /// </summary>
            FilledFromTop,

            /// <summary>
            /// Water should be evenly distributed down the profile.
            /// </summary>
            EvenlyDistributed 
        };
        /// <summary>
        /// Gets or sets the percent method.
        /// </summary>
        public PercentMethodEnum PercentMethod { get; set; }
        /// <summary>
        /// The fraction full (0-1)
        /// </summary>
        public double FractionFull = double.NaN;
        /// <summary>
        /// The depth wet soil (mm)
        /// </summary>
        public double DepthWetSoil = double.NaN;
        /// <summary>
        /// Gets or sets the crop name that the water should be relative to.
        /// </summary>
        public string RelativeTo { get; set; }

        
    }

}