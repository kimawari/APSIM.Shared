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
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Various soil utilities.
    /// </summary>
    public class SoilUtilities
    {

        /// <summary>
        /// Create a soil object from the XML passed in.
        /// </summary>
        /// <param name="Xml">The XML.</param>
        /// <returns></returns>
        public static Soil FromXML(string Xml)
        {
            XmlSerializer x = new XmlSerializer(typeof(Soil));
            StringReader F = new StringReader(Xml);
            return x.Deserialize(F) as Soil;
        }

        /// <summary>
        /// Write soil to XML
        /// </summary>
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

        /// <summary>
        /// Convert the specified thicknesses to mid points for plotting.
        /// </summary>
        /// <param name="Thickness">The thicknesses.</param>
        /// <returns>
        /// The array of midpoints
        /// </returns>
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
        /// <returns>
        /// Cumulative thicknesses.
        /// </returns>
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

        /// <summary>Creates a standardised the soil to a uniform thickness.</summary>
        /// <param name="soil">The soil.</param>
        /// <returns>A standardised soil.</returns>
        public static Soil CreateStandardisedSoil(Soil soil)
        {
            Soil newSoil = new Soil();
            newSoil.ApsoilNumber = soil.ApsoilNumber;
            newSoil.ASCOrder = soil.ASCOrder;
            newSoil.ASCSubOrder = soil.ASCSubOrder;
            newSoil.Comments = soil.Comments;
            newSoil.Country = soil.Country;
            newSoil.DataSource = soil.DataSource;
            newSoil.Latitude = soil.Latitude;
            newSoil.LocalName = soil.LocalName;
            newSoil.LocationAccuracy = soil.LocationAccuracy;
            newSoil.Longitude = soil.Longitude;
            newSoil.Name = soil.Name;
            newSoil.NaturalVegetation = soil.NaturalVegetation;
            newSoil.NearestTown = soil.NearestTown;
            newSoil.RecordNumber = soil.RecordNumber;
            newSoil.Region = soil.Region;
            newSoil.Site = soil.Site;
            newSoil.SoilType = soil.SoilType;
            newSoil.State = soil.State;
            newSoil.YearOfSampling = soil.YearOfSampling;

            newSoil.Water = soil.Water;
            newSoil.Analysis = SetAnalysisThickness(soil.Analysis, soil.Water.Thickness, soil);
            newSoil.InitialWater = soil.InitialWater;
            newSoil.SoilWater = SetSoilWaterThickness(soil.SoilWater, soil.Water.Thickness, soil);
            newSoil.SoilOrganicMatter = SetSoilOrganicMatterThickness(soil.SoilOrganicMatter, soil.Water.Thickness, soil);

            newSoil.Samples = new List<Sample>();
            foreach (Sample sample in soil.Samples)
                newSoil.Samples.Add(SetSampleThickness(sample, soil.Water.Thickness, soil));

            return newSoil;
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
            SoilCrop cropAtStandardThickness = SetCropThickness(crop, soil.Water.Thickness, soil);

            return PAWC(soil.Water.Thickness,
                        cropAtStandardThickness.LL,
                        soil.Water.DUL,
                        cropAtStandardThickness.XF);
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

        /// <summary>
        /// Calculate plant available water CAPACITY. Units: mm/mm
        /// </summary>
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
                throw new Exception("Number of soil layers in SoilWater is different to number of layers in SoilWater.Crop");

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

        /// <summary>
        /// Get or set the names of crops. Note: When setting, the crops will be reorded to match
        /// the setting list of names. Also new crops will be added / deleted as required.
        /// </summary>
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

        /// <summary>
        /// Return a specific crop to caller. Will throw if crop doesn't exist.
        /// </summary>
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

        /// <summary>Convert the crop to the specified thickness. Ensures LL is between AirDry and DUL.</summary>
        /// <param name="crop">The crop to convert</param>
        /// <param name="thickness">The thicknesses to convert the crop to.</param>
        /// <param name="soil">The soil the crop belongs to.</param>
        /// <returns>The new soil crop at standard thicknesses.</returns>
        public static SoilCrop SetCropThickness(SoilCrop crop, double[] thickness, Soil soil)
        {
            if (MathUtilities.AreEqual(thickness, crop.Thickness))
                return crop;

            SoilCrop newCrop = new SoilCrop();
            newCrop.Thickness = thickness;
            newCrop.LL = MapConcentration(crop.LL, crop.Thickness, thickness, LastValue(crop.LL));
            newCrop.KL = MapConcentration(crop.KL, crop.Thickness, thickness, LastValue(crop.KL));
            newCrop.XF = MapConcentration(crop.XF, crop.Thickness, thickness, LastValue(crop.XF));

            newCrop.LL = MathUtilities.Constrain(newCrop.LL, AirDryMapped(soil, thickness), DULMapped(soil, thickness));

            return newCrop;
        }

        #endregion

        #region SoilWater
        /// <summary>Sets the soil water thickness.</summary>
        /// <param name="soilWater">The soil water.</param>
        /// <param name="thickness">Thickness to change soil water to.</param>
        /// <param name="soil">The soil.</param>
        /// <returns>A new sample with the specified thickness</returns>
        private static SoilWater SetSoilWaterThickness(SoilWater soilWater, double[] thickness, Soil soil)
        {
            if (MathUtilities.AreEqual(thickness, soilWater.Thickness))
                return soilWater;

            string[] metadata = StringUtilities.CreateStringArray("Mapped", thickness.Length);

            SoilWater newSoilWater = new SoilWater();
            newSoilWater.CatchmentArea = soilWater.CatchmentArea;
            newSoilWater.CN2Bare = soilWater.CN2Bare;
            newSoilWater.CNCov = soilWater.CNCov;
            newSoilWater.CNRed = soilWater.CNRed;
            newSoilWater.DiffusConst = soilWater.DiffusConst;
            newSoilWater.DiffusSlope = soilWater.DiffusSlope;
            newSoilWater.DischargeWidth = soilWater.DischargeWidth;
            newSoilWater.MaxPond = soilWater.MaxPond;
            newSoilWater.Salb = soilWater.Salb;
            newSoilWater.Slope = soilWater.Slope;
            newSoilWater.SummerCona = soilWater.SummerCona;
            newSoilWater.SummerDate = soilWater.SummerDate;
            newSoilWater.SummerU = soilWater.SummerU;
            newSoilWater.WinterCona = soilWater.WinterCona;
            newSoilWater.WinterDate = soilWater.WinterDate;
            newSoilWater.WinterU = soilWater.WinterU;

            newSoilWater.Thickness = thickness;
            newSoilWater.KLAT = MapConcentration(soilWater.KLAT, soilWater.Thickness, thickness, LastValue(soilWater.KLAT));
            newSoilWater.MWCON = MapConcentration(soilWater.MWCON, soilWater.Thickness, thickness, LastValue(soilWater.MWCON));
            newSoilWater.SWCON = MapConcentration(soilWater.SWCON, soilWater.Thickness, thickness, LastValue(soilWater.SWCON));

            return newSoilWater;
        }

        #endregion

        #region Soil organic matter
        /// <summary>Sets the soil organic matter thickness.</summary>
        /// <param name="soilOrganicMatter">The soil organic matter.</param>
        /// <param name="thickness">Thickness to change soil water to.</param>
        /// <param name="soil">The soil.</param>
        /// <returns>A new SoilOrganicMatterwith the specified thickness</returns>
        private static SoilOrganicMatter SetSoilOrganicMatterThickness(SoilOrganicMatter soilOrganicMatter, double[] thickness, Soil soil)
        {
            if (MathUtilities.AreEqual(thickness, soilOrganicMatter.Thickness))
                return soilOrganicMatter;

            string[] metadata = StringUtilities.CreateStringArray("Mapped", thickness.Length);

            SoilOrganicMatter newSoilOrganicMatter = new SoilOrganicMatter();
            newSoilOrganicMatter.EnrACoeff = soilOrganicMatter.EnrACoeff;
            newSoilOrganicMatter.EnrBCoeff = soilOrganicMatter.EnrBCoeff;
            newSoilOrganicMatter.RootCN = soilOrganicMatter.RootCN;
            newSoilOrganicMatter.RootWt = soilOrganicMatter.RootWt;
            newSoilOrganicMatter.SoilCN = soilOrganicMatter.SoilCN;

            newSoilOrganicMatter.Thickness = thickness;
            newSoilOrganicMatter.FBiom = MapConcentration(soilOrganicMatter.FBiom, soilOrganicMatter.Thickness, thickness, LastValue(soilOrganicMatter.FBiom));
            newSoilOrganicMatter.FInert = MapConcentration(soilOrganicMatter.FInert, soilOrganicMatter.Thickness, thickness, LastValue(soilOrganicMatter.FInert));
            newSoilOrganicMatter.OC = MapConcentration(soilOrganicMatter.OC, soilOrganicMatter.Thickness, thickness, LastValue(soilOrganicMatter.OC));

            newSoilOrganicMatter.OCMetadata = metadata;
            newSoilOrganicMatter.OCUnits = soilOrganicMatter.OCUnits;
            return newSoilOrganicMatter;
        }

        #endregion

        #region Sample

        /// <summary>
        /// Calculates the specified soil water (mm/mm)
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="sample">The sample.</param>
        /// <param name="toUnits">To units.</param>
        /// <returns></returns>
        public static double[] SW(Soil soil, Sample sample, Sample.SWUnitsEnum toUnits)
        {
            if (toUnits != sample.SWUnits && sample.SW != null)
            {

                // convert the numbers
                if (sample.SWUnits == Sample.SWUnitsEnum.Volumetric)
                {
                    if (toUnits == Sample.SWUnitsEnum.Gravimetric)
                        return MathUtilities.Divide(sample.SW, BDMapped(soil, sample.Thickness));
                    else if (toUnits == Sample.SWUnitsEnum.mm)
                        return MathUtilities.Multiply(sample.SW, sample.Thickness);
                }
                else if (sample.SWUnits == Sample.SWUnitsEnum.Gravimetric)
                {
                    if (toUnits == Sample.SWUnitsEnum.Volumetric)
                        return MathUtilities.Multiply(sample.SW, BDMapped(soil, sample.Thickness));
                    else if (toUnits == Sample.SWUnitsEnum.mm)
                        return MathUtilities.Multiply(MathUtilities.Multiply(sample.SW, BDMapped(soil, sample.Thickness)), sample.Thickness);
                }
                else
                {
                    if (toUnits == Sample.SWUnitsEnum.Volumetric)
                        return MathUtilities.Divide(sample.SW, sample.Thickness);
                    else if (toUnits == Sample.SWUnitsEnum.Gravimetric)
                        return MathUtilities.Divide(MathUtilities.Divide(sample.SW, sample.Thickness), BDMapped(soil, sample.Thickness));
                }
            }

            return sample.SW;
        }

        /// <summary>Constrains the sample SW to between AirDry and DUL.</summary>
        /// <param name="sample">The sample.</param>
        /// <param name="soil">The soil.</param>
        public static void ConstrainSampleSW(Sample sample, Soil soil)
        {
            if (sample.SW != null)
            {
                // Make sure the soil water isn't below airdry or above DUL.
                sample.SW = MathUtilities.Constrain(SW(soil, sample, Sample.SWUnitsEnum.Volumetric),
                                                    AirDryMapped(soil, sample.Thickness),
                                                    DULMapped(soil, sample.Thickness));
            }
        }

        /// <summary>Sets the sample thickness.</summary>
        /// <param name="sample">The sample.</param>
        /// <param name="thickness">The thickness to change the sample to.</param>
        /// <param name="soil">The soil.</param>
        /// <returns>A new sample with the specified thickness</returns>
        private static Sample SetSampleThickness(Sample sample, double[] thickness, Soil soil)
        {
            if (MathUtilities.AreEqual(thickness, sample.Thickness))
                return sample;

            string[] metadata = StringUtilities.CreateStringArray("Mapped", thickness.Length);

            Sample newSample = new Sample();
            newSample.Name = sample.Name;
            newSample.Thickness = thickness;
            newSample.Date = sample.Date;
            newSample.CL = MapConcentration(sample.CL, sample.Thickness, thickness, LastValue(sample.CL));
            newSample.EC = MapConcentration(sample.EC, sample.Thickness, thickness, LastValue(sample.EC));
            newSample.ESP = MapConcentration(sample.ESP, sample.Thickness, thickness, LastValue(sample.ESP));
            newSample.NH4 = MapConcentration(sample.NH4, sample.Thickness, thickness, 0.2);
            newSample.NO3 = MapConcentration(sample.NO3, sample.Thickness, thickness, 1.0);
            newSample.OC = MapConcentration(sample.OC, sample.Thickness, thickness, LastValue(sample.OC));
            newSample.PH = MapConcentration(sample.PH, sample.Thickness, thickness, LastValue(sample.PH));
            newSample.SW = MapSW(sample.SW, sample.Thickness, thickness);

            newSample.NH4Units = sample.NH4Units;
            newSample.NO3Units = sample.NO3Units;
            newSample.OCUnits = sample.OCUnits;
            newSample.PHUnits = sample.PHUnits;
            newSample.SWUnits = sample.SWUnits;

            return newSample;            
        }

        #endregion

        #region Analysis

        /// <summary>Sets the analysis thickness.</summary>
        /// <param name="analysis">The analysis.</param>
        /// <param name="thickness">The thickness to change the analysis to.</param>
        /// <param name="soil">The soil.</param>
        /// <returns>A new analysis with the specified thickness</returns>
        private static Analysis SetAnalysisThickness(Analysis analysis, double[] thickness, Soil soil)
        {
            if (MathUtilities.AreEqual(thickness, analysis.Thickness))
                return analysis;

            string[] metadata = StringUtilities.CreateStringArray("Mapped", thickness.Length);

            Analysis newAnalysis = new Analysis();
            newAnalysis.Thickness = thickness;
            newAnalysis.Al = MapConcentration(analysis.Al, analysis.Thickness, thickness, LastValue(analysis.Al));
            newAnalysis.AlMetadata = metadata;
            newAnalysis.Ca = MapConcentration(analysis.Ca, analysis.Thickness, thickness, LastValue(analysis.Ca));
            newAnalysis.CaMetadata = metadata;
            newAnalysis.CEC = MapConcentration(analysis.CEC, analysis.Thickness, thickness, LastValue(analysis.CEC));
            newAnalysis.CECMetadata = metadata;
            newAnalysis.CL = MapConcentration(analysis.CL, analysis.Thickness, thickness, LastValue(analysis.CL));
            newAnalysis.CLMetadata = metadata;
            newAnalysis.EC = MapConcentration(analysis.EC, analysis.Thickness, thickness, LastValue(analysis.EC));
            newAnalysis.ECMetadata = metadata;
            newAnalysis.ESP = MapConcentration(analysis.ESP, analysis.Thickness, thickness, LastValue(analysis.ESP));
            newAnalysis.ESPMetadata = metadata;
            newAnalysis.K = MapConcentration(analysis.K, analysis.Thickness, thickness, LastValue(analysis.K));
            newAnalysis.KMetadata = metadata;
            newAnalysis.Mg = MapConcentration(analysis.Mg, analysis.Thickness, thickness, LastValue(analysis.Mg));
            newAnalysis.MgMetadata = metadata;
            newAnalysis.Mn = MapConcentration(analysis.Mn, analysis.Thickness, thickness, LastValue(analysis.Mn));
            newAnalysis.MnMetadata = metadata;
            newAnalysis.MunsellColour = null;
            newAnalysis.Na = MapConcentration(analysis.Na, analysis.Thickness, thickness, LastValue(analysis.Na));
            newAnalysis.NaMetadata = metadata;
            newAnalysis.ParticleSizeClay = MapConcentration(analysis.ParticleSizeClay, analysis.Thickness, thickness, LastValue(analysis.ParticleSizeClay));
            newAnalysis.ParticleSizeClayMetadata = metadata;
            newAnalysis.ParticleSizeSand = MapConcentration(analysis.ParticleSizeSand, analysis.Thickness, thickness, LastValue(analysis.ParticleSizeSand));
            newAnalysis.ParticleSizeSandMetadata = metadata;
            newAnalysis.ParticleSizeSilt = MapConcentration(analysis.ParticleSizeSilt, analysis.Thickness, thickness, LastValue(analysis.ParticleSizeSilt));
            newAnalysis.ParticleSizeSiltMetadata = metadata;
            newAnalysis.PH = MapConcentration(analysis.PH, analysis.Thickness, thickness, LastValue(analysis.PH));
            newAnalysis.PHMetadata = metadata;
            newAnalysis.Rocks = MapConcentration(analysis.Rocks, analysis.Thickness, thickness, LastValue(analysis.Rocks));
            newAnalysis.RocksMetadata = metadata;
            newAnalysis.Texture = null;

            newAnalysis.PHUnits = analysis.PHUnits;
            newAnalysis.BoronUnits = analysis.BoronUnits;
            
            return newAnalysis;
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
            LL = MapConcentration(LL, PredictedThickness, soil.Water.Thickness, LL.Last());
            KL = MapConcentration(KL, PredictedThickness, soil.Water.Thickness, KL.Last());
            double[] XF = MapConcentration(PredictedXF, PredictedThickness, soil.Water.Thickness, PredictedXF.Last());
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

        /// <summary>
        /// 
        /// </summary>
        private enum MapType { Mass, Concentration, UseBD }

        /// <summary>
        /// Map soil variables (using concentration) from one layer structure to another.
        /// </summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="defaultValueForBelowProfile">The default value for below profile.</param>
        /// <returns></returns>
        private static double[] MapConcentration(double[] fromValues, double[] fromThickness,
                                                 double[] toThickness, 
                                                 double defaultValueForBelowProfile)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            // convert from values to a mass basis with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(defaultValueForBelowProfile);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(3000);
            double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

            double[] newValues = MapMass(massValues, thickness.ToArray(), toThickness);

            // Convert mass back to concentration and return
            return MathUtilities.Divide(newValues, toThickness);
        }

        /// <summary>
        /// Map soil variables (using BD) from one layer structure to another.
        /// </summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <param name="soil">The soil.</param>
        /// <param name="defaultValueForBelowProfile">The default value for below profile.</param>
        /// <returns></returns>
        private static double[] MapUsingBD(double[] fromValues, double[] fromThickness,
                                           double[] toThickness,
                                           Soil soil,
                                           double defaultValueForBelowProfile)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            // create an array of values with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(defaultValueForBelowProfile);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(3000);

            // convert fromValues to a mass basis
            double[] BD = BDMapped(soil, fromThickness);
            for (int Layer = 0; Layer < values.Count; Layer++)
                values[Layer] = values[Layer] * BD[Layer] * fromThickness[Layer] / 100;

            // change layer structure
            double[] newValues = MapMass(values.ToArray(), thickness.ToArray(), toThickness);

            // convert newValues back to original units and return
            BD = BDMapped(soil, toThickness);
            for (int Layer = 0; Layer < newValues.Length; Layer++)
                newValues[Layer] = newValues[Layer] * 100.0 / BD[Layer] / toThickness[Layer];
            return newValues;
        }

        /// <summary>
        /// Map soil water from one layer structure to another.
        /// </summary>
        /// <param name="fromValues">The from values.</param>
        /// <param name="fromThickness">The from thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] MapSW(double[] fromValues, double[] fromThickness,
                                      double[] toThickness)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            // convert from values to a mass basis with a dummy bottom layer.
            List<double> values = new List<double>();
            values.AddRange(fromValues);
            values.Add(SecondLastValue(fromValues) * 0.8);
            values.Add(LastValue(fromValues) * 0.4);
            values.Add(0.0);
            List<double> thickness = new List<double>();
            thickness.AddRange(fromThickness);
            thickness.Add(LastValue(fromThickness));
            thickness.Add(LastValue(fromThickness));
            thickness.Add(3000);
            double[] massValues = MathUtilities.Multiply(values.ToArray(), thickness.ToArray());

            double[] newValues = MapMass(massValues, thickness.ToArray(), toThickness);

            // Convert mass back to concentration and return
            return MathUtilities.Divide(newValues, toThickness);
        }

        /// <summary>
        /// Map soil variables from one layer structure to another.
        /// </summary>
        /// <param name="fromValues">The f values.</param>
        /// <param name="fromThickness">The f thickness.</param>
        /// <param name="toThickness">To thickness.</param>
        /// <returns>The from values mapped to the specified thickness</returns>
        private static double[] MapMass(double[] fromValues, double[] fromThickness, double[] toThickness)
        {
            if (fromValues == null || fromThickness == null)
                return null;

            double[] FromThickness = MathUtilities.RemoveMissingValuesFromBottom((double[])fromThickness.Clone());
            double[] FromValues = (double[])fromValues.Clone();

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
            FromValues = MathUtilities.RemoveMissingValuesFromBottom(FromValues);
            FromThickness = MathUtilities.RemoveMissingValuesFromBottom(FromThickness);

            if (MathUtilities.AreEqual(FromThickness, toThickness))
                return FromValues;

            if (FromValues.Length != FromThickness.Length)
                return null;

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
            double[] ToMass = new double[toThickness.Length];
            for (int Layer = 1; Layer <= toThickness.Length; Layer++)
            {
                double LayerBottom = MathUtilities.Sum(toThickness, 0, Layer, 0.0);
                double LayerTop = LayerBottom - toThickness[Layer - 1];
                bool DidInterpolate;
                double CumMassTop = MathUtilities.LinearInterpReal(LayerTop, CumDepth,
                    CumMass, out DidInterpolate);
                double CumMassBottom = MathUtilities.LinearInterpReal(LayerBottom, CumDepth,
                    CumMass, out DidInterpolate);
                ToMass[Layer - 1] = CumMassBottom - CumMassTop;
            }

            for (int i = 0; i < ToMass.Length; i++)
                if (double.IsNaN(ToMass[i]))
                    ToMass[i] = 0.0;

            for (int i = 0; i < ToMass.Length; i++)
                if (double.IsNaN(ToMass[i]))
                    ToMass[i] = 0.0;
            return ToMass;
        }

        /// <summary>
        /// Bulk density - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] BDMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.Water.BD, soil.Water.Thickness, ToThickness, soil.Water.BD.Last());
        }

        /// <summary>
        /// AirDry - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] AirDryMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.Water.AirDry, soil.Water.Thickness, ToThickness, soil.Water.AirDry.Last());
        }

        /// <summary>
        /// Lower limit 15 bar - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] LL15Mapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.Water.LL15, soil.Water.Thickness, ToThickness, soil.Water.LL15.Last());
        }

        /// <summary>
        /// Drained upper limit - mapped to the specified layer structure. Units: mm/mm
        /// </summary>
        /// <param name="soil">The soil.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        private static double[] DULMapped(Soil soil, double[] ToThickness)
        {
            return MapConcentration(soil.Water.DUL, soil.Water.Thickness, ToThickness, soil.Water.DUL.Last());
        }

        /// <summary>Return the last value that isn't a missing value.</summary>
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        private static double LastValue(double[] Values)
        {
            if (Values == null) return double.NaN;
            for (int i = Values.Length - 1; i >= 0; i--)
                if (!double.IsNaN(Values[i]))
                    return Values[i];
            return 0;
        }

        /// <summary>Return the second last value that isn't a missing value.</summary>
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        private static double SecondLastValue(double[] Values)
        {
            bool foundLastValue = false;
            if (Values == null) return double.NaN;
            for (int i = Values.Length - 1; i >= 0; i--)
            {
                if (!double.IsNaN(Values[i]))
                {
                    if (foundLastValue)
                        return Values[i];
                    else
                        foundLastValue = true;
                }
            }

            return 0;
        }
        #endregion
    }
}
