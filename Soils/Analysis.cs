using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Soils
{
    public class Analysis
    {
        
        public double[] Thickness { get; set; }
        [Units("%")]
        public double[] Rocks { get; set; }
        public string[] RocksMetadata { get; set; }
        public string[] Texture { get; set; }
        public string[] TextureMetadata { get; set; }
        public string[] MunsellColour { get; set; }
        public string[] MunsellMetadata { get; set; }
        [Units("1:5 dS/m")]
        public double[] EC { get; set; }
        public string[] ECMetadata { get; set; }
        public double[] PH { get; set; }
        public string[] PHMetadata { get; set; }
        [Units("mg/kg")]
        public double[] CL { get; set; }
        public string[] CLMetadata { get; set; }
        [Units("Hot water mg/kg")]
        public double[] Boron { get; set; }
        public string[] BoronMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] CEC { get; set; }
        public string[] CECMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Ca { get; set; }
        public string[] CaMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Mg { get; set; }
        public string[] MgMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Na { get; set; }
        public string[] NaMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] K { get; set; }
        public string[] KMetadata { get; set; }
        [Units("%")]
        public double[] ESP { get; set; }
        public string[] ESPMetadata { get; set; }
        [Units("mg/kg")]
        public double[] Mn { get; set; }
        public string[] MnMetadata { get; set; }
        [Units("cmol+/kg")]
        public double[] Al { get; set; }
        public string[] AlMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeSand { get; set; }
        public string[] ParticleSizeSandMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeSilt { get; set; }
        public string[] ParticleSizeSiltMetadata { get; set; }
        [Units("%")]
        public double[] ParticleSizeClay { get; set; }
        public string[] ParticleSizeClayMetadata { get; set; }

        // Support for PH units.
        public enum PHUnitsEnum { Water, CaCl2 }
        public PHUnitsEnum PHUnits { get; set; }
              
        // Support for Boron units.
        public enum BoronUnitsEnum { HotWater, HotCaCl2 }
        public BoronUnitsEnum BoronUnits { get; set; }
    }
}
