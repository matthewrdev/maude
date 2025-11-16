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
    private FrameLayout? overlay;
    private FrameLayout? contentHost;
    private MaudeChartView? chartView;
    private IViewHandler? chartHandler;

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
            EnsureChartView(mauiContext, dataSink);
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

        overlay = new FrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
        };

        overlay.SetBackgroundColor(Color.Transparent);
        overlay.Clickable = false;
        overlay.Focusable = false;
        overlay.Touch += (_, _) => { }; // ensure we never handle touches

        contentHost = new FrameLayout(activity)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
        };

        contentHost.SetPadding(0, 0, 0, 0);

        overlay.AddView(contentHost);
        rootView.AddView(overlay);
    }

    private void EnsureChartView(IMauiContext mauiContext, IMaudeDataSink dataSink)
    {
        if (chartView == null)
            {
            chartView = new MaudeChartView
            {
                RenderMode = MaudeChartRenderMode.Overlay,
                WidthRequest = 320,
                HeightRequest = 200
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

                contentHost.RemoveAllViews();
                contentHost.AddView(platformView, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent));
            }
        }

        chartView!.DataSink = dataSink;
        chartView.WindowDuration = TimeSpan.FromMinutes(1);
    }

    private void UpdatePosition(Activity activity, MaudeOverlayPosition position)
    {
        if (overlay == null)
        {
            return;
        }

        var lp = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = ConvertGravity(position)
        };

        var margin = DpToPx(activity, 16);
        lp.SetMargins(margin, margin, margin, margin);
        overlay.LayoutParameters = lp;
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
