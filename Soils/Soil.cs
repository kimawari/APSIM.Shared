
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
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [UIIgnore]
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the record number.
        /// </summary>
        /// <value>
        /// The record number.
        /// </value>
        [Description("Record number")]
        public int RecordNumber { get; set; }
        /// <summary>
        /// Gets or sets the asc order.
        /// </summary>
        /// <value>
        /// The asc order.
        /// </value>
        [Description("Australian Soil Classification Order")]
        public string ASCOrder { get; set; }
        /// <summary>
        /// Gets or sets the asc sub order.
        /// </summary>
        /// <value>
        /// The asc sub order.
        /// </value>
        [Description("Australian Soil Classification Sub-Order")]
        public string ASCSubOrder { get; set; }
        /// <summary>
        /// Gets or sets the type of the soil.
        /// </summary>
        /// <value>
        /// The type of the soil.
        /// </value>
        [Description("Soil texture or other descriptor")]
        public string SoilType { get; set; }
        /// <summary>
        /// Gets or sets the name of the local.
        /// </summary>
        /// <value>
        /// The name of the local.
        /// </value>
        [Description("Local name")]
        public string LocalName { get; set; }
        /// <summary>
        /// Gets or sets the site.
        /// </summary>
        /// <value>
        /// The site.
        /// </value>
        public string Site { get; set; }
        /// <summary>
        /// Gets or sets the nearest town.
        /// </summary>
        /// <value>
        /// The nearest town.
        /// </value>
        [Description("Nearest town")]
        public string NearestTown { get; set; }
        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>
        /// The region.
        /// </value>
        public string Region { get; set; }
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public string State { get; set; }
        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        /// <value>
        /// The country.
        /// </value>
        public string Country { get; set; }
        /// <summary>
        /// Gets or sets the natural vegetation.
        /// </summary>
        /// <value>
        /// The natural vegetation.
        /// </value>
        [Description("Natural vegetation")]
        public string NaturalVegetation { get; set; }
        /// <summary>
        /// Gets or sets the apsoil number.
        /// </summary>
        /// <value>
        /// The apsoil number.
        /// </value>
        [Description("APSoil number")]
        public string ApsoilNumber { get; set; }
        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        [Description("Latitude (WGS84)")]
        public double Latitude { get; set; }
        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        [Description("Longitude (WGS84)")]
        public double Longitude { get; set; }
        /// <summary>
        /// Gets or sets the location accuracy.
        /// </summary>
        /// <value>
        /// The location accuracy.
        /// </value>
        [Description("Location accuracy")]
        public string LocationAccuracy { get; set; }
        /// <summary>
        /// Gets or sets the year of sampling.
        /// </summary>
        /// <value>
        /// The year of sampling.
        /// </value>
        [Description("Year of sampling")]
        public int YearOfSampling { get; set; }

        /// <summary>
        /// Gets or sets the data source.
        /// </summary>
        /// <value>
        /// The data source.
        /// </value>
        [UILargeText]
        [Description("Data source")]
        public string DataSource { get; set; }
        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        [UILargeText]
        public string Comments { get; set; }

        /// <summary>
        /// Gets or sets the water.
        /// </summary>
        /// <value>
        /// The water.
        /// </value>
        public Water Water { get; set; }
        /// <summary>
        /// Gets or sets the soil water.
        /// </summary>
        /// <value>
        /// The soil water.
        /// </value>
        public SoilWater SoilWater { get; set; }
        /// <summary>
        /// Gets or sets the soil organic matter.
        /// </summary>
        /// <value>
        /// The soil organic matter.
        /// </value>
        public SoilOrganicMatter SoilOrganicMatter { get; set; }
        /// <summary>
        /// Gets or sets the analysis.
        /// </summary>
        /// <value>
        /// The analysis.
        /// </value>
        public Analysis Analysis { get; set; }
        /// <summary>
        /// Gets or sets the initial water.
        /// </summary>
        /// <value>
        /// The initial water.
        /// </value>
        public InitialWater InitialWater { get; set; }

        /// <summary>
        /// Gets or sets the samples.
        /// </summary>
        /// <value>
        /// The samples.
        /// </value>
        [XmlElement("Sample")]
        public List<Sample> Samples { get; set; }
    }
}
