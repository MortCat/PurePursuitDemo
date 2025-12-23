using PurePursuitDemo.Models;

namespace PurePursuitDemo.Models
{
    public sealed class PurePursuitResult
    {
        public bool IsValid { get; set; }
        public string Warning { get; set; } = "";

        public Point2 LookaheadWorld { get; set; }
        public Point2 LookaheadLocal { get; set; }

        public double AlphaRad { get; set; }
        public double Kappa { get; set; }
        public double RadiusR { get; set; } // signed

        public double Omega { get; set; }
        public double VLeft { get; set; }
        public double VRight { get; set; }

        // teaching aid
        public double DeltaAckermannRad { get; set; }

        public Point2 CircleCenterWorld { get; set; } // for drawing
        public double CircleRadiusAbs { get; set; }
    }
}
