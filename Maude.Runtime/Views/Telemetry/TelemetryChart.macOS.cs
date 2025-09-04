#if __MACCATALYST__
using System;
using CoreGraphics;
using Microsoft.Maui.Controls;
using SkiaSharp.Views.Maui.Controls;
using UIKit;

namespace Maude.Runtime.Views.Telemetry
{
	public partial class TelemetryChart
    {
        // Mutable as cannot setup via constructor
        private UIHoverGestureRecognizer chartHoverGesture;
        private UITapGestureRecognizer chartClickGesture;
        private UITapGestureRecognizer chartDoubleClickGesture;
        private UIPanGestureRecognizer chartPannedGesture;

        private CGPoint? clickStartPosition;

        public void InitialiseNative()
        {
            canvasView.HandlerChanged += CanvasView_HandlerChanged;
            canvasView.HandlerChanging += CanvasView_HandlerChanging;

            chartHoverGesture = new UIHoverGestureRecognizer(OnChartHoverGesture);
            chartClickGesture = new UITapGestureRecognizer(OnChartClick);
            chartDoubleClickGesture = new UITapGestureRecognizer(OnChartClick)
            {
                NumberOfTapsRequired = 2,
            };
            chartPannedGesture = new UIPanGestureRecognizer(OnChartPanned);
        }

        private void OnChartPanned(UIPanGestureRecognizer recognizer)
        {
            // TODO Drag selection here.
        }

        private void CanvasView_HandlerChanging(object sender, HandlerChangingEventArgs e)
        {
            var canvasView = sender as SKCanvasView;
            if (canvasView == null)
            {
                return;
            }

            var oldView = e.OldHandler?.PlatformView as UIKit.UIView;
            if (oldView != null)
            {
                oldView.RemoveGestureRecognizer(chartHoverGesture);
                oldView.RemoveGestureRecognizer(chartClickGesture);
                oldView.RemoveGestureRecognizer(chartDoubleClickGesture);
                oldView.RemoveGestureRecognizer(chartPannedGesture);
            }
        }

        private void CanvasView_HandlerChanged(object sender, EventArgs e)
        {
            var view = canvasView.Handler?.PlatformView as UIKit.UIView;

            if (view != null)
            {
                view.AddGestureRecognizer(chartHoverGesture);
                view.AddGestureRecognizer(chartClickGesture);
                view.AddGestureRecognizer(chartDoubleClickGesture);
                view.AddGestureRecognizer(chartPannedGesture);
            }
        }

        private void OnChartHoverGesture(UIHoverGestureRecognizer recognizer)
        {
            if (recognizer.State == UIGestureRecognizerState.Possible
                || recognizer.State == UIGestureRecognizerState.Recognized)
            {
                return;
            }

            var view = recognizer.View;
            var location = recognizer.LocationInView(view);

            var @event = ChartHoverEvent.Changed;
            if (recognizer.State == UIGestureRecognizerState.Began)
            {
                @event = ChartHoverEvent.Started;
            }
            else if (recognizer.State != UIGestureRecognizerState.Changed)
            {
                @event = ChartHoverEvent.Ended;
            }

            this.OnChartHover?.Invoke(this, new ChartHoverEventArgs(@event, new Point(location.X, location.Y)));
        }

        private void OnChartClick(UITapGestureRecognizer recognizer)
        {
            var canvasSize = this.canvasView.CanvasSize;

            var view = recognizer.View;
            var location = recognizer.LocationInView(view);
            renderer.CalculateDisplayAreaStartEndDateTimes(this.Position, this.TelemetryStartTimeUtc, TelemetryTimeDisplayMode, out var startUtc, out var endUtc);
            var position = renderer.ConvertToChartPosition(new Point(location.X, location.Y), (int)canvasSize.Width, (int)canvasSize.Height, startUtc, endUtc);

            if (position != null)
            {
                if (recognizer == this.chartClickGesture)
                {
                    OnChartClicked?.Invoke(this, new ChartClickedEventArgs(position.Value));
                }
                else if (recognizer == this.chartDoubleClickGesture)
                {
                    OnChartDoubleClicked?.Invoke(this, new ChartClickedEventArgs(position.Value));
                }
            }
        }

    }
}
#endif