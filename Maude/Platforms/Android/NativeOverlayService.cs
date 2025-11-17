#if ANDROID
using System;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using Application = Microsoft.Maui.Controls.Application;
using Color = Android.Graphics.Color;
using View = Android.Views.View;

namespace Maude;

internal sealed class NativeOverlayService : INativeOverlayService
{
    private PassthroughFrameLayout? overlay;
    private PassthroughFrameLayout? contentHost;
    private MaudeChartView? chartView;
    private IViewHandler? chartHandler;
    private int overlayWidthPx;
    private int overlayHeightPx;

    public bool IsVisible { get; private set; }

    public void Show(IMaudeDataSink dataSink, MaudeOverlayPosition position)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null || dataSink == null)
        {
            return;
        }

        activity.RunOnUiThread(() =>
        {
            var rootView = activity.Window?.DecorView as ViewGroup;
            var mauiContext = Application.Current?.Handler?.MauiContext;

            if (rootView == null || mauiContext == null)
            {
                return;
            }

            EnsureOverlay(activity, rootView);
            EnsureChartView(activity, mauiContext, dataSink);
            UpdatePosition(activity, position);

            overlay!.Visibility = ViewStates.Visible;
            IsVisible = true;
        });
    }

    private void EnsureOverlay(Activity activity, ViewGroup rootView)
    {
        if (overlay != null)
        {
            return;
        }

        overlayWidthPx = DpToPx(activity, 320);
        overlayHeightPx = DpToPx(activity, 180);

        overlay = new PassthroughFrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        overlay.SetBackgroundColor(Color.Transparent);
        overlay.Clickable = false;
        overlay.Focusable = false;
        overlay.Touch += (_, _) => { }; // ensure we never handle touches

        contentHost = new PassthroughFrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                overlayWidthPx,
                overlayHeightPx)
        };

        contentHost.SetPadding(0, 0, 0, 0);

        overlay.AddView(contentHost);
        rootView.AddView(overlay);
    }

    private void EnsureChartView(Activity activity, IMauiContext mauiContext, IMaudeDataSink dataSink)
    {
        if (chartView == null)
            {
            chartView = new MaudeChartView
            {
                RenderMode = MaudeChartRenderMode.Overlay,
                WidthRequest = overlayWidthPx,
                HeightRequest = overlayHeightPx
            };

            chartView.InputTransparent = true;
            chartView.IsEnabled = false;
            chartHandler = chartView.ToHandler(mauiContext);

            if (chartHandler?.PlatformView is View platformView && contentHost != null)
            {
                if (platformView.Parent is ViewGroup existingParent)
                {
                    existingParent.RemoveView(platformView);
                }

                platformView.Clickable = false;
                platformView.Focusable = false;
                platformView.FocusableInTouchMode = false;
                platformView.Touch += (_, _) => { };

                var lp = platformView.LayoutParameters as FrameLayout.LayoutParams
                         ?? new FrameLayout.LayoutParams(
                             overlayWidthPx,
                             overlayHeightPx);

                lp.Width = overlayWidthPx;
                lp.Height = overlayHeightPx;

                contentHost.RemoveAllViews();
                contentHost.AddView(platformView, lp);
            }
        }

        chartView!.DataSink = dataSink;
        chartView.WindowDuration = TimeSpan.FromMinutes(1);
    }

    private void UpdatePosition(Activity activity, MaudeOverlayPosition position)
    {
        if (contentHost == null)
        {
            return;
        }

        var lp = contentHost.LayoutParameters as FrameLayout.LayoutParams
                 ?? new FrameLayout.LayoutParams(
                     ViewGroup.LayoutParams.WrapContent,
                     ViewGroup.LayoutParams.WrapContent);

        lp.Gravity = ConvertGravity(position);

        var margin = DpToPx(activity, 16);
        lp.SetMargins(margin, margin, margin, margin);
        contentHost.LayoutParameters = lp;
    }

    private static int DpToPx(Context context, float dp) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources.DisplayMetrics);

    private static GravityFlags ConvertGravity(MaudeOverlayPosition position) => position switch
    {
        MaudeOverlayPosition.TopLeft => GravityFlags.Top | GravityFlags.Left,
        MaudeOverlayPosition.TopRight => GravityFlags.Top | GravityFlags.Right,
        MaudeOverlayPosition.BottomLeft => GravityFlags.Bottom | GravityFlags.Left,
        _ => GravityFlags.Bottom | GravityFlags.Right
    };

    public void Hide()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null || overlay == null)
        {
            return;
        }

        activity.RunOnUiThread(() =>
        {
            overlay!.Visibility = ViewStates.Gone;
            CleanupChart();
            IsVisible = false;
        });
    }

    private void CleanupChart()
    {
        if (contentHost != null)
        {
            contentHost.RemoveAllViews();
        }

        chartView?.Detach();
        chartView = null;

        chartHandler?.DisconnectHandler();
        chartHandler = null;
    }
}
#endif

#if ANDROID
internal sealed class PassthroughFrameLayout : FrameLayout
{
    public PassthroughFrameLayout(Context context) : base(context)
    {
    }

    public override bool DispatchTouchEvent(MotionEvent? e) => false;

    public override bool OnInterceptTouchEvent(MotionEvent? ev) => false;

    public override bool OnTouchEvent(MotionEvent? e) => false;
}
#endif
