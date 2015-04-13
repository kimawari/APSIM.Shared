using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APSIM.Shared.Soils
{
    /// <summary>
    /// A specification of soil water model constants and parameters.
    /// </summary>
    public class SoilWater
    {
        /// <summary>
        /// Gets or sets the summer cona.
        /// </summary>
        /// <value>
        /// The summer cona.
        /// </value>
        [Description("Summer Cona")]
        public double SummerCona { get; set; }
        /// <summary>
        /// Gets or sets the summer u.
        /// </summary>
        /// <value>
        /// The summer u.
        /// </value>
        [Description("Summer U")]
        public double SummerU { get; set; }
        /// <summary>
        /// Gets or sets the summer date.
        /// </summary>
        /// <value>
        /// The summer date.
        /// </value>
        [Description("Summer Date")]
        public string SummerDate { get; set; }
        /// <summary>
        /// Gets or sets the winter cona.
        /// </summary>
        /// <value>
        /// The winter cona.
        /// </value>
        [Description("Winter Cona")]
        public double WinterCona { get; set; }
        /// <summary>
        /// Gets or sets the winter u.
        /// </summary>
        /// <value>
        /// The winter u.
        /// </value>
        [Description("Winter U")]
        public double WinterU { get; set; }
        /// <summary>
        /// Gets or sets the winter date.
        /// </summary>
        /// <value>
        /// The winter date.
        /// </value>
        [Description("Winter Date")]
        public string WinterDate { get; set; }
        /// <summary>
        /// Gets or sets the diffus constant.
        /// </summary>
        /// <value>
        /// The diffus constant.
        /// </value>
        [Description("Diffusivity Constant")]
        public double DiffusConst { get; set; }
        /// <summary>
        /// Gets or sets the diffus slope.
        /// </summary>
        /// <value>
        /// The diffus slope.
        /// </value>
        [Description("Diffusivity Slope")]
        public double DiffusSlope { get; set; }
        /// <summary>
        /// Gets or sets the salb.
        /// </summary>
        /// <value>
        /// The salb.
        /// </value>
        [Description("Soil albedo")]
        public double Salb { get; set; }
        /// <summary>
        /// Gets or sets the c n2 bare.
        /// </summary>
        /// <value>
        /// The c n2 bare.
        /// </value>
        [Description("Bare soil runoff curve number")]
        public double CN2Bare { get; set; }
        /// <summary>
        /// Gets or sets the cn red.
        /// </summary>
        /// <value>
        /// The cn red.
        /// </value>
        [Description("Max. reduction in curve number due to cover")]
        public double CNRed { get; set; }
        /// <summary>
        /// Gets or sets the cn cov.
        /// </summary>
        /// <value>
        /// The cn cov.
        /// </value>
        [Description("Cover for max curve number reduction")]
        public double CNCov { get; set; }
        /// <summary>
        /// Gets or sets the slope.
        /// </summary>
        /// <value>
        /// The slope.
        /// </value>
        public double Slope { get; set; }
        /// <summary>
        /// Gets or sets the width of the discharge.
        /// </summary>
        /// <value>
        /// The width of the discharge.
        /// </value>
        [Description("Discharge width")]
        public double DischargeWidth { get; set; }
        /// <summary>
        /// Gets or sets the catchment area.
        /// </summary>
        /// <value>
        /// The catchment area.
        /// </value>
        [Description("Catchment area")]
        public double CatchmentArea { get; set; }
        /// <summary>
        /// Gets or sets the maximum pond.
        /// </summary>
        /// <value>
        /// The maximum pond.
        /// </value>
        [Description("Maximum pond")]
        public double MaxPond { get; set; }

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        /// <value>
        /// The thickness.
        /// </value>
        public double[] Thickness { get; set; }
        /// <summary>
        /// Gets or sets the swcon.
        /// </summary>
        /// <value>
        /// The swcon.
        /// </value>
        [Units("0-1")]
        public double[] SWCON { get; set; }
        /// <summary>
        /// Gets or sets the mwcon.
        /// </summary>
        /// <value>
        /// The mwcon.
        /// </value>
        [Units("0-1")]
        public double[] MWCON { get; set; }
        /// <summary>
        /// Gets or sets the klat.
        /// </summary>
        /// <value>
        /// The klat.
        /// </value>
        [Units("mm/day")]
        public double[] KLAT { get; set; }
    }
}
