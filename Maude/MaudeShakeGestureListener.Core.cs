using System;

namespace Maude;

/// <summary>
/// Shared shake gesture listener surface; platform partials provide sensor wiring and call <see cref="HandleShake"/>.
/// </summary>
internal partial class MaudeShakeGestureListener : IDisposable
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

        if (!EvaluateShakePredicate("enabling shake gesture"))
        {
            MaudeLogger.Info("Shake gesture predicate returned false; listener will remain idle until predicate allows it.");
        }

        isEnabled = true;
        OnEnable();
    }

    public void Disable()
    {
        if (!isEnabled)
        {
            return;
        }

        isEnabled = false;
        OnDisable();
    }

    internal void HandleShake()
    {
        if (!isEnabled)
        {
            return;
        }

        if (!EvaluateShakePredicate("processing shake gesture"))
        {
            return;
        }

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

    public void Dispose()
    {
        Disable();
    }

    partial void OnEnable();
    partial void OnDisable();
}
