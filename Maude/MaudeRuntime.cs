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
                    throw new InvalidOperationException("You must call 'MaudeRuntime.Initialize(MaudeOptions)' before accessing the MaudeRuntime Instance.");
                }
                
                return runtime;
            }
        }
    }

    public static void Initialize(MaudeOptions options = null)
    {
        lock (runtimeLock)
        {
            if (runtime != null)
            {
                throw new InvalidOperationException("The MaudeRuntime has already been initialized.");
            }
            
            options = options ?? MaudeOptions.Default;
            
            runtime = new  MaudeRuntimeImpl(options);
        }
    }
    
    /// <summary>
    /// Activates the MA
    /// </summary>
    public static void Activate()
    {
        Instance.Activate();
    }

    public static void Deactivate()
    {
        Instance.Deactivate();
    }

    public static void Clear()
    {
        Instance.Clear();
    }

    public static bool IsActive()
    {
        return Instance.IsActive;
    }

    public static bool IsPresented => Instance.IsPresented;

    public static bool IsPresentationEnabled => Instance.IsPresentationEnabled;

    public static void Present()
    {
        Instance.Present();
    }

    /// <summary>
    /// Dismisses the presented Maude slide in sheet.
    /// </summary>
    public static void Dismiss()
    {
        Instance.Dismiss();
    }

    public static void PresentChartOverlay(MaudeOverlayPosition position = MaudeOverlayPosition.TopRight)
    {
        Instance.PresentChartOverlay(position);
    }

    public static void DismissChartOverlay()
    {
        Instance.DismissChartOverlay();
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

    /// <summary>
    /// Captures a new event using the given <paramref name="label"/> against the <paramref name="channel"/> using the default icon.
    /// <para/>
    /// The provided <paramref name="channel"/> <b>must</b> be a built-in channel or predefined during your setup of Maude via <see cref="MaudeRuntime.Initialize"/>.
    /// <para/>
    /// Event's recorded against unknown channels will be discarded.
    /// </summary>
    public static void Event(string label, byte channel)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
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
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(label));
        
        Instance.Event(label, icon, channel);
    }
}
