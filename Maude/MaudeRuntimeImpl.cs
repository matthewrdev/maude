using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Maude;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class MaudeRuntimeImpl : IMaudeRuntime
{
    private readonly MaudeOptions options;
    
    private readonly IMaudePresentationService presentationService;
    private MemorySamplerThread? samplerThread;
    private readonly Lock samplerLock = new Lock();
    
    private readonly MaudeMutableDataSink MutableDataSink;
    private readonly IFrameRateMonitor frameRateMonitor;
    private bool fpsTrackingEnabled;
    private MaudeEventRenderingBehaviour eventRenderingBehaviour;
    private MaudeChartTheme chartTheme;
    
    private readonly MaudeShakeGestureListener shakeGestureListener;

    public IMaudeDataSink DataSink => MutableDataSink;

    internal MaudeSaveSnapshotAction? SaveSnapshotAction => options.SaveSnapshotAction;

    public MaudeRuntimeImpl(MaudeOptions options, IMaudePresentationService? presentationService = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        MaudeLogger.Info($"Creating runtime with sample frequency {options.SampleFrequencyMilliseconds}ms, retention {options.RetentionPeriodSeconds}s, shake gesture allowed: {options.AllowShakeGesture}, additional channels: {options.AdditionalChannels?.Count ?? 0}.");
        MutableDataSink = new MaudeMutableDataSink(options);
        MaudeLogger.Info("Mutable data sink created.");
        frameRateMonitor = FrameRateMonitorFactory.Create();
        fpsTrackingEnabled = options.EnableFramesPerSecond;
        eventRenderingBehaviour = options.EventRenderingBehaviour;
        chartTheme = options.ChartTheme;
        shakeGestureListener = new MaudeShakeGestureListener(this, options);
        MaudeLogger.Info("Shake gesture listener initialised.");
        this.presentationService = presentationService
            ?? MaudeRuntimePlatform.CreatePresentationService(this.options, MutableDataSink)
            ?? new NullPresentationService();
    }
    
    public bool IsActive { get; private set; }
    
    public bool IsSheetPresented => presentationService.IsSheetPresented;

    public bool IsPresentationEnabled => presentationService.IsPresentationEnabled;

    public bool IsFramesPerSecondEnabled => fpsTrackingEnabled;
    
    public MaudeEventRenderingBehaviour EventRenderingBehaviour
    {
        get => eventRenderingBehaviour;
        set => eventRenderingBehaviour = value;
    }

    public MaudeChartTheme ChartTheme
    {
        get => chartTheme;
        set => chartTheme = value;
    }
    
    public bool IsOverlayPresented => presentationService.IsOverlayPresented;
    
    public event EventHandler? OnActivated;
    
    public event EventHandler? OnDeactivated;

    internal async Task ExecuteSaveSnapshotActionAsync()
    {
        var action = options.SaveSnapshotAction;
        if (action == null)
        {
            return;
        }

        MaudeSnapshot snapshot;
        try
        {
            snapshot = MutableDataSink.Snapshot();
        }
        catch (Exception ex)
        {
            MaudeLogger.Error("Failed to capture snapshot for save action.");
            MaudeLogger.Exception(ex);
            return;
        }

        try
        {
            await action.CopyDelegate(snapshot);
        }
        catch (Exception ex)
        {
            MaudeLogger.Error("Save snapshot action delegate threw an exception.");
            MaudeLogger.Exception(ex);
        }
    }

    public void Activate()
    {
        lock (samplerLock)
        {
            if (IsActive)
            {
                MaudeLogger.Info("Activate requested but runtime is already active.");
                EnableShakeGesture();
                return;
            }

            if (ShouldTrackFps())
            {
                frameRateMonitor.Start();
            }
            samplerThread = new MemorySamplerThread(options.SampleFrequencyMilliseconds, snapshot =>
            {
                MutableDataSink.RecordMemorySnapshot(snapshot);
                RecordFrameSample();
            });
            
            IsActive = true;
            MaudeLogger.Info($"Memory sampler started with frequency {options.SampleFrequencyMilliseconds}ms.");
        }

        EnableShakeGesture();
        OnActivated?.Invoke(this, EventArgs.Empty);
    }

    public void Deactivate()
    {
        lock (samplerLock)
        {
            if (!IsActive)
            {
                MaudeLogger.Info("Deactivate requested but runtime is already inactive.");
                DisableShakeGesture();
                return;
            }

            samplerThread?.Dispose();
            samplerThread = null;
            IsActive = false;
            MaudeLogger.Info("Memory sampler disposed and activity flag cleared.");
        }

        frameRateMonitor.Stop();
        DisableShakeGesture();
        OnDeactivated?.Invoke(this, EventArgs.Empty);
    }

    private void RecordFrameSample()
    {
        if (!ShouldTrackFps())
        {
            return;
        }

        var fps = frameRateMonitor.ConsumeFramesPerSecond();

        // Skip recording until we have a meaningful sample.
        if (fps <= 0)
        {
            return;
        }

        MutableDataSink.Metric(fps, MaudeConstants.ReservedChannels.FramesPerSecond_Id);
    }

    public void EnableFramesPerSecond()
    {
        fpsTrackingEnabled = true;
        if (IsActive)
        {
            frameRateMonitor.Start();
        }
    }

    public void DisableFramesPerSecond()
    {
        fpsTrackingEnabled = false;
        frameRateMonitor.Stop();
    }

    private bool ShouldTrackFps() => fpsTrackingEnabled;

    public void PresentSheet() => presentationService.PresentSheet();

    public void DismissSheet() => presentationService.DismissSheet();

    public void PresentOverlay()
    {
        PresentOverlay(options.DefaultOverlayPosition);
    }

    public void PresentOverlay(MaudeOverlayPosition position)
    {
        presentationService.PresentOverlay(position);
    }

    public void DismissOverlay()
    {
        presentationService.DismissOverlay();
    }
    
    public void EnableShakeGesture()
    {
        if (!options.AllowShakeGesture)
        {
            MaudeLogger.Info("Shake gesture enable requested but gestures are disabled in options; disabling listener.");
            shakeGestureListener.Disable();
            return;
        }
        
        if (!IsActive)
        {
            MaudeLogger.Info("Shake gesture enable requested while runtime is inactive; disabling instead.");
            shakeGestureListener.Disable();
            return;
        }
        
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

    public void Event(string label, MaudeEventType type)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' with type '{type}' on detached channel.");
        MutableDataSink.Event(label, type);
    }

    public void Event(string label, MaudeEventType type, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with type '{type}' and details on detached channel.");
        MutableDataSink.Event(label, type, details);
    }

    public void Event(string label, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' on channel {channel}.");
        MutableDataSink.Event(label, channel);
    }

    public void Event(string label, MaudeEventType type, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        MaudeLogger.Info($"Recording event '{label}' with type '{type}' on channel {channel}.");
        
        MutableDataSink.Event(label, type, channel);
    }

    public void Event(string label, MaudeEventType type, byte channel, string details)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));

        MaudeLogger.Info($"Recording event '{label}' with type '{type}' and details on channel {channel}.");

        MutableDataSink.Event(label, type, channel, details);
    }

    public void Clear()
    {
        MaudeLogger.Info("Clearing data sink contents.");
        MutableDataSink.Clear();
    }
}
