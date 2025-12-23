using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PurePursuitDemo.Models;
using PurePursuitDemo.ViewModels;

namespace PurePursuitDemo.Views
{
    public sealed class DemoCanvasControl : FrameworkElement
    {
        // camera (world->screen)
        private double _scale = 120.0; // px per meter
        private Vector _offset;        // screen offset
        private Point _lastMouse;
        private bool _isRightPanning;

        // editing
        private int _dragPointIndex = -1;
        private bool _dragYawHandle = false;

        private const double HitPx = 10.0;

        public DemoCanvasControl()
        {
            Focusable = true;
            Loaded += (_, __) =>
            {
                _offset = new Vector(ActualWidth * 0.5, ActualHeight * 0.55);
                HookVm();
                InvalidateVisual();
            };
            DataContextChanged += (_, __) =>
            {
                HookVm();
                InvalidateVisual();
            };

            SizeChanged += (_, __) => InvalidateVisual();

            MouseWheel += OnMouseWheel;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            if (vm.Canvas.ShowGrid) DrawGrid(dc);

            DrawPath(dc, vm);
            DrawVehicle(dc, vm);
            DrawLookahead(dc, vm);
            DrawOverlays(dc, vm);
        }

        // -----------------------
        // Rendering
        // -----------------------
        private void DrawGrid(DrawingContext dc)
        {
            // 1m grid
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(35, 120, 120, 120)), 1);
            pen.Freeze();

            // draw lines around visible world range
            var worldTL = ScreenToWorld(new Point(0, 0));
            var worldBR = ScreenToWorld(new Point(ActualWidth, ActualHeight));

            double minX = Math.Floor(Math.Min(worldTL.X, worldBR.X)) - 1;
            double maxX = Math.Ceiling(Math.Max(worldTL.X, worldBR.X)) + 1;
            double minY = Math.Floor(Math.Min(worldTL.Y, worldBR.Y)) - 1;
            double maxY = Math.Ceiling(Math.Max(worldTL.Y, worldBR.Y)) + 1;

            for (double x = minX; x <= maxX; x += 1.0)
            {
                var a = WorldToScreen(new Point2(x, minY));
                var b = WorldToScreen(new Point2(x, maxY));
                dc.DrawLine(pen, a, b);
            }

            for (double y = minY; y <= maxY; y += 1.0)
            {
                var a = WorldToScreen(new Point2(minX, y));
                var b = WorldToScreen(new Point2(maxX, y));
                dc.DrawLine(pen, a, b);
            }

            // axes
            var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 20, 20, 20)), 2);
            axisPen.Freeze();

            dc.DrawLine(axisPen, WorldToScreen(new Point2(minX, 0)), WorldToScreen(new Point2(maxX, 0)));
            dc.DrawLine(axisPen, WorldToScreen(new Point2(0, minY)), WorldToScreen(new Point2(0, maxY)));
        }

        private void DrawPath(DrawingContext dc, MainViewModel vm)
        {
            if (vm.Canvas.PathPoints.Count <= 0) return;

            var pen = new Pen(new SolidColorBrush(Color.FromRgb(239, 68, 68)), 3);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.LineJoin = PenLineJoin.Round;

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                var p0 = WorldToScreen(vm.Canvas.PathPoints[0]);
                g.BeginFigure(p0, false, false);
                for (int i = 1; i < vm.Canvas.PathPoints.Count; i++)
                    g.LineTo(WorldToScreen(vm.Canvas.PathPoints[i]), true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);

            // control points
            var cpFill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            var cpStroke = new Pen(new SolidColorBrush(Color.FromRgb(239, 68, 68)), 2);
            for (int i = 0; i < vm.Canvas.PathPoints.Count; i++)
            {
                var sp = WorldToScreen(vm.Canvas.PathPoints[i]);
                dc.DrawEllipse(cpFill, cpStroke, sp, 5.5, 5.5);
            }
        }

        private void DrawVehicle(DrawingContext dc, MainViewModel vm)
        {
            var v = vm.Canvas.Vehicle;
            var p = WorldToScreen(new Point2(v.X, v.Y));

            // vehicle body (rounded rect-like shape)
            double w = 0.45; // m (visual only)
            double h = 0.28;
            var halfW = w / 2.0;
            var halfH = h / 2.0;

            // create local rectangle corners -> world -> screen
            Point2[] rect =
            {
                LocalToWorld(v, new Point2( halfW,  halfH)),
                LocalToWorld(v, new Point2( halfW, -halfH)),
                LocalToWorld(v, new Point2(-halfW, -halfH)),
                LocalToWorld(v, new Point2(-halfW,  halfH)),
            };

            var pen = new Pen(new SolidColorBrush(Color.FromRgb(17, 24, 39)), 2);
            var fill = new SolidColorBrush(Color.FromRgb(229, 231, 235));

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(WorldToScreen(rect[0]), true, true);
                g.LineTo(WorldToScreen(rect[1]), true, false);
                g.LineTo(WorldToScreen(rect[2]), true, false);
                g.LineTo(WorldToScreen(rect[3]), true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(fill, pen, geo);

            // heading arrow
            var head = LocalToWorld(v, new Point2(0.35, 0));
            var headScreen = WorldToScreen(head);
            var arrowPen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 3);
            arrowPen.StartLineCap = PenLineCap.Round;
            arrowPen.EndLineCap = PenLineCap.Round;
            dc.DrawLine(arrowPen, p, headScreen);

            // yaw handle (small circle in front)
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                           null,
                           headScreen, 6, 6);
        }

        private void DrawLookahead(DrawingContext dc, MainViewModel vm)
        {
            var la = WorldToScreen(vm.Canvas.Lookahead);

            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                           new Pen(new SolidColorBrush(Color.FromRgb(6, 95, 70)), 2),
                           la, 6.5, 6.5);
        }

        private void DrawOverlays(DrawingContext dc, MainViewModel vm)
        {
            var r = vm.Canvas.Result;
            if (r == null || !r.IsValid) return;

            // line vehicle -> lookahead
            var v = vm.Canvas.Vehicle;
            var pV = WorldToScreen(new Point2(v.X, v.Y));
            var pLa = WorldToScreen(r.LookaheadWorld);

            var linkPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 245, 158, 11)), 2);
            linkPen.DashStyle = DashStyles.Dash;
            dc.DrawLine(linkPen, pV, pLa);

            // local axes
            if (vm.Canvas.ShowLocalAxes)
            {
                var xAxis = LocalToWorld(v, new Point2(0.8, 0));
                var yAxis = LocalToWorld(v, new Point2(0, 0.8));

                var px = WorldToScreen(xAxis);
                var py = WorldToScreen(yAxis);

                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(200, 37, 99, 235)), 2), pV, px);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(200, 16, 185, 129)), 2), pV, py);
            }

            // circle
            if (vm.Canvas.ShowCircle && !double.IsInfinity(r.CircleRadiusAbs) && !double.IsNaN(r.CircleCenterWorld.X))
            {
                var c = WorldToScreen(r.CircleCenterWorld);
                double radPx = r.CircleRadiusAbs * _scale;

                var cPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 55, 65, 81)), 2);
                cPen.DashStyle = DashStyles.DashDot;

                dc.DrawEllipse(null, cPen, c, radPx, radPx);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(55, 65, 81)), null, c, 4, 4);
            }

            // angle alpha visualization (simple arc around vehicle)
            if (vm.Canvas.ShowAngles)
            {
                DrawAlphaArc(dc, vm);
            }

            // text labels
            DrawLabel(dc, pLa + new Vector(10, -10), "Look-ahead", Color.FromRgb(16, 185, 129));
            DrawLabel(dc, pV + new Vector(10, 10), "Vehicle", Color.FromRgb(37, 99, 235));
        }

        private void DrawAlphaArc(DrawingContext dc, MainViewModel vm)
        {
            var r = vm.Canvas.Result!;
            var v = vm.Canvas.Vehicle;
            var origin = WorldToScreen(new Point2(v.X, v.Y));

            // arc radius in px
            double radPx = 50;

            // alpha defined in vehicle local frame between +x and vector to lookahead
            // We'll draw an arc in screen space by sampling.
            int steps = 24;
            double a0 = 0.0;
            double a1 = r.AlphaRad;

            var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 245, 158, 11)), 3);

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                // start direction in world = vehicle yaw
                for (int i = 0; i <= steps; i++)
                {
                    double t = i / (double)steps;
                    double a = a0 + (a1 - a0) * t;

                    // local direction rotated by vehicle yaw
                    double dirYaw = v.YawRad + a;

                    var ptWorld = new Point2(
                        v.X + Math.Cos(dirYaw) * (radPx / _scale),
                        v.Y + Math.Sin(dirYaw) * (radPx / _scale));

                    var pt = WorldToScreen(ptWorld);

                    if (i == 0) g.BeginFigure(pt, false, false);
                    else g.LineTo(pt, true, false);
                }
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);

            // alpha text
            string txt = $"α={r.AlphaRad * 180.0 / Math.PI:F1}°";
            DrawLabel(dc, origin + new Vector(8, -60), txt, Color.FromRgb(245, 158, 11));
        }

        private void DrawLabel(DrawingContext dc, Point p, string text, Color color)
        {
            var ft = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                12,
                new SolidColorBrush(color),
                1.25);

            dc.DrawText(ft, p);
        }

        // -----------------------
        // Interaction
        // -----------------------
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // zoom at cursor
            Point mouse = e.GetPosition(this);
            Point2 worldBefore = ScreenToWorld(mouse);

            double factor = e.Delta > 0 ? 1.10 : 1.0 / 1.10;
            _scale = Math.Clamp(_scale * factor, 40.0, 400.0);

            Point2 worldAfter = ScreenToWorld(mouse);

            // adjust offset to keep the same world point under mouse
            var sBefore = WorldToScreen(worldBefore);
            var sAfter = WorldToScreen(worldAfter);
            _offset += (Vector)(mouse - sAfter);

            InvalidateVisual();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _lastMouse = e.GetPosition(this);

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            if (e.ChangedButton == MouseButton.Right)
            {
                _isRightPanning = true;
                CaptureMouse();
                return;
            }

            if (e.ChangedButton != MouseButton.Left) return;

            // left button: depends on mode
            switch (vm.Tool.Mode)
            {
                case EditorMode.DrawPath:
                    {
                        // hit test point
                        _dragPointIndex = HitTestControlPoint(vm, _lastMouse);
                        if (_dragPointIndex < 0)
                        {
                            var w = ScreenToWorld(_lastMouse);
                            vm.Canvas.PathPoints.Add(w);
                            vm.Recompute();
                        }
                        CaptureMouse();
                        break;
                    }

                case EditorMode.PlaceVehicle:
                    {
                        // check yaw handle hit
                        if (HitTestYawHandle(vm, _lastMouse))
                        {
                            _dragYawHandle = true;
                            CaptureMouse();
                            break;
                        }

                        var w = ScreenToWorld(_lastMouse);
                        vm.Canvas.Vehicle.X = w.X;
                        vm.Canvas.Vehicle.Y = w.Y;
                        vm.Recompute();
                        CaptureMouse();
                        break;
                    }

                case EditorMode.PickLookahead:
                    {
                        var w = ScreenToWorld(_lastMouse);
                        vm.Canvas.Lookahead = w;
                        vm.Recompute();
                        InvalidateVisual();
                        break;
                    }

                case EditorMode.PanZoom:
                    {
                        // left does nothing; use right drag + wheel
                        break;
                    }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            var mouse = e.GetPosition(this);

            if (_isRightPanning && e.RightButton == MouseButtonState.Pressed)
            {
                Vector d = mouse - _lastMouse;
                _offset += d;
                _lastMouse = mouse;
                InvalidateVisual();
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _lastMouse = mouse;
                return;
            }

            // dragging
            if (vm.Tool.Mode == EditorMode.DrawPath && _dragPointIndex >= 0)
            {
                var w = ScreenToWorld(mouse);
                vm.Canvas.PathPoints[_dragPointIndex] = w;
                vm.Recompute();
                InvalidateVisual();
                return;
            }

            if (vm.Tool.Mode == EditorMode.PlaceVehicle && _dragYawHandle)
            {
                // yaw from vehicle pos to mouse
                var v = vm.Canvas.Vehicle;
                var w = ScreenToWorld(mouse);

                double dx = w.X - v.X;
                double dy = w.Y - v.Y;
                v.YawRad = Math.Atan2(dy, dx);

                vm.Recompute();
                InvalidateVisual();
                return;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                _isRightPanning = false;

            if (e.ChangedButton == MouseButton.Left)
            {
                _dragPointIndex = -1;
                _dragYawHandle = false;
            }

            ReleaseMouseCapture();
        }

        private int HitTestControlPoint(MainViewModel vm, Point screen)
        {
            for (int i = 0; i < vm.Canvas.PathPoints.Count; i++)
            {
                var sp = WorldToScreen(vm.Canvas.PathPoints[i]);
                if ((sp - screen).Length <= HitPx) return i;
            }
            return -1;
        }

        private bool HitTestYawHandle(MainViewModel vm, Point screen)
        {
            var v = vm.Canvas.Vehicle;
            var handleWorld = LocalToWorld(v, new Point2(0.35, 0));
            var handleScreen = WorldToScreen(handleWorld);
            return (handleScreen - screen).Length <= HitPx + 2;
        }

        // -----------------------
        // Coord transforms
        // -----------------------
        private Point WorldToScreen(Point2 w)
        {
            // screen x = x*scale + offsetX
            // screen y = -y*scale + offsetY
            double sx = w.X * _scale + _offset.X;
            double sy = -w.Y * _scale + _offset.Y;
            return new Point(sx, sy);
        }

        private Point2 ScreenToWorld(Point s)
        {
            double x = (s.X - _offset.X) / _scale;
            double y = -(s.Y - _offset.Y) / _scale;
            return new Point2(x, y);
        }

        private static Point2 LocalToWorld(Pose2 pose, Point2 local)
        {
            double c = Math.Cos(pose.YawRad);
            double s = Math.Sin(pose.YawRad);

            double x = pose.X + (c * local.X - s * local.Y);
            double y = pose.Y + (s * local.X + c * local.Y);
            return new Point2(x, y);
        }

        // -----------------------
        // VM hook for redraw
        // -----------------------
        private void HookVm()
        {
            if (DataContext is not MainViewModel vm) return;

            if (vm.Canvas.PathPoints is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += (_, __) => InvalidateVisual();
            }

            vm.PropertyChanged += (_, __) => InvalidateVisual();
            vm.Canvas.PropertyChanged += (_, __) => InvalidateVisual();
            vm.Params.PropertyChanged += (_, __) => InvalidateVisual();
            vm.Tool.PropertyChanged += (_, __) => InvalidateVisual();
        }
    }
}
