using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace Maude;

internal class MaudeRuntimeImpl : IMaudeRuntime
{
    private readonly MaudeOptions options;
    private readonly SemaphoreSlim presentationSemaphore = new SemaphoreSlim(1, 1);
    private WeakReference<IMaudePopup> presentedMaudeViewReference;
    
    private readonly SemaphoreSlim chartOverlaySemaphore = new SemaphoreSlim(1, 1);
    private WeakReference<MaudeChartWindowOverlay> chartOverlayReference;
    
    private MemorySamplerThread samplerThread;
    private readonly Lock samplerLock = new Lock();
    
    private readonly MaudeMutableDataSink MutableDataSink;
    
    private readonly MaudeShakeGestureListener shakeGestureListener;

    public IMaudeDataSink DataSink => MutableDataSink;

    public MaudeRuntimeImpl(MaudeOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        MaudeLogger.Info($"Creating runtime with sample frequency {options.SampleFrequencyMilliseconds}ms, retention {options.RetentionPeriodSeconds}s, shake gesture allowed: {options.AllowShakeGesture}, additional channels: {options.AdditionalChannels?.Count ?? 0}.");
        MutableDataSink = new MaudeMutableDataSink(options);
        MaudeLogger.Info("Mutable data sink created.");
        shakeGestureListener = new MaudeShakeGestureListener(this, options);
        shakeGestureListener.Enable();
        MaudeLogger.Info("Shake gesture listener initialised and enabled.");
    }
    
    public bool IsActive { get; private set; }

    public bool IsSheetPresented => presentedMaudeViewReference != null && presentedMaudeViewReference.TryGetTarget(out _);

    public bool IsPresentationEnabled => true;
    
    public bool IsOverlayPresented => chartOverlayReference != null && chartOverlayReference.TryGetTarget(out _);
    
    public event EventHandler? OnActivated;
    
    public event EventHandler? OnDeactivated;

    public void Activate()
    {
        MaudeLogger.Info("Activate requested.");
        lock (samplerLock)
        {
            if (IsActive)
            {
                MaudeLogger.Info("Activate requested but runtime is already active.");
                return;
            }
            
            samplerThread = new MemorySamplerThread(options.SampleFrequencyMilliseconds, snapshot =>
            {
                MutableDataSink.RecordMemorySnapshot(snapshot);
            });
            
            IsActive = true;
            MaudeLogger.Info($"Memory sampler started with frequency {options.SampleFrequencyMilliseconds}ms.");
        }

        OnActivated?.Invoke(this, EventArgs.Empty);
        MaudeLogger.Info("Activation complete and event dispatched.");
    }

    public void Deactivate()
    {
        MaudeLogger.Info("Deactivate requested.");
        lock (samplerLock)
        {
            if (!IsActive)
            {
                MaudeLogger.Info("Deactivate requested but runtime is already inactive.");
                return;
            }

            samplerThread?.Dispose();
            samplerThread = null;
            IsActive = false;
            MaudeLogger.Info("Memory sampler disposed and activity flag cleared.");
        }

        OnDeactivated?.Invoke(this, EventArgs.Empty);
        MaudeLogger.Info("Deactivation complete and event dispatched.");
    }

    public void PresentSheet()
    {
        if (!IsPresentationEnabled)
        {
            MaudeLogger.Warning("Presentation requested while presentation is disabled.");
            return;
        }

        if (IsSheetPresented)
        {
            MaudeLogger.Info("Presentation requested but sheet is already showing.");
            return;
        }

        MaudeLogger.Info("Queueing sheet presentation on main thread.");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await presentationSemaphore.WaitAsync();
            try
            {
                if (IsSheetPresented)
                {
                    MaudeLogger.Info("Sheet presentation skipped because it is already showing after acquiring the semaphore.");
                    return;
                }

                var maudeView = new MaudeView();
                
                var popupView = await CreateAndOpenPopup(maudeView);
                WirePopupLifecycle(popupView, maudeView);
                this.presentedMaudeViewReference = new  WeakReference<IMaudePopup>(popupView);
            }
            catch (Exception ex)
            {
                MaudeLogger.Error("Failed to present Maude sheet.");
                MaudeLogger.Exception(ex);
                this.presentedMaudeViewReference = null;
            }
            finally
            {
                presentationSemaphore.Release();
            }
        });
    }
    
    
    private async Task<IMaudePopup> CreateAndOpenPopup(MaudeView maudeView)
    {
        if (maudeView == null)
        {
            throw new ArgumentNullException(nameof(maudeView));
        }

        maudeView.HorizontalOptions = LayoutOptions.Fill;
        maudeView.VerticalOptions = LayoutOptions.Fill;

        var mauiContext = Application.Current?.Handler?.MauiContext;
        maudeView.BackgroundColor = Colors.WhiteSmoke;
        
#if ANDROID
        MaudeLogger.Info("Creating Android popup for Maude view.");
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var handler = maudeView.ToHandler(mauiContext);
        var platformView = handler.PlatformView;
        
        var themedCtx = new AndroidX.AppCompat.View.ContextThemeWrapper(activity, Resource.Style.Maui_MainTheme);

        var popup = new MaudePopup(themedCtx);
        popup.PopupView = maudeView;
        popup.SetContentView(platformView);

        popup.Show();

#elif IOS
        MaudeLogger.Info("Creating iOS popup for Maude view.");
        
        var window = UIKit.UIApplication.SharedApplication
                         ?.ConnectedScenes
                         ?.OfType<UIKit.UIWindowScene>()
                         ?.SelectMany(scene => scene.Windows)
                         ?.FirstOrDefault(w => w.IsKeyWindow)    // visible/active window
                     ?? UIKit.UIApplication.SharedApplication?.Windows?.FirstOrDefault();

        var rootController = window?.RootViewController?.PresentedViewController
                             ?? window?.RootViewController;

        if (rootController == null)
        {
            MaudeLogger.Error("Unable to locate the root controller for iOS popup presentation.");
            throw new InvalidOperationException();
        }

        var handler = maudeView.ToHandler(mauiContext);
        var platformView = handler.PlatformView as UIKit.UIView;
        
        var popup = new MaudePopup(maudeView, platformView, rootController);

        popup.Show();
#endif

        return popup;
    }

    private void WirePopupLifecycle(IMaudePopup popup, MaudeView maudeView)
    {
        if (popup == null)
        {
            MaudeLogger.Warning("WirePopupLifecycle invoked with a null popup.");
            return;
        }
        
        if (maudeView == null)
        {
            MaudeLogger.Warning("WirePopupLifecycle invoked without a MaudeView instance to unbind.");
        }

        MaudeLogger.Info("Wiring popup lifecycle handlers.");
        void Handler(object sender, EventArgs args)
        {
            popup.OnClosed -= Handler;
            MaudeLogger.Info("Maude popup closed; cleaning up references.");
            
            try
            {
                maudeView?.UnbindRuntime();
            }
            catch (Exception ex)
            {
                MaudeLogger.Warning("Failed to unbind runtime when popup closed.");
                MaudeLogger.Exception(ex);
            }
            
            if (ReferenceEquals(presentedMaudeViewReference, null))
            {
                return;
            }
            
            presentedMaudeViewReference = null;

            if (popup is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        popup.OnClosed += Handler;
        MaudeLogger.Info("Popup lifecycle wired.");
    }

    public void DismissSheet()
    {
        MaudeLogger.Info("DismissSheet requested.");
        if (!IsSheetPresented)
        {
            MaudeLogger.Info("DismissSheet skipped because no sheet is currently presented.");
            return;
        }

        MaudeLogger.Info("Queueing sheet dismissal on main thread.");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await presentationSemaphore.WaitAsync();
            try
            {
                if (!IsSheetPresented)
                {
                    MaudeLogger.Info("DismissSheet skipped after acquiring semaphore because no sheet is presented.");
                    return;
                }

                if (this.presentedMaudeViewReference.TryGetTarget(out var view))
                {
                    MaudeLogger.Info("Closing presented Maude sheet.");
                    view.Close();
                }

                presentedMaudeViewReference = null;
                MaudeLogger.Info("Sheet dismissal complete and references cleared.");
            }
            catch (Exception ex)
            {
                MaudeLogger.Error("Failed to dismiss Maude sheet.");
                MaudeLogger.Exception(ex);
            }
            finally
            {
                presentationSemaphore.Release();
            }
        });
    }

    public void PresentOverlay(MaudeOverlayPosition position = MaudeOverlayPosition.TopRight)
    {
        MaudeLogger.Info($"PresentOverlay requested at position {position}.");
        if (!IsPresentationEnabled)
        {
            MaudeLogger.Warning("Overlay presentation requested while presentation is disabled.");
            return;
        }

        MaudeLogger.Info("Queueing overlay presentation on main thread.");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await chartOverlaySemaphore.WaitAsync();
            try
            {
                if (IsOverlayPresented && chartOverlayReference.TryGetTarget(out var existingOverlay))
                {
                    MaudeLogger.Info("Overlay already presented; updating position.");
                    existingOverlay.UpdatePosition(position);
                    return;
                }

                var window = Application.Current?.Windows?.FirstOrDefault();
                if (window == null)
                {
                    MaudeLogger.Warning("Overlay presentation skipped because no application window was found.");
                    return;
                }

                var overlay = new MaudeChartWindowOverlay(window, MutableDataSink, position);
                if (WindowOverlayHelpers.TryAddOverlay(window, overlay))
                {
                    chartOverlayReference = new WeakReference<MaudeChartWindowOverlay>(overlay);
                    MaudeLogger.Info("Overlay presented and reference stored.");
                }
                else
                {
                    MaudeLogger.Warning("Failed to add overlay to window; disposing overlay instance.");
                    overlay.Dispose();
                }
            }
            catch (Exception ex)
            {
                MaudeLogger.Error("Failed to present overlay.");
                MaudeLogger.Exception(ex);
                chartOverlayReference = null;
            }
            finally
            {
                chartOverlaySemaphore.Release();
            }
        });
    }

    public void DismissOverlay()
    {
        MaudeLogger.Info("DismissOverlay requested.");
        if (!IsOverlayPresented)
        {
            MaudeLogger.Info("DismissOverlay skipped because no overlay is currently presented.");
            return;
        }

        MaudeLogger.Info("Queueing overlay dismissal on main thread.");
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await chartOverlaySemaphore.WaitAsync();
            try
            {
                if (chartOverlayReference?.TryGetTarget(out var overlay) == true)
                {
                    var window = Application.Current?.Windows?.FirstOrDefault();
                    WindowOverlayHelpers.TryRemoveOverlay(window, overlay);
                    overlay.Dispose();
                    MaudeLogger.Info("Overlay removed and disposed.");
                }

                chartOverlayReference = null;
            }
            catch (Exception ex)
            {
                MaudeLogger.Error("Failed to dismiss overlay.");
                MaudeLogger.Exception(ex);
            }
            finally
            {
                chartOverlaySemaphore.Release();
            }
        });
    }
    
    public void EnableShakeGesture()
    {
        MaudeLogger.Info("EnableShakeGesture requested.");
        shakeGestureListener.Enable();
        MaudeLogger.Info("Shake gesture listener enabled.");
    }

    public void DisableShakeGesture()
    {
        MaudeLogger.Info("DisableShakeGesture requested.");
        shakeGestureListener.Disable();
        MaudeLogger.Info("Shake gesture listener disabled.");
    }

    public void Metric(long value, byte channel)
    {
        MutableDataSink.Metric(value, channel);
    }
    
    public void Event(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' on detached channel.");
        MutableDataSink.Event(label);
    }

    public void Event(string label, string icon)
    {
        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' on detached channel.");
        MutableDataSink.Event(label, icon);
    }

    public void Event(string label, string icon, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' and details on detached channel.");
        MutableDataSink.Event(label, icon, details);
    }

    public void Event(string label, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' on channel {channel}.");
        MutableDataSink.Event(label, channel);
    }

    public void Event(string label, string icon, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' on channel {channel}.");
        
        MutableDataSink.Event(label, icon, channel);
    }

    public void Event(string label, string icon, byte channel, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' and details on channel {channel}.");

        MutableDataSink.Event(label, icon, channel, details);
    }

    public void Clear()
    {
        MaudeLogger.Info("Clearing data sink contents.");
        MutableDataSink.Clear();
    }
}
