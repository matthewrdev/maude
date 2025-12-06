using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Maude;

internal sealed class MaudeShakeGestureListener : IDisposable
{
    private readonly IMaudeRuntime runtime;
    private readonly MaudeOptions options;
    private bool isEnabled;

    public MaudeShakeGestureListener(IMaudeRuntime runtime, MaudeOptions options)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void Enable()
    {
        if (isEnabled)
        {
            return;
        }
        
        if (!options.AllowShakeGesture)
        {
            return;
        }

        var accelerometer = Accelerometer.Default;
        if (accelerometer == null || !accelerometer.IsSupported)
        {
            MaudeLogger.Warning("Shake gesture is not supported on this device.");
            return;
        }

        var predicateAllows = EvaluateShakePredicate("enabling shake gesture");
        if (!predicateAllows)
        {
            MaudeLogger.Info("Shake gesture predicate returned false; listener will remain idle until predicate allows it.");
        }

        try
        {
            accelerometer.ShakeDetected -= OnShakeDetected;
            accelerometer.ShakeDetected += OnShakeDetected;

            if (!accelerometer.IsMonitoring)
            {
                accelerometer.Start(SensorSpeed.UI);
            }
            
            isEnabled = true;
        }
        catch (FeatureNotSupportedException)
        {
            MaudeLogger.Warning("Shake gesture is not supported on this device.");
        }
        catch (Exception ex)
        {
            MaudeLogger.Exception(ex);
        }
    }

    public void Disable()
    {
        if (!isEnabled)
        {
            return;
        }

        var accelerometer = Accelerometer.Default;
        try
        {
            if (accelerometer != null)
            {
                accelerometer.ShakeDetected -= OnShakeDetected;

                if (accelerometer.IsMonitoring)
                {
                    accelerometer.Stop();
                }
            }
        }
        catch (Exception ex)
        {
            MaudeLogger.Exception(ex);
        }
        finally
        {
            isEnabled = false;
        }
    }

    private void OnShakeDetected(object? sender, EventArgs e)
    {
        if (!EvaluateShakePredicate("processing shake gesture"))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (options.ShakeGestureBehaviour)
            {
                case MaudeShakeGestureBehaviour.SlideSheet:
                    if (runtime.IsSheetPresented)
                    {
                        runtime.DismissSheet();
                    }
                    else
                    {
                        runtime.PresentSheet();
                    }
                    break;
                
                case MaudeShakeGestureBehaviour.Overlay:
                    if (runtime.IsOverlayPresented)
                    {
                        runtime.DismissOverlay();
                    }
                    else
                    {
                        runtime.PresentOverlay();
                    }
                    break;
            }
        });
    }

    public void Dispose()
    {
        Disable();
    }

    private bool EvaluateShakePredicate(string context)
    {
        if (options.ShakeGesturePredicate == null)
        {
            return true;
        }

        try
        {
            return options.ShakeGesturePredicate();
        }
        catch (Exception ex)
        {
            MaudeLogger.Error($"Shake gesture predicate threw while {context}.");
            MaudeLogger.Exception(ex);
            return false;
        }
    }
}
