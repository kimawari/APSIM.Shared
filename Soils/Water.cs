using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace APSIM.Shared.Soils
{
    /// <summary>
    /// A water specification for a soil.
    /// </summary>
    public class Water
    {
        /// <summary>
        /// The _ thickness
        /// </summary>
        private double[] _Thickness;
        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        /// <value>
        /// The thickness.
        /// </value>
        public double[] Thickness
        {
            get
            {
                return _Thickness;
            }
            set
            {
                _Thickness = value;
            }
        }

        /// <summary>
        /// Gets or sets the bd.
        /// </summary>
        /// <value>
        /// The bd.
        /// </value>
        [Units("g/cc")]
        public double[] BD { get; set; }
        /// <summary>
        /// Gets or sets the air dry.
        /// </summary>
        /// <value>
        /// The air dry.
        /// </value>
        [Units("mm/mm")]
        public double[] AirDry { get; set; }
        /// <summary>
        /// Gets or sets the l L15.
        /// </summary>
        /// <value>
        /// The l L15.
        /// </value>
        [Units("mm/mm")]
        public double[] LL15 { get; set; }
        /// <summary>
        /// Gets or sets the dul.
        /// </summary>
        /// <value>
        /// The dul.
        /// </value>
        [Units("mm/mm")]
        public double[] DUL { get; set; }
        /// <summary>
        /// Gets or sets the sat.
        /// </summary>
        /// <value>
        /// The sat.
        /// </value>
        [Units("mm/mm")]
        public double[] SAT { get; set; }
        /// <summary>
        /// Gets or sets the ks.
        /// </summary>
        /// <value>
        /// The ks.
        /// </value>
        [Units("mm/day")]
        public double[] KS { get; set; }

        /// <summary>
        /// Gets or sets the bd metadata.
        /// </summary>
        /// <value>
        /// The bd metadata.
        /// </value>
        public string[] BDMetadata { get; set; }
        /// <summary>
        /// Gets or sets the air dry metadata.
        /// </summary>
        /// <value>
        /// The air dry metadata.
        /// </value>
        public string[] AirDryMetadata { get; set; }
        /// <summary>
        /// Gets or sets the l L15 metadata.
        /// </summary>
        /// <value>
        /// The l L15 metadata.
        /// </value>
        public string[] LL15Metadata { get; set; }
        /// <summary>
        /// Gets or sets the dul metadata.
        /// </summary>
        /// <value>
        /// The dul metadata.
        /// </value>
        public string[] DULMetadata { get; set; }
        /// <summary>
        /// Gets or sets the sat metadata.
        /// </summary>
        /// <value>
        /// The sat metadata.
        /// </value>
        public string[] SATMetadata { get; set; }
        /// <summary>
        /// Gets or sets the ks metadata.
        /// </summary>
        /// <value>
        /// The ks metadata.
        /// </value>
        public string[] KSMetadata { get; set; }

        /// <summary>
        /// Gets or sets the crops.
        /// </summary>
        /// <value>
        /// The crops.
        /// </value>
        [XmlElement("SoilCrop")]
        public List<SoilCrop> Crops { get; set; }
    }
}
