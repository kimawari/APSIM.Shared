using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace APSIM.Shared.Soils
{
    public class Water
    {
        private double[] _Thickness;
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

        [Units("g/cc")]
        public double[] BD { get; set; }
        [Units("mm/mm")]
        public double[] AirDry { get; set; }
        [Units("mm/mm")]
        public double[] LL15 { get; set; }
        [Units("mm/mm")]
        public double[] DUL { get; set; }
        [Units("mm/mm")]
        public double[] SAT { get; set; }
        [Units("mm/day")]
        public double[] KS { get; set; }

        public string[] BDMetadata { get; set; }
        public string[] AirDryMetadata { get; set; }
        public string[] LL15Metadata { get; set; }
        public string[] DULMetadata { get; set; }
        public string[] SATMetadata { get; set; }
        public string[] KSMetadata { get; set; }

        [XmlElement("SoilCrop")]
        public List<SoilCrop> Crops { get; set; }
    }
}
