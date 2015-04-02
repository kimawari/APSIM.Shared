using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace APSIM.Shared.Soils
{
    public class InitialWater
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        public enum PercentMethodEnum { FilledFromTop, EvenlyDistributed };
        public PercentMethodEnum PercentMethod { get; set; }
        public double FractionFull = double.NaN;
        public double DepthWetSoil = double.NaN;
        public string RelativeTo { get; set; }

        
    }

}