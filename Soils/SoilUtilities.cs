// -----------------------------------------------------------------------
// <copyright file="SoilUtilities.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace APSIM.Shared.Soils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using APSIM.Shared.Utilities;

    /// <summary>Various soil utilities.</summary>
    public class SoilUtilities
    {
        /// <summary>Create a soil object from the XML passed in.</summary>
        /// <param name="Xml">The XML.</param>
        /// <returns></returns>
        public static Soil FromXML(string Xml)
        {
            XmlSerializer x = new XmlSerializer(typeof(Soil));
            StringReader F = new StringReader(Xml);
            return x.Deserialize(F) as Soil;
        }

        /// <summary>Write soil to XML</summary>
        /// <param name="soil">The soil.</param>
        /// <returns></returns>
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

        /// <summary>Convert the specified thicknesses to mid points for plotting.</summary>
        /// <param name="Thickness">The thicknesses.</param>
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

        /// <summary>Returns a cumulative thickness based on the specified thickness.</summary>
        /// <param name="Thickness">The thickness.</param>
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
 
        #region PAWC

        /// <summary>Return the plant available water CAPACITY at standard thickness. Units: mm/mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        public static double[] PAWC(Soil soil)
        {
            return PAWC(soil.Water.Thickness, soil.Water.LL15, soil.Water.DUL, null);
        }

        /// <summary>Return the plant available water CAPACITY for the specified crop at standard thickness. Units: mm/mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        /// <param name="crop">The crop.</param>
        /// <returns></returns>
        public static double[] PAWCCrop(Soil soil, SoilCrop crop)
        {
            return PAWC(soil.Water.Thickness,
                        crop.LL,
                        soil.Water.DUL,
                        crop.XF);
        }

        /// <summary>Return the plant available water CAPACITY at standard thickness. Units: mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        public static double[] PAWCmm(Soil soil)
        {
            double[] pawc = PAWC(soil);
            return MathUtilities.Multiply(pawc, soil.Water.Thickness);
        }
        
        /// <summary>Return the plant available water CAPACITY for the specified crop at standard thickness. Units: mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        /// <param name="crop">The crop.</param>
        /// <returns></returns>
        public static double[] PAWCCropmm(Soil soil, SoilCrop crop)
        {
            double[] pawc = PAWCCrop(soil, crop);
            return MathUtilities.Multiply(pawc, soil.Water.Thickness);
        }

        /// <summary>Calculate plant available water CAPACITY. Units: mm/mm</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="LL">The ll.</param>
        /// <param name="DUL">The dul.</param>
        /// <param name="XF">The xf.</param>
        /// <returns></returns>
        public static double[] PAWC(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] PAWC = new double[Thickness.Length];
            if (LL == null)
                return PAWC;
            if (Thickness.Length != DUL.Length || Thickness.Length != LL.Length)
                throw new Exception("Number of soil layers in Water is different to number of layers in Water.Crop");

            for (int layer = 0; layer != Thickness.Length; layer++)
                if (DUL[layer] == MathUtilities.MissingValue ||
                    LL[layer] == MathUtilities.MissingValue)
                    PAWC[layer] = 0;
                else
                    PAWC[layer] = Math.Max(DUL[layer] - LL[layer], 0.0);

            bool ZeroXFFound = false;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (ZeroXFFound || XF != null && XF[layer] == 0)
                {
                    ZeroXFFound = true;
                    PAWC[layer] = 0;
                }
            return PAWC;
        }

        #endregion

        #region Crop

        /// <summary>Get or set the names of crops.</summary>
        /// <param name="soil">The soil.</param>
        /// <returns></returns>
        public static string[] GetCropNames(Soil soil)
        {
            if (soil.Water == null || soil.Water.Crops == null)
                return new string[0];

            List<string> cropNames = new List<string>();
            for (int i = 0; i < soil.Water.Crops.Count; i++)
                cropNames.Add(soil.Water.Crops[i].Name);

            return cropNames.ToArray();
        }

        /// <summary>Return a specific crop to caller. Will throw if crop doesn't exist.</summary>
        /// <param name="soil">The soil.</param>
        /// <param name="cropName">Name of the crop.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Soil could not find crop:  + cropName</exception>
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

        #region Predicted Crops
        /// <summary>
        /// The black vertosol crop list
        /// </summary>
        private static string[] BlackVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
        /// <summary>
        /// The grey vertosol crop list
        /// </summary>
        private static string[] GreyVertosolCropList = new string[] { "Wheat", "Sorghum", "Cotton" };
        /// <summary>
        /// The predicted thickness
        /// </summary>
        private static double[] PredictedThickness = new double[] { 150, 150, 300, 300, 300, 300, 300 };
        /// <summary>
        /// The predicted xf
        /// </summary>
        private static double[] PredictedXF = new double[] { 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00 };
        /// <summary>
        /// The wheat kl
        /// </summary>
        private static double[] WheatKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The sorghum kl
        /// </summary>
        private static double[] SorghumKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.04, 0.03 };
        /// <summary>
        /// The barley kl
        /// </summary>
        private static double[] BarleyKL = new double[] { 0.07, 0.07, 0.07, 0.05, 0.05, 0.03, 0.02 };
        /// <summary>
        /// The chickpea kl
        /// </summary>
        private static double[] ChickpeaKL = new double[] { 0.06, 0.06, 0.06, 0.06, 0.06, 0.06, 0.06 };
        /// <summary>
        /// The mungbean kl
        /// </summary>
        private static double[] MungbeanKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.00, 0.00 };
        /// <summary>
        /// The cotton kl
        /// </summary>
        private static double[] CottonKL = new double[] { 0.10, 0.10, 0.10, 0.10, 0.09, 0.07, 0.05 };
        /// <summary>
        /// The canola kl
        /// </summary>
        private static double[] CanolaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The pigeon pea kl
        /// </summary>
        private static double[] PigeonPeaKL = new double[] { 0.06, 0.06, 0.06, 0.05, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The maize kl
        /// </summary>
        private static double[] MaizeKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The cowpea kl
        /// </summary>
        private static double[] CowpeaKL = new double[] { 0.06, 0.06, 0.06, 0.04, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The sunflower kl
        /// </summary>
        private static double[] SunflowerKL = new double[] { 0.01, 0.01, 0.08, 0.06, 0.04, 0.02, 0.01 };
        /// <summary>
        /// The fababean kl
        /// </summary>
        private static double[] FababeanKL = new double[] { 0.08, 0.08, 0.08, 0.08, 0.06, 0.04, 0.03 };
        /// <summary>
        /// The lucerne kl
        /// </summary>
        private static double[] LucerneKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.09, 0.09 };
        /// <summary>
        /// The perennial kl
        /// </summary>
        private static double[] PerennialKL = new double[] { 0.01, 0.01, 0.01, 0.01, 0.09, 0.07, 0.05 };

        /// <summary>
        /// 
        /// </summary>
        private class BlackVertosol
        {
            /// <summary>
            /// The cotton a
            /// </summary>
            internal static double[] CottonA = new double[] { 0.832, 0.868, 0.951, 0.988, 1.043, 1.095, 1.151 };
            /// <summary>
            /// The sorghum a
            /// </summary>
            internal static double[] SorghumA = new double[] { 0.699, 0.802, 0.853, 0.907, 0.954, 1.003, 1.035 };
            /// <summary>
            /// The wheat a
            /// </summary>
            internal static double[] WheatA = new double[] { 0.124, 0.049, 0.024, 0.029, 0.146, 0.246, 0.406 };

            /// <summary>
            /// The cotton b
            /// </summary>
            internal static double CottonB = -0.0070;
            /// <summary>
            /// The sorghum b
            /// </summary>
            internal static double SorghumB = -0.0038;
            /// <summary>
            /// The wheat b
            /// </summary>
            internal static double WheatB = 0.0116;

        }
        /// <summary>
        /// 
        /// </summary>
        private class GreyVertosol
        {
            /// <summary>
            /// The cotton a
            /// </summary>
            internal static double[] CottonA = new double[] { 0.853, 0.851, 0.883, 0.953, 1.022, 1.125, 1.186 };
            /// <summary>
            /// The sorghum a
            /// </summary>
            internal static double[] SorghumA = new double[] { 0.818, 0.864, 0.882, 0.938, 1.103, 1.096, 1.172 };
            /// <summary>
            /// The wheat a
            /// </summary>
            internal static double[] WheatA = new double[] { 0.660, 0.655, 0.701, 0.745, 0.845, 0.933, 1.084 };
            /// <summary>
            /// The barley a
            /// </summary>
            internal static double[] BarleyA = new double[] { 0.847, 0.866, 0.835, 0.872, 0.981, 1.036, 1.152 };
            /// <summary>
            /// The chickpea a
            /// </summary>
            internal static double[] ChickpeaA = new double[] { 0.435, 0.452, 0.481, 0.595, 0.668, 0.737, 0.875 };
            /// <summary>
            /// The fababean a
            /// </summary>
            internal static double[] FababeanA = new double[] { 0.467, 0.451, 0.396, 0.336, 0.190, 0.134, 0.084 };
            /// <summary>
            /// The mungbean a
            /// </summary>
            internal static double[] MungbeanA = new double[] { 0.779, 0.770, 0.834, 0.990, 1.008, 1.144, 1.150 };
            /// <summary>
            /// The cotton b
            /// </summary>
            internal static double CottonB = -0.0082;
            /// <summary>
            /// The sorghum b
            /// </summary>
            internal static double SorghumB = -0.007;
            /// <summary>
            /// The wheat b
            /// </summary>
            internal static double WheatB = -0.0032;
            /// <summary>
            /// The barley b
            /// </summary>
            internal static double BarleyB = -0.0051;
            /// <summary>
            /// The chickpea b
            /// </summary>
            internal static double ChickpeaB = 0.0029;
            /// <summary>
            /// The fababean b
            /// </summary>
            internal static double FababeanB = 0.02455;
            /// <summary>
            /// The mungbean b
            /// </summary>
            internal static double MungbeanB = -0.0034;
        }

        /// <summary>
        /// Return a list of predicted crop names or an empty string[] if none found.
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <returns></returns>
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
        /// <param name="soil">The soil.</param>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
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
            LL = LayerStructure.MapConcentration(LL, PredictedThickness, soil.Water.Thickness, LL.Last());
            KL = LayerStructure.MapConcentration(KL, PredictedThickness, soil.Water.Thickness, KL.Last());
            double[] XF = LayerStructure.MapConcentration(PredictedXF, PredictedThickness, soil.Water.Thickness, PredictedXF.Last());
            string[] Metadata = StringUtilities.CreateStringArray("Estimated", soil.Water.Thickness.Length);

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
        /// <param name="soil">The soil.</param>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <returns></returns>
        private static double[] PredictedLL(Soil soil, double[] A, double B)
        {
            double[] DepthCentre = ToMidPoints(PredictedThickness);
            double[] LL15 = LayerStructure.LL15Mapped(soil, PredictedThickness);
            double[] DUL = LayerStructure.DULMapped(soil, PredictedThickness);
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

            //  make the top 3 layers the same as the top 3 layers of LL15
            if (LL.Length >= 3)
            {
                LL[0] = LL15[0];
                LL[1] = LL15[1];
                LL[2] = LL15[2];
            }
            return LL;
        }
        #endregion

    }
}
