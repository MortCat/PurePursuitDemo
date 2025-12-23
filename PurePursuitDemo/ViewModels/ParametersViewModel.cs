using PurePursuitDemo.Utilities;

namespace PurePursuitDemo.ViewModels
{
    public sealed class ParametersViewModel : ObservableObject
    {
        private double _lookaheadDistance = 1.0;
        private double _linearSpeed = 0.6;
        private double _trackWidth = 0.45;
        private double _wheelBase = 0.35;

        public double LookaheadDistance
        {
            get => _lookaheadDistance;
            set => SetProperty(ref _lookaheadDistance, value);
        }

        public double LinearSpeed
        {
            get => _linearSpeed;
            set => SetProperty(ref _linearSpeed, value);
        }

        public double TrackWidth
        {
            get => _trackWidth;
            set => SetProperty(ref _trackWidth, value);
        }

        public double WheelBase
        {
            get => _wheelBase;
            set => SetProperty(ref _wheelBase, value);
        }
    }
}
