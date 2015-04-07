
namespace APSIM.Shared.Soils
{
    using System.Xml.Serialization;

    public class Sample
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        public string Date { get; set; }

        public double[] Thickness { get; set; }
        public double[] NO3 { get; set; }
        public double[] NH4 { get; set; }
        public double[] SW { get; set; }
        public double[] OC { get; set; }
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }
        [Units("mg/kg")]
        public double[] CL { get; set; }
        [Units("%")]
        public double[] ESP { get; set; }
        public double[] PH { get; set; }

        public Sample() {Name = "Sample"; }

        // Support for NO3 units.
        public enum NUnitsEnum { ppm, kgha }
        public NUnitsEnum NO3Units { get; set; }
                 
        // Support for NH4 units.
        public NUnitsEnum NH4Units { get; set; }
         
        // Support for SW units.
        public enum SWUnitsEnum { Volumetric, Gravimetric, mm }
        public SWUnitsEnum SWUnits { get; set; }

        // Support for OC units.
        public enum OCSampleUnitsEnum { Total, WalkleyBlack }
        public OCSampleUnitsEnum OCUnits { get; set; }

        // Support for PH units.
        public enum PHSampleUnitsEnum { Water, CaCl2 }
        public PHSampleUnitsEnum PHUnits { get; set; }
    }
}
