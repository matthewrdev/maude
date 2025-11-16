namespace Maude;

public static class MaudeRuntime
{
    private static readonly Lock runtimeLock = new Lock();

    private static MaudeRuntimeImpl runtime;

    public static IMaudeRuntime Instance => MutableInstance;

    public static bool IsInitialized
    {
        get
        {
            lock (runtimeLock)
            {
                return runtime != null;
            }
        }
    }

    internal static MaudeRuntimeImpl MutableInstance
    {
        get
        {
            lock (runtimeLock)
            {
                if (runtime == null)
                {
                    MaudeLogger.Error($"Attempted to access MaudeRuntime before it was initialized.");
                    throw new InvalidOperationException("You must call 'MaudeRuntime.Initialize(MaudeOptions)' before accessing the MaudeRuntime Instance.");
                }
                
                return runtime;
            }
        }
    }
    
    
    private static void Initialize_Internal(bool activateImmediately, 
                                            MaudeOptions options)
    {
        MaudeLogger.Info($"Initialising MaudeRuntime (activateImmediately: {activateImmediately}).");
        lock (runtimeLock)
        {
            if (runtime != null)
            {
                throw new InvalidOperationException("The MaudeRuntime has already been initialized.");
            }

            options = options ?? MaudeOptions.Default;
            MaudeLogger.Info($"Using options: sample frequency {options.SampleFrequencyMilliseconds}ms, retention {options.RetentionPeriodSeconds}s, additional channels {options.AdditionalChannels?.Count ?? 0}.");
            
            if (options.AdditionalLogger != null)
            {
                MaudeLogger.RegisterCallback(options.AdditionalLogger);
            }
            
            runtime = new MaudeRuntimeImpl(options);
            MaudeLogger.Info($"MaudeRuntime initialisation complete.");
        }

        if (activateImmediately)
        {
            MaudeLogger.Info($"Activate immediately requested post initialisation.");
            Activate();
        }
    }

    /// <summary>
    /// Initialises the <see cref="IMaudeRuntime"/> using the provided <paramref name="options"/> and immediately begins monitoring memory usage.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void InitializeAndActivate(MaudeOptions options = null)
    {
        Initialize_Internal(activateImmediately: true, options);
    }

    /// <summary>
    /// Initialises the <see cref="IMaudeRuntime"/> using the provided <paramref name="options"/>.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void Initialize(MaudeOptions options = null)
    {   
        Initialize_Internal(activateImmediately: false, options);
    }
    
    /// <summary>
    /// Starts memory sampling and raises OnActivated when complete.
    /// </summary>
    public static void Activate()
    {
        Instance.Activate();
    }

    /// <summary>
    /// Stops memory sampling and raises OnDeactivated when complete.
    /// </summary>
    public static void Deactivate()
    {
        Instance.Deactivate();
    }

    /// <summary>
    /// Clears the backing data sink, removing all recorded metrics and events.
    /// </summary>
    public static void Clear()
    {
        Instance.Clear();
    }

    public static bool IsActive()
    {
        return Instance.IsActive;
    }

    /// <summary>
    /// Returns true when the slide-in sheet UI is currently presented.
    /// </summary>
    public static bool IsPresented => Instance.IsSheetPresented;

    /// <summary>
    /// Should the slide sheet or overlay be allowed to be presented?
    /// </summary>
    public static bool IsPresentationEnabled => Instance.IsPresentationEnabled;
    
    /// <summary>
    /// Returns true when the chart overlay is currently presented.
    /// </summary>
    public static bool IsChartOverlayPresented => Instance.IsOverlayPresented;

    public static void Present()
    {
        Instance.PresentSheet();
    }

    /// <summary>
    /// Dismisses the presented Maude slide in sheet.
    /// </summary>
    public static void Dismiss()
    {
        Instance.DismissSheet();
    }

    /// <summary>
    /// Shows the chart overlay on the current window at the given position.
    /// </summary>
    public static void PresentChartOverlay(MaudeOverlayPosition position = MaudeOverlayPosition.TopRight)
    {
        Instance.PresentOverlay(position);
    }

    public static void EnableShakeGesture()
    {
        Instance.EnableShakeGesture();
    }

    public static void DisableShakeGesture()
    {
        Instance.DisableShakeGesture();
    }

    /// <summary>
    /// Hides and disposes the chart overlay.
    /// </summary>
    public static void DismissChartOverlay()
    {
        Instance.DismissOverlay();
    }
    
    /// <summary>
    /// Captures a new metric using the given <paramref name="value"/> against the <paramref name="channel"/>
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Metrics recorded against unknown channels will be discarded.
    /// </summary>
    public static void Metric(long value, byte channel)
    {
        Instance.Metric(value, channel);
    }
    
    
    public static void Event(string label)
    {
        Instance.Event(label);
    }

    public static void Event(string label, string icon)
    {
        Instance.Event(label, icon);
    }

    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> and <paramref name="icon"/> with the provided <paramref name="details"/> against the unspecified channel.
    /// </summary>
    public static void Event(string label, string icon, string details)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        }

        Instance.Event(label, icon, details);
    }
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the default icon.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    public static void Event(string label, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        }
        
        Instance.Event(label, channel);
    }
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the given 
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    public static void Event(string label, string icon, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        }
        
        Instance.Event(label, icon, channel);
    }
    
    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the provided <paramref name="icon"/> with the additional <paramref name="details"/>.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    public static void Event(string label, string icon, byte channel, string details)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        }
        
        Instance.Event(label, icon, channel, details);
    }
}
