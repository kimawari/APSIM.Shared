
namespace APSIM.Shared.Soils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// The soil class encapsulates a soil characterisation and 0 or more soil samples.
    /// the methods in this class that return double[] always return using the 
    /// "Standard layer structure" i.e. the layer structure as defined by the Water child object.
    /// method. Mapping will occur to achieve this if necessary.
    /// To obtain the "raw", unmapped, values use the child classes e.g. SoilWater, Analysis and Sample.
    /// </summary>
    public class Soil
    {
        private static bool PathFixed = false;

        [UIIgnore]
        [XmlAttribute("name")]
        public string Name { get; set; }

        [Description("Record number")]
        public int RecordNumber { get; set; }
        [Description("Australian Soil Classification Order")]
        public string ASCOrder { get; set; }
        [Description("Australian Soil Classification Sub-Order")]
        public string ASCSubOrder { get; set; }
        [Description("Soil texture or other descriptor")]
        public string SoilType { get; set; }
        [Description("Local name")]
        public string LocalName { get; set; }
        public string Site { get; set; }
        [Description("Nearest town")]
        public string NearestTown { get; set; }
        public string Region { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        [Description("Natural vegetation")]
        public string NaturalVegetation { get; set; }
        [Description("APSoil number")]
        public string ApsoilNumber { get; set; }
        [Description("Latitude (WGS84)")]
        public double Latitude { get; set; }
        [Description("Longitude (WGS84)")]
        public double Longitude { get; set; }
        [Description("Location accuracy")]
        public string LocationAccuracy { get; set; }
        [Description("Year of sampling")]
        public int YearOfSampling { get; set; }

        [UILargeText]
        [Description("Data source")]
        public string DataSource { get; set; }
        [UILargeText]
        public string Comments { get; set; }

        public Water Water { get; set; }
        public SoilWater SoilWater { get; set; }
        public SoilOrganicMatter SoilOrganicMatter { get; set; }
        public Analysis Analysis { get; set; }
        public InitialWater InitialWater { get; set; }

        [XmlElement("Sample")]
        public List<Sample> Samples { get; set; }
    }
}
