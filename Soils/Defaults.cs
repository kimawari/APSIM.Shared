// -----------------------------------------------------------------------
// <copyright file="Defaults.cs" company="APSIM Initiative">
// Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace APSIM.Shared.Soils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using APSIM.Shared.Utilities;

    /// <summary>Implements the defaults as listed in the soil protocol.</summary>
    /// <remarks>
    /// A PROTOCOL FOR THE DEVELOPMENT OF SOIL PARAMETER VALUES FOR USE IN APSIM
    ///     Neal Dalgliesh, Zvi Hochman, Neil Huth and Dean Holzworth
    /// </remarks>
    public class Defaults
    {

        /// <summary>Fills in missing values where possible.</summary>
        /// <param name="soil">The soil.</param>
        public static void FillInMissingValues(Soil soil)
        {
            CheckAnalysisForMissingValues(soil);

            foreach (SoilCrop crop in soil.Water.Crops)
            {
                if (crop.XF == null)
                {
                    crop.XF = MathUtilities.CreateArrayOfValues(1.0, crop.Thickness.Length);
                    crop.XFMetadata = StringUtilities.CreateStringArray("Estimated", crop.Thickness.Length);
                }
                if (crop.KL == null)
                    FillInKLForCrop(crop);
            }
        }

        private static string[] cropNames = {"Wheat", "Oats",
                                             "Sorghum", "Barley", "Chickpea", "Mungbean", "Cotton", "Canola", 
                                             "PigeonPea", "Maize", "Cowpea", "Sunflower", "Fababean", "Lucerne",
                                             "Lupin", "Lentil", "Triticale", "Millet", "Soybean" };

        private static double[] defaultKLThickness = new double[] { 150, 300, 600, 900, 1200, 1500, 1800 };
        private static double[,] defaultKLs =  {{0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.03,   0.02},
                                                {0.06,   0.06,   0.06,   0.06,   0.06,   0.06,   0.06},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.00,   0.00},
                                                {0.10,   0.10,   0.10,   0.10,   0.09,   0.07,   0.05},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.05,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.10,   0.10,   0.08,   0.06,   0.04,   0.02,   0.01},
                                                {0.08,   0.08,   0.08,   0.08,   0.06,   0.04,   0.03},
                                                {0.10,   0.10,   0.10,   0.10,   0.09,   0.09,   0.09},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01},
                                                {0.07,   0.07,   0.07,   0.04,   0.02,   0.01,   0.01},
                                                {0.07,   0.07,   0.07,   0.05,   0.05,   0.04,   0.03},
                                                {0.06,   0.06,   0.06,   0.04,   0.04,   0.02,   0.01}};

        /// <summary>Fills in KL for crop.</summary>
        /// <param name="crop">The crop.</param>
        private static void FillInKLForCrop(SoilCrop crop)
        {
            int i = StringUtilities.IndexOfCaseInsensitive(cropNames, crop.Name);
            if (i != -1)
            {
                double[] KLs = GetRowOfArray(defaultKLs, i);

                double[] cumThickness = SoilUtilities.ToCumThickness(crop.Thickness);
                crop.KL = new double[crop.Thickness.Length];
                for (int l = 0; l < crop.Thickness.Length; l++)
                {
                    bool didInterpolate;
                    crop.KL[l] = MathUtilities.LinearInterpReal(cumThickness[l], defaultKLThickness, KLs, out didInterpolate);
                }
            }
        }

        /// <summary>Gets the row of a 2 dimensional array.</summary>
        /// <param name="array">The array.</param>
        /// <param name="row">The row index</param>
        /// <returns>The values in the specified row.</returns>
        private static double[] GetRowOfArray(double[,] array, int row)
        {
            List<double> values = new List<double>();
            for (int col = 0; col < array.GetLength(1); col++)
                values.Add(array[row, col]);

            return values.ToArray();
        }

        /// <summary>Checks the analysis for missing values.</summary>
        /// <param name="soil">The soil.</param>
        private static void CheckAnalysisForMissingValues(Soil soil)
        {
            for (int i = 0; i < soil.Analysis.Thickness.Length; i++)
            {
                if (soil.Analysis.CL != null && double.IsNaN(soil.Analysis.CL[i]))
                    soil.Analysis.CL[i] = 0;

                if (soil.Analysis.EC != null && double.IsNaN(soil.Analysis.EC[i]))
                    soil.Analysis.EC[i] = 0;

                if (soil.Analysis.ESP != null && double.IsNaN(soil.Analysis.ESP[i]))
                    soil.Analysis.ESP[i] = 0;

                if (soil.Analysis.PH != null && double.IsNaN(soil.Analysis.PH[i]))
                    soil.Analysis.PH[i] = 7;
            }
        }
    }
}
