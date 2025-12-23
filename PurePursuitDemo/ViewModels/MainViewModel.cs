using System.ComponentModel;
using PurePursuitDemo.Models;
using PurePursuitDemo.Services;
using PurePursuitDemo.Utilities;

namespace PurePursuitDemo.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        public ToolStateViewModel Tool { get; } = new();
        public ParametersViewModel Params { get; } = new();
        public CanvasViewModel Canvas { get; } = new();

        private readonly PurePursuitSolver _solver = new();

        public RelayCommand ClearAllCommand { get; }

        public MainViewModel()
        {
            ClearAllCommand = new RelayCommand(() =>
            {
                Canvas.PathPoints.Clear();
                // Re-assign objects to guarantee PropertyChanged for bindings
                Canvas.Vehicle = new Pose2 { X = 0.0, Y = 0.0, YawRad = 0.0 };
                Canvas.Lookahead = new Point2(1.0, 0.0);
                Recompute();
            });

            // recompute whenever params/canvas changes (basic wiring)
            Params.PropertyChanged += (_, __) => Recompute();
            Canvas.PropertyChanged += CanvasOnPropertyChanged;

            // initial state
            Recompute();
        }

        private void CanvasOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // avoid infinite loops; recompute only when inputs changed
            if (e.PropertyName is nameof(CanvasViewModel.Result) or nameof(CanvasViewModel.ResultText))
                return;

            Recompute();
        }

        public void Recompute()
        {
            var r = _solver.Solve(
                Canvas.Vehicle,
                Canvas.Lookahead,
                Params.LookaheadDistance,
                Params.LinearSpeed,
                Params.TrackWidth,
                Params.WheelBase);

            Canvas.Result = r;
        }
    }
}
