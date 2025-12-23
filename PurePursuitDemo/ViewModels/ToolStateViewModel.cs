using PurePursuitDemo.Utilities;

namespace PurePursuitDemo.ViewModels
{
    public enum EditorMode
    {
        DrawPath,
        PlaceVehicle,
        PickLookahead,
        PanZoom
    }

    public sealed class ToolStateViewModel : ObservableObject
    {
        private EditorMode _mode = EditorMode.DrawPath;

        public EditorMode Mode
        {
            get => _mode;
            set
            {
                if (SetProperty(ref _mode, value))
                {
                    Raise(nameof(IsDrawPathMode));
                    Raise(nameof(IsPlaceVehicleMode));
                    Raise(nameof(IsPickLookaheadMode));
                    Raise(nameof(IsPanMode));
                }
            }
        }

        public bool IsDrawPathMode
        {
            get => Mode == EditorMode.DrawPath;
            set { if (value) Mode = EditorMode.DrawPath; }
        }

        public bool IsPlaceVehicleMode
        {
            get => Mode == EditorMode.PlaceVehicle;
            set { if (value) Mode = EditorMode.PlaceVehicle; }
        }

        public bool IsPickLookaheadMode
        {
            get => Mode == EditorMode.PickLookahead;
            set { if (value) Mode = EditorMode.PickLookahead; }
        }

        public bool IsPanMode
        {
            get => Mode == EditorMode.PanZoom;
            set { if (value) Mode = EditorMode.PanZoom; }
        }
    }
}
