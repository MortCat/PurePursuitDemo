using System;
using PurePursuitDemo.Models;

namespace PurePursuitDemo.Services
{
    public sealed class PurePursuitSolver
    {
        public PurePursuitResult Solve(
            Pose2 vehicle,
            Point2 lookaheadWorld,
            double configuredLd,
            double v,
            double trackWidth,
            double wheelBase)
        {
            // Ld: use UI-configured lookahead distance for teaching.
            // If the picked lookahead point is not exactly Ld away, we keep using configuredLd
            // and show a hint.
            double dx = lookaheadWorld.X - vehicle.X;
            double dy = lookaheadWorld.Y - vehicle.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double Ld = Math.Max(configuredLd, 1e-6);

            // world -> local (vehicle frame, +x forward)
            double cy = Math.Cos(vehicle.YawRad);
            double sy = Math.Sin(vehicle.YawRad);

            double xLocal = cy * dx + sy * dy;
            double yLocal = -sy * dx + cy * dy;

            double alpha = Math.Atan2(yLocal, xLocal);

            // recommended curvature form
            double kappa = 2.0 * Math.Sin(alpha) / Ld;

            var res = new PurePursuitResult
            {
                IsValid = true,
                LookaheadWorld = lookaheadWorld,
                LookaheadLocal = new Point2(xLocal, yLocal),
                AlphaRad = alpha,
                Kappa = kappa,
            };

            if (Math.Abs(kappa) < 1e-9)
            {
                res.RadiusR = double.PositiveInfinity;
                res.CircleRadiusAbs = double.PositiveInfinity;
                res.CircleCenterWorld = new Point2(double.NaN, double.NaN);
                res.Omega = 0.0;
                res.VLeft = v;
                res.VRight = v;
                res.DeltaAckermannRad = 0.0;
                res.Warning = "kappa≈0 → 幾乎直行（圓半徑趨近無限大）";
                if (Math.Abs(dist - configuredLd) > 1e-3)
                    res.Warning += $"\n提示：你選的 lookahead 與車距離為 {dist:F3}m，與 Ld={configuredLd:F3}m 不一致。";
                return res;
            }

            double R = 1.0 / kappa; // signed
            res.RadiusR = R;
            res.CircleRadiusAbs = Math.Abs(R);

            // circle center in local: (0, R) then local->world
            // local->world: [x;y] = rot(yaw)*[xl;yl] + [vx;vy]
            double cxLocal = 0.0;
            double cyLocal = R;

            double cxWorld = vehicle.X + (cy * cxLocal - sy * cyLocal);
            double cyWorld = vehicle.Y + (sy * cxLocal + cy * cyLocal);
            res.CircleCenterWorld = new Point2(cxWorld, cyWorld);

            // diff-drive outputs
            double omega = v * kappa;
            res.Omega = omega;
            res.VLeft = v - omega * (trackWidth / 2.0);
            res.VRight = v + omega * (trackWidth / 2.0);

            // teaching delta
            res.DeltaAckermannRad = Math.Atan(wheelBase * kappa);

            // extra hint
            if (xLocal < 0.0)
                res.Warning = "lookahead 在車後方（x_local<0），Pure Pursuit 可能不穩，建議選前方點或用弧長取點。";

            if (string.IsNullOrWhiteSpace(res.Warning) && Math.Abs(dist - configuredLd) > 1e-3)
                res.Warning = $"提示：你選的 lookahead 與車距離為 {dist:F3}m，與 Ld={configuredLd:F3}m 不一致。";

            return res;
        }
    }
}
