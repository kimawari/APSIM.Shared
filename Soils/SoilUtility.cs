// -----------------------------------------------------------------------
// <copyright file="SoilUtility.cs" company="CSIRO">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace APSIM.Shared.Soils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using System.IO;

    /// <summary>
    /// Various soil utilities.
    /// </summary>
    public class SoilUtility
    {

        /// <summary>
        /// Create a soil object from the XML passed in.
        /// </summary>
        public static Soil FromXML(string Xml)
        {
            XmlSerializer x = new XmlSerializer(typeof(Soil));
            StringReader F = new StringReader(Xml);
            return x.Deserialize(F) as Soil;
        }

        /// <summary>
        /// Write soil to XML
        /// </summary>
        public static string ToXML(Soil soil)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer x = new XmlSerializer(typeof(Soil));

            StringWriter Out = new StringWriter();
            x.Serialize(Out, soil, ns);
            string st = Out.ToString();
            if (st.Length > 5 && st.Substring(0, 5) == "<?xml")
            {
                // remove the first line: <?xml version="1.0"?>/n
                int posEol = st.IndexOf("\n");
                if (posEol != -1)
                    return st.Substring(posEol + 1);
            }
            return st;
        }

        /// <summary>
        /// Convert the specified thicknesses to mid points for plotting.
        /// </summary>
        /// <param name="Thickness">The thicknesses.</param>
        /// <returns>The array of midpoints</returns>
        static public double[] ToMidPoints(double[] Thickness)
        {
            double[] CumThickness = ToCumThickness(Thickness);
            double[] MidPoints = new double[CumThickness.Length];
            for (int Layer = 0; Layer != CumThickness.Length; Layer++)
            {
                if (Layer == 0)
                    MidPoints[Layer] = CumThickness[Layer] / 2.0;
                else
                    MidPoints[Layer] = (CumThickness[Layer] + CumThickness[Layer - 1]) / 2.0;
            }
            return MidPoints;
        }

        /// <summary>
        /// Convert the thickness to cumulative thickness.
        /// </summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns>Cumulative thicknesses.</returns>
        static public double[] ToCumThickness(double[] Thickness)
        {
            // ------------------------------------------------
            // Return cumulative thickness for each layer - mm
            // ------------------------------------------------
            double[] CumThickness = new double[Thickness.Length];
            if (Thickness.Length > 0)
            {
                CumThickness[0] = Thickness[0];
                for (int Layer = 1; Layer != Thickness.Length; Layer++)
                    CumThickness[Layer] = Thickness[Layer] + CumThickness[Layer - 1];
            }
            return CumThickness;
        }

        #region Crop

        /// <summary>
        /// Get or set the names of crops. Note: When setting, the crops will be reorded to match
        /// the setting list of names. Also new crops will be added / deleted as required.
        /// </summary>
        public static string[] GetCropNames(Soil soil)
        {
            if (soil.Water == null || soil.Water.Crops == null)
                return new string[0];

            List<string> cropNames = new List<string>();
            for (int i = 0; i < soil.Water.Crops.Count; i++)
                cropNames.Add(soil.Water.Crops[i].Name);

            return cropNames.ToArray();
        }

        /// <summary>
        /// Return a specific crop to caller. Will throw if crop doesn't exist.
        /// </summary>
        public static SoilCrop Crop(Soil soil, string cropName)
        {
            if (soil.Water == null || soil.Water.Crops == null)
                return null;

            SoilCrop measuredCrop = soil.Water.Crops.Find(c => c.Name.Equals(cropName, StringComparison.InvariantCultureIgnoreCase));
            if (measuredCrop != null)
                return measuredCrop;
            SoilCrop Predicted = PredictedCrop(soil, cropName);
            if (Predicted != null)
                return Predicted;
            throw new Exception("Soil could not find crop: " + cropName);
        }

        #endregion

        #region Sample

        public static double[] SW(Soil soil, Sample sample, Sample.SWUnitsEnum toUnits)
        {
            if (toUnits != sample.SWUnits && sample.SW != null)
            {

                // convert the numbers
                if (sample.SWUnits == Sample.SWUnitsEnum.Volumetric)
                {
                    if (toUnits == Sample.SWUnitsEnum.Gravimetric)
                        return Utility.Math.Divide(sample.SW, BDMapped(soil, sample.Thickness));
                    else if (toUnits == Sample.SWUnitsEnum.mm)
                        return Utility.Math.Multiply(sample.SW, sample.Thickness);
                }
                else if (sample.SWUnits == Sample.SWUnitsEnum.Gravimetric)
                {
                    if (toUnits == Sample.SWUnitsEnum.Volumetric)
                        return Utility.Math.Multiply(sample.SW, BDMapped(soil, sample.Thickness));
                    else if (toUnits == Sample.SWUnitsEnum.mm)
                        return Utility.Math.Multiply(Utility.Math.Multiply(sample.SW, BDMapped(soil, sample.Thickness)), sample.Thickness);
                }
                else
                {
                    if (toUnits == Sample.SWUnitsEnum.Volumetric)
                        return Utility.Math.Divide(sample.SW, sample.Thickness);
                    else if (toUnits == Sample.SWUnitsEnum.Gravimetric)
                        return Utility.Math.Divide(Utility.Math.Divide(sample.SW, sample.Thickness), BDMapped(soil, sample.Thickness));
                }
            }

            return sample.SW;
        }
        #endregion

        #region Predicted Crops
        private static string[] BlackVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
        private static string[] GreyVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
        private static double[] PredictedThickness = new double[] { 150, 150, 300, 300, 300, 300, 300 };
        private static double[] PredictedXF = new double[] { 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00 };
        private static double[] WheatKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        private static double[] SorghumKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.04, 0.03 };
        private static double[] BarleyKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.03, 0.02 };
        private static double[] ChickpeaKL = new double[] { 0.06, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06 };
        private static double[] MungbeanKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.00, 0.00 };
        private static double[] CottonKL = new double[] { 0.10, 0.10, 0.10, 0.10, 0.09, 0.07, 0.05 };
        private static double[] CanolaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        private static double[] PigeonPeaKL = new double[] { 0.06, 0.06, 0.06, 0.05, 0.04, 0.02, 0.01 };
        private static double[] MaizeKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        private static double[] CowpeaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        private static double[] SunflowerKL = new double[] { 0.01, 0.01, 0.08, 0.06, 0.04, 0.02, 0.01 };
        private static double[] FababeanKL = new double[] { 0.08, 0.08, 0.08, 0.08, 0.06, 0.04, 0.03 };
        private static double[] LucerneKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.09, 0.09 };
        private static double[] PerennialKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.07, 0.05 };

        private class BlackVertosol
        {
            internal static double[] CottonA = new double[] { 0.832, 0.868, 0.951, 0.988, 1.043, 1.095, 1.151 };
            internal static double[] SorghumA = new double[] { 0.699, 0.802, 0.853, 0.907, 0.954, 1.003, 1.035 };
            internal static double[] WheatA = new double[] { 0.124, 0.049, 0.024, 0.029, 0.146, 0.246, 0.406 };

            internal static double CottonB = -0.0070;
            internal static double SorghumB = -0.0038;
            internal static double WheatB = 0.0116;

        }
        private class GreyVertosol
        {
            internal static double[] CottonA = new double[] { 0.853, 0.851, 0.883, 0.953, 1.022, 1.125, 1.186 };
            internal static double[] SorghumA = new double[] { 0.818, 0.864, 0.882, 0.938, 1.103, 1.096, 1.172 };
            internal static double[] WheatA = new double[] { 0.660, 0.655, 0.701, 0.745, 0.845, 0.933, 1.084 };
            internal static double[] BarleyA = new double[] { 0.847, 0.866, 0.835, 0.872, 0.981, 1.036, 1.152 };
            internal static double[] ChickpeaA = new double[] { 0.435, 0.452, 0.481, 0.595, 0.668, 0.737, 0.875 };
            internal static double[] FababeanA = new double[] { 0.467, 0.451, 0.396, 0.336, 0.190, 0.134, 0.084 };
            internal static double[] MungbeanA = new double[] { 0.779, 0.770, 0.834, 0.990, 1.008, 1.144, 1.150 };
            internal static double CottonB = -0.0082;
            internal static double SorghumB = -0.007;
            internal static double WheatB = -0.0032;
            internal static double BarleyB = -0.0051;
            internal static double ChickpeaB = 0.0029;
            internal static double FababeanB = 0.02455;
            internal static double MungbeanB = -0.0034;
        }

        /// <summary>
        /// Return a list of predicted crop names or an empty string[] if none found.
        /// </summary>
        public string[] PredictedCropNames(Soil soil)
        {
            if (soil.SoilType != null)
            {
                if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
                    return BlackVertosolCropList;
                else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
                    return GreyVertosolCropList;
            }
            return new string[0];
        }

        /// <summary>
        /// Return a predicted SoilCrop for the specified crop name or null if not found.
        /// </summary>
        private static SoilCrop PredictedCrop(Soil soil, string CropName)
        {
            double[] A = null;
            double B = double.NaN;
            double[] KL = null;

            if (soil.SoilType == null)
                return null;

            if (soil.SoilType.Equals("Black Vertosol", StringComparison.CurrentCultureIgnoreCase))
            {
                if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.CottonA;
                    B = BlackVertosol.CottonB;
                    KL = CottonKL;
                }
                else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.SorghumA;
                    B = BlackVertosol.SorghumB;
                    KL = SorghumKL;
                }
                else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = BlackVertosol.WheatA;
                    B = BlackVertosol.WheatB;
                    KL = WheatKL;
                }
            }
            else if (soil.SoilType.Equals("Grey Vertosol", StringComparison.CurrentCultureIgnoreCase))
            {
                if (CropName.Equals("Cotton", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.CottonA;
                    B = GreyVertosol.CottonB;
                    KL = CottonKL;
                }
                else if (CropName.Equals("Sorghum", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.SorghumA;
                    B = GreyVertosol.SorghumB;
                    KL = SorghumKL;
                }
                else if (CropName.Equals("Wheat", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.WheatA;
                    B = GreyVertosol.WheatB;
                    KL = WheatKL;
                }
                else if (CropName.Equals("Barley", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.BarleyA;
                    B = GreyVertosol.BarleyB;
                    KL = BarleyKL;
                }
                else if (CropName.Equals("Chickpea", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.ChickpeaA;
                    B = GreyVertosol.ChickpeaB;
                    KL = ChickpeaKL;
                }
                else if (CropName.Equals("Fababean", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.FababeanA;
                    B = GreyVertosol.FababeanB;
                    KL = FababeanKL;
                }
                else if (CropName.Equals("Mungbean", StringComparison.CurrentCultureIgnoreCase))
                {
                    A = GreyVertosol.MungbeanA;
                    B = GreyVertosol.MungbeanB;
                    KL = MungbeanKL;
                }
            }


            if (A == null)
                return null;

            double[] LL = PredictedLL(soil, A, B);
            LL = Map(LL, PredictedThickness, soil.Water.Thickness, MapType.Concentration, soil, LL.Last());
            KL = Map(KL, PredictedThickness, soil.Water.Thickness, MapType.Concentration, soil, KL.Last());
            double[] XF = Map(PredictedXF, PredictedThickness, soil.Water.Thickness, MapType.Concentration, soil, PredictedXF.Last());
            string[] Metadata = Utility.String.CreateStringArray("Estimated", soil.Water.Thickness.Length);

            return new SoilCrop()
            {
                Thickness = soil.Water.Thickness,
                LL = LL,
                LLMetadata = Metadata,
                KL = KL,
                KLMetadata = Metadata,
                XF = XF,
                XFMetadata = Metadata
            };
        }

        /// <summary>
        /// Calculate and return a predicted LL from the specified A and B values.
        /// </summary>
        private static double[] PredictedLL(Soil soil, double[] A, double B)
        {
            double[] DepthCentre = ToMidPoints(PredictedThickness);
            double[] LL15 = LL15Mapped(soil, PredictedThickness);
            double[] DUL = DULMapped(soil, PredictedThickness);
            double[] LL = new double[PredictedThickness.Length];
            for (int i = 0; i != PredictedThickness.Length; i++)
            {
                double DULPercent = DUL[i] * 100.0;
                LL[i] = DULPercent * (A[i] + B * DULPercent);
                LL[i] /= 100.0;

                // Bound the predicted LL values.
                LL[i] = Math.Max(LL[i], LL15[i]);
                LL[i] = Math.Min(LL[i], DUL[i]);
            }

            //  make the top 3 layers the same as the the top 3 layers of LL15
            if (LL.Length >= 3)
            {
                LL[0] = LL15[0];
                LL[1] = LL15[1];
                LL[2] = LL15[2];
            }
            return LL;
        }
        #endregion

        #region Mapping

        private enum MapType { Mass, Concentration, UseBD }

        /// <summary>
        /// Map soil variables from one layer structure to another.
        /// </summary>
        private static double[] Map(double[] FValues, double[] FThickness,
                                    double[] ToThickness, MapType MapType,
                                    Soil soil,
                                    double DefaultValueForBelowProfile = double.NaN)
        {
            if (FValues == null || FThickness == null)
                return null;

            double[] FromThickness = Utility.Math.RemoveMissingValuesFromBottom((double[])FThickness.Clone());
            double[] FromValues = (double[])FValues.Clone();

            if (FromValues == null)
                return null;

            // remove missing layers.
            for (int i = 0; i < FromValues.Length; i++)
            {
                if (double.IsNaN(FromValues[i]) || i >= FromThickness.Length || double.IsNaN(FromThickness[i]))
                {
                    FromValues[i] = double.NaN;
                    if (i == FromThickness.Length)
                        Array.Resize(ref FromThickness, i + 1);
                    FromThickness[i] = double.NaN;
                }
            }
            FromValues = Utility.Math.RemoveMissingValuesFromBottom(FromValues);
            FromThickness = Utility.Math.RemoveMissingValuesFromBottom(FromThickness);

            if (Utility.Math.AreEqual(FromThickness, ToThickness))
                return FromValues;

            if (FromValues.Length != FromThickness.Length)
                return null;

            // Add the default value if it was specified.
            if (!double.IsNaN(DefaultValueForBelowProfile))
            {
                Array.Resize(ref FromThickness, FromThickness.Length + 1);
                Array.Resize(ref FromValues, FromValues.Length + 1);
                FromThickness[FromThickness.Length - 1] = 3000;  // to push to profile deep.
                FromValues[FromValues.Length - 1] = DefaultValueForBelowProfile;
            }

            // If necessary convert FromValues to a mass.
            if (MapType == MapType.Concentration)
                FromValues = Utility.Math.Multiply(FromValues, FromThickness);
            else if (MapType == MapType.UseBD)
            {
                double[] BD = soil.Water.BD;
                for (int Layer = 0; Layer < FromValues.Length; Layer++)
                    FromValues[Layer] = FromValues[Layer] * BD[Layer] * FromThickness[Layer] / 100;
            }

            // Remapping is achieved by first constructing a map of
            // cumulative mass vs depth
            // The new values of mass per layer can be linearly
            // interpolated back from this shape taking into account
            // the rescaling of the profile.

            double[] CumDepth = new double[FromValues.Length + 1];
            double[] CumMass = new double[FromValues.Length + 1];
            CumDepth[0] = 0.0;
            CumMass[0] = 0.0;
            for (int Layer = 0; Layer < FromThickness.Length; Layer++)
            {
                CumDepth[Layer + 1] = CumDepth[Layer] + FromThickness[Layer];
                CumMass[Layer + 1] = CumMass[Layer] + FromValues[Layer];
            }

            //look up new mass from interpolation pairs
            double[] ToMass = new double[ToThickness.Length];
            for (int Layer = 1; Layer <= ToThickness.Length; Layer++)
            {
                double LayerBottom = Utility.Math.Sum(ToThickness, 0, Layer, 0.0);
                double LayerTop = LayerBottom - ToThickness[Layer - 1];
                bool DidInterpolate;
                double CumMassTop = Utility.Math.LinearInterpReal(LayerTop, CumDepth,
                    CumMass, out DidInterpolate);
                double CumMassBottom = Utility.Math.LinearInterpReal(LayerBottom, CumDepth,
                    CumMass, out DidInterpolate);
                ToMass[Layer - 1] = CumMassBottom - CumMassTop;
            }

            // If necessary convert FromValues back into their former units.
            if (MapType == MapType.Concentration)
                ToMass = Utility.Math.Divide(ToMass, ToThickness);
            else if (MapType == MapType.UseBD)
            {
                double[] BD = BDMapped(soil, ToThickness);
                for (int Layer = 0; Layer < FromValues.Length; Layer++)
                    ToMass[Layer] = ToMass[Layer] * 100.0 / BD[Layer] / ToThickness[Layer];
            }

            for (int i = 0; i < ToMass.Length; i++)
                if (double.IsNaN(ToMass[i]))
                    ToMass[i] = 0.0;
            return ToMass;
        }

        /// <summary>
        /// Bulk density - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        public static double[] BDMapped(Soil soil, double[] ToThickness)
        {
            return Map(soil.Water.BD, soil.Water.Thickness, ToThickness, MapType.Concentration, soil, soil.Water.BD.Last());
        }

        /// <summary>
        /// AirDry - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        public static double[] AirDryMapped(Soil soil, double[] ToThickness)
        {
            return Map(soil.Water.AirDry, soil.Water.Thickness, ToThickness, MapType.Concentration, soil, soil.Water.AirDry.Last());
        }

        /// <summary>
        /// Lower limit 15 bar - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        public static double[] LL15Mapped(Soil soil, double[] ToThickness)
        {
            return Map(soil.Water.LL15, soil.Water.Thickness, ToThickness, MapType.Concentration, soil, soil.Water.LL15.Last());
        }

        /// <summary>
        /// Drained upper limit - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        public static double[] DULMapped(Soil soil, double[] ToThickness)
        {
            return Map(soil.Water.DUL, soil.Water.Thickness, ToThickness, MapType.Concentration, soil, soil.Water.DUL.Last());
        }

        #endregion
    }
}
