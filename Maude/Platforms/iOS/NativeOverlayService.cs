#if IOS || MACCATALYST
using System;
using System.Collections.Generic;
using CoreGraphics;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;
using UIKit;

namespace Maude;

internal sealed class NativeOverlayService : INativeOverlayService
{
    private static readonly nfloat OverlayWidth = 220;
    private static readonly nfloat OverlayHeight = 130;

    private readonly Dictionary<UIWindow, UIView> overlays = new();
    private readonly Dictionary<UIWindow, UIView> hosts = new();
    private readonly Dictionary<UIWindow, MaudeChartView> chartViews = new();
    private readonly Dictionary<UIWindow, IViewHandler> handlers = new();
    private readonly Dictionary<UIWindow, NSLayoutConstraint[]> positionConstraints = new();

    public bool IsVisible { get; private set; }

    public void Show(IMaudeDataSink dataSink, MaudeOverlayPosition position)
    {
        if (dataSink == null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var app = UIApplication.SharedApplication;
            foreach (var scene in app.ConnectedScenes)
            {
                if (scene is not UIWindowScene windowScene)
                {
                    continue;
                }

                foreach (var window in windowScene.Windows)
                {
                    if (window.RootViewController?.View == null)
                    {
                        continue;
                    }

                    EnsureOverlayForWindow(window, dataSink, position);
                }
            }

            IsVisible = true;
        });
    }

    private void EnsureOverlayForWindow(UIWindow window, IMaudeDataSink dataSink, MaudeOverlayPosition position)
    {
        var rootView = window.RootViewController!.View!;
        var overlay = GetOrCreateOverlay(window, rootView);
        var host = GetOrCreateHost(window, overlay);
        var chartView = GetOrCreateChart(window, host);
        if (chartView == null)
        {
            return;
        }

        chartView.DataSink = dataSink;
        chartView.RenderMode = MaudeChartRenderMode.Overlay;
        chartView.WindowDuration = TimeSpan.FromMinutes(1);

        UpdatePosition(window, overlay, host, position);

        overlay.Hidden = false;
        overlay.Frame = rootView.Bounds;
    }

    private UIView GetOrCreateOverlay(UIWindow window, UIView rootView)
    {
        if (overlays.TryGetValue(window, out var overlay))
        {
            return overlay;
        }

        overlay = new UIView(rootView.Bounds)
        {
            BackgroundColor = UIColor.Clear,
            Opaque = false,
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            UserInteractionEnabled = false
        };

        rootView.AddSubview(overlay);
        overlays[window] = overlay;
        return overlay;
    }

    private UIView GetOrCreateHost(UIWindow window, UIView overlay)
    {
        if (hosts.TryGetValue(window, out var host))
        {
            return host;
        }

        host = new UIView
        {
            TranslatesAutoresizingMaskIntoConstraints = false,
            BackgroundColor = UIColor.Clear,
            UserInteractionEnabled = false,
            LayoutMargins = UIEdgeInsets.Zero
        };

        overlay.AddSubview(host);
        NSLayoutConstraint.ActivateConstraints(new[]
        {
            host.WidthAnchor.ConstraintEqualTo(OverlayWidth),
            host.HeightAnchor.ConstraintEqualTo(OverlayHeight),
        });

        hosts[window] = host;
        return host;
    }

    private MaudeChartView? GetOrCreateChart(UIWindow window, UIView host)
    {
        if (chartViews.TryGetValue(window, out var chartView))
        {
            return chartView;
        }

        var mauiContext = Application.Current?.Handler?.MauiContext;
        if (mauiContext == null)
        {
            MaudeLogger.Error("Unable to resolve a MAUI context for overlay rendering.");
            return null;
        }

        chartView = new MaudeChartView
        {
            RenderMode = MaudeChartRenderMode.Overlay,
            WidthRequest = 360,
            HeightRequest = 240
        };

        var handler = chartView.ToHandler(mauiContext);
        if (handler?.PlatformView is not UIView platformView)
        {
            MaudeLogger.Error("Failed to obtain a platform view for the chart overlay.");
            return null;
        }

        platformView.TranslatesAutoresizingMaskIntoConstraints = false;
        platformView.UserInteractionEnabled = false;
        host.AddSubview(platformView);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            platformView.WidthAnchor.ConstraintEqualTo(OverlayWidth),
            platformView.HeightAnchor.ConstraintEqualTo(OverlayHeight),
            platformView.CenterXAnchor.ConstraintEqualTo(host.CenterXAnchor),
            platformView.CenterYAnchor.ConstraintEqualTo(host.CenterYAnchor),
        });

        chartViews[window] = chartView;
        handlers[window] = handler;
        return chartView;
    }

    private void UpdatePosition(UIWindow window, UIView overlay, UIView host, MaudeOverlayPosition position)
    {
        if (positionConstraints.TryGetValue(window, out var constraints))
        {
            NSLayoutConstraint.DeactivateConstraints(constraints);
        }

        var margin = 12;
        constraints = position switch
        {
            MaudeOverlayPosition.TopLeft => new[]
            {
                host.LeadingAnchor.ConstraintEqualTo(overlay.LeadingAnchor, margin),
                host.TopAnchor.ConstraintEqualTo(overlay.TopAnchor, margin)
            },
            MaudeOverlayPosition.TopRight => new[]
            {
                host.TrailingAnchor.ConstraintEqualTo(overlay.TrailingAnchor, -margin),
                host.TopAnchor.ConstraintEqualTo(overlay.TopAnchor, margin)
            },
            MaudeOverlayPosition.BottomLeft => new[]
            {
                host.LeadingAnchor.ConstraintEqualTo(overlay.LeadingAnchor, margin),
                host.BottomAnchor.ConstraintEqualTo(overlay.BottomAnchor, -margin)
            },
            _ => new[]
            {
                host.TrailingAnchor.ConstraintEqualTo(overlay.TrailingAnchor, -margin),
                host.BottomAnchor.ConstraintEqualTo(overlay.BottomAnchor, -margin)
            }
        };

        NSLayoutConstraint.ActivateConstraints(constraints);
        positionConstraints[window] = constraints;
    }

    public void Hide()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var overlay in overlays.Values)
            {
                overlay.Hidden = true;
            }

            foreach (var host in hosts.Values)
            {
                foreach (var child in host.Subviews)
                {
                    child.RemoveFromSuperview();
                }
            }

            foreach (var chart in chartViews.Values)
            {
                chart.Detach();
            }

            foreach (var handler in handlers.Values)
            {
                handler.DisconnectHandler();
            }

            chartViews.Clear();
            handlers.Clear();
            positionConstraints.Clear();

            IsVisible = false;
        });
    }
}
#endif
