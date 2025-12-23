using System;
using System.Collections.ObjectModel;
using System.Text;
using PurePursuitDemo.Models;
using PurePursuitDemo.Utilities;

namespace PurePursuitDemo.ViewModels
{
    public sealed class CanvasViewModel : ObservableObject
    {
        public ObservableCollection<Point2> PathPoints { get; } = new();

        private Pose2 _vehicle = new() { X = 0.0, Y = 0.0, YawRad = 0.0 };
        public Pose2 Vehicle
        {
            get => _vehicle;
            set => SetProperty(ref _vehicle, value);
        }

        private Point2 _lookahead = new(1.0, 0.0);
        public Point2 Lookahead
        {
            get => _lookahead;
            set => SetProperty(ref _lookahead, value);
        }

        private PurePursuitResult? _result;
        public PurePursuitResult? Result
        {
            get => _result;
            set
            {
                if (SetProperty(ref _result, value))
                    Raise(nameof(ResultText));
            }
        }

        // overlay toggles
        private bool _showGrid = true;
        public bool ShowGrid { get => _showGrid; set => SetProperty(ref _showGrid, value); }

        private bool _showCircle = true;
        public bool ShowCircle { get => _showCircle; set => SetProperty(ref _showCircle, value); }

        private bool _showAngles = true;
        public bool ShowAngles { get => _showAngles; set => SetProperty(ref _showAngles, value); }

        private bool _showLocalAxes = true;
        public bool ShowLocalAxes { get => _showLocalAxes; set => SetProperty(ref _showLocalAxes, value); }

        public string ResultText
        {
            get
            {
                if (Result == null) return "尚未計算（請畫路徑、放車、選 look-ahead）";

                var sb = new StringBuilder();
                sb.AppendLine($"Lookahead (world): ({Result.LookaheadWorld.X:F3}, {Result.LookaheadWorld.Y:F3})");
                sb.AppendLine($"Lookahead (local): ({Result.LookaheadLocal.X:F3}, {Result.LookaheadLocal.Y:F3})");
                sb.AppendLine($"alpha: {Result.AlphaRad * 180.0 / Math.PI:F2} deg");
                sb.AppendLine($"kappa: {Result.Kappa:F5}  (1/m)");
                sb.AppendLine($"R: {(double.IsInfinity(Result.RadiusR) ? "∞" : Result.RadiusR.ToString("F3"))} m");
                sb.AppendLine($"omega: {Result.Omega:F3} rad/s");
                sb.AppendLine($"vL / vR: {Result.VLeft:F3} / {Result.VRight:F3} m/s");
                sb.AppendLine($"delta(teach): {Result.DeltaAckermannRad * 180.0 / Math.PI:F2} deg");
                if (!string.IsNullOrWhiteSpace(Result.Warning))
                    sb.AppendLine($"⚠ {Result.Warning}");
                return sb.ToString();
            }
        }
    }
}
