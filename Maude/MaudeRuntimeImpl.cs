using System.Diagnostics.CodeAnalysis;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace Maude;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class MaudeRuntimeImpl : IMaudeRuntime
{
    private readonly MaudeOptions options;
    private readonly SemaphoreSlim presentationSemaphore = new SemaphoreSlim(1, 1);
    private WeakReference<IMaudePopup>? presentedMaudeViewReference;
    
    private readonly SemaphoreSlim chartOverlaySemaphore = new SemaphoreSlim(1, 1);
    private readonly INativeOverlayService overlayService;
    
    private MemorySamplerThread? samplerThread;
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
        overlayService = new NativeOverlayService();
    }
    
    public bool IsActive { get; private set; }
    
    public bool IsSheetPresented => presentedMaudeViewReference != null && presentedMaudeViewReference.TryGetTarget(out _);

    public bool IsPresentationEnabled => true;
    
    public bool IsOverlayPresented => overlayService?.IsVisible == true;
    
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
                
                var popupView = CreateAndOpenPopup(maudeView);

                if (popupView != null)
                {
                    MaudeLogger.Error("An error occured while opening the slide sheet.");
                    WirePopupLifecycle(popupView, maudeView);
                    presentedMaudeViewReference = new WeakReference<IMaudePopup>(popupView);
                }
                else
                {
                    maudeView.UnbindRuntime();
                    MaudeLogger.Error("An error occured while opening the slide sheet.");
                }
            }
            catch (Exception ex)
            {
                MaudeLogger.Error("Failed to present Maude sheet.");
                MaudeLogger.Exception(ex);
                presentedMaudeViewReference = null;
            }
            finally
            {
                presentationSemaphore.Release();
            }
        });
    }
    
    
    private IMaudePopup? CreateAndOpenPopup(MaudeView maudeView)
    {
        if (maudeView == null)
        {
            throw new ArgumentNullException(nameof(maudeView));
        }

        maudeView.HorizontalOptions = LayoutOptions.Fill;
        maudeView.VerticalOptions = LayoutOptions.Fill;

        var mauiContext = Application.Current?.Handler?.MauiContext;
        if (mauiContext == null)
        {
            MaudeLogger.Error("Unable to present the slide sheet as the 'Application.Current?.Handler?.MauiContext' is null.");
            return null;
        }
        
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
                     ?? UIKit.UIApplication.SharedApplication
                         ?.ConnectedScenes
                         ?.OfType<UIKit.UIWindowScene>()
                         ?.SelectMany(scene => scene.Windows)
                         ?.FirstOrDefault();

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

        void Handler(object sender, EventArgs args)
        {
            popup.OnClosed -= Handler;
            MaudeLogger.Info("Maude slide sheet closed.");
            
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
    }

    public void DismissSheet()
    {
        if (!IsSheetPresented)
        {
            MaudeLogger.Info("DismissSheet skipped because no sheet is currently presented.");
            return;
        }

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
                MaudeLogger.Info("Slide sheet was dismissed.");
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

    public void PresentOverlay()
    {
        PresentOverlay(options.DefaultOverlayPosition);
    }

    public void PresentOverlay(MaudeOverlayPosition position)
    {
        MaudeLogger.Info($"PresentOverlay requested at position {position}.");
        if (!IsPresentationEnabled)
        {
            MaudeLogger.Warning("Overlay presentation requested while presentation is disabled.");
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await chartOverlaySemaphore.WaitAsync();
            try
            {
                overlayService.Show(MutableDataSink, position);
            }
            catch (Exception ex)
            {
                MaudeLogger.Exception(ex);
            }
            finally
            {
                chartOverlaySemaphore.Release();
            }
        });
    }

    public void DismissOverlay()
    {
        if (!IsOverlayPresented)
        {
            MaudeLogger.Info("DismissOverlay skipped because no overlay is currently presented.");
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await chartOverlaySemaphore.WaitAsync();
            try
            {
                MaudeLogger.Info("Dismissed the charting overlay.");
                overlayService.Hide();
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
        shakeGestureListener.Enable();
        MaudeLogger.Info("Shake gesture listener enabled.");
    }

    public void DisableShakeGesture()
    {
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
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' on detached channel.");
        MutableDataSink.Event(label, icon ?? MaudeConstants.DefaultEventIcon);
    }

    public void Event(string label, string icon, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' and details on detached channel.");
        MutableDataSink.Event(label, icon ?? MaudeConstants.DefaultEventIcon, details);
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
        
        MutableDataSink.Event(label, icon ?? MaudeConstants.DefaultEventIcon, channel);
    }

    public void Event(string label, string icon, byte channel, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with icon '{(icon ?? "NA")}' and details on channel {channel}.");

        MutableDataSink.Event(label, icon ?? MaudeConstants.DefaultEventIcon, channel, details);
    }

    public void Clear()
    {
        MaudeLogger.Info("Clearing data sink contents.");
        MutableDataSink.Clear();
    }
}
