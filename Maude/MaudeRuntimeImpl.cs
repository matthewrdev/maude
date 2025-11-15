using System.Threading;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

namespace Maude;

internal class MaudeRuntimeImpl : IMaudeRuntime
{
    private readonly MaudeOptions options;
    private readonly SemaphoreSlim presentationSemaphore = new SemaphoreSlim(1, 1);
    private MemorySamplerThread samplerThread;
    private WeakReference<IMaudePopup> presentedMaudeViewReference;
    private readonly object samplerLock = new object();
    private bool gcNotificationsStarted;

    public readonly MaudeMutableDataSink MutableDataSink;

    public IMaudeDataSink DataSink => MutableDataSink;

    public MaudeRuntimeImpl(MaudeOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        this.options = options;
        MutableDataSink = new MaudeMutableDataSink(options);
    }
    
    public bool IsActive { get; private set; }

    public bool IsPresented => presentedMaudeViewReference != null && presentedMaudeViewReference.TryGetTarget(out _);

    public bool IsPresentationEnabled => true;
    
    public event EventHandler? OnActivated;
    
    public event EventHandler? OnDeactivated;
    
    public void Activate()
    {
        lock (samplerLock)
        {
            if (IsActive)
            {
                return;
            }
            
            if (!gcNotificationsStarted)
            {
                gcNotificationsStarted = true;
                GCNotification.Start();
            }
            
            samplerThread = new MemorySamplerThread(options.SampleFrequencyMilliseconds, snapshot =>
            {
                MutableDataSink.RecordMemorySnapshot(snapshot);
            });
            
            IsActive = true;
        }

        OnActivated?.Invoke(this, EventArgs.Empty);
    }

    public void Deactivate()
    {
        lock (samplerLock)
        {
            if (!IsActive)
            {
                return;
            }

            samplerThread?.Dispose();
            samplerThread = null;
            IsActive = false;
        }

        OnDeactivated?.Invoke(this, EventArgs.Empty);
    }

    public void Present()
    {
        if (!IsPresentationEnabled || IsPresented)
        {
            return;
        }
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await presentationSemaphore.WaitAsync();
            try
            {
                if (IsPresented)
                {
                    return;
                }

                var maudeView = new MaudeView();
                
                var popupView = await CreateAndOpenPopup(maudeView);
                WirePopupLifecycle(popupView);
                this.presentedMaudeViewReference = new  WeakReference<IMaudePopup>(popupView);
            }
            catch (Exception ex)
            {
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
        if (maudeView == null) throw new ArgumentNullException(nameof(maudeView));
            
        maudeView.HorizontalOptions = LayoutOptions.Fill;
        maudeView.VerticalOptions = LayoutOptions.Fill;

        var mauiContext = Application.Current?.Handler?.MauiContext;
        maudeView.BackgroundColor = Colors.WhiteSmoke;
        
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var handler = maudeView.ToHandler(mauiContext);
        var platformView = handler.PlatformView;
        
        var themedCtx = new AndroidX.AppCompat.View.ContextThemeWrapper(activity, Resource.Style.Maui_MainTheme);

        var popup = new MaudePopup(themedCtx);
        popup.PopupView = maudeView;
        popup.SetContentView(platformView);

        popup.Show();

#elif IOS
        var window = UIKit.UIApplication.SharedApplication.KeyWindow;
        var rootController = window?.RootViewController?.PresentedViewController ?? window?.RootViewController;

        if (rootController == null)
        {
            throw new InvalidOperationException();
        }

        var handler = maudeView.ToHandler(mauiContext);
        var platformView = handler.PlatformView as UIKit.UIView;
        
        var popup = new MaudePopup(maudeView, platformView, rootController);

        popup.Show();
#endif

        return popup;
    }

    private void WirePopupLifecycle(IMaudePopup popup)
    {
        if (popup == null)
        {
            return;
        }

        void Handler(object sender, EventArgs args)
        {
            popup.OnClosed -= Handler;
            
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

    public void Dismiss()
    {
        if (!IsPresented)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await presentationSemaphore.WaitAsync();
            try
            {
                if (!IsPresented)
                {
                    return;
                }

                if (this.presentedMaudeViewReference.TryGetTarget(out var view))
                {
                    view.Close();
                }

                presentedMaudeViewReference = null;
            }
            catch (Exception ex)
            {
                MaudeLogger.Exception(ex);
            }
            finally
            {
                presentationSemaphore.Release();
            }
        });
    }

    public void Metric(long value, byte channel)
    {
        MutableDataSink.Metric(value, channel);
    }

    public void Event(string label, byte channel)
    {
        MutableDataSink.Event(label, channel);
    }

    public void Event(string label, string icon, byte channel)
    {
        MutableDataSink.Event(label, icon, channel);
    }

    public void Clear()
    {
        MutableDataSink.Clear();
    }
}
