namespace Maude.Runtime;

public static class Maude
{
    private static readonly Lock Lock = new Lock();
    private static IMaudeRuntime runtime = null;

    public static IMaudeRuntime Runtime
    {
        get
        {
            lock (runtime)
            {
                runtime = runtime ?? new MaudeRuntime();
            }
        }
    }
    
    
    public event 
    
    public static void Start()
    {
        
    }

    public static void Stop()
    {
    }

    public static void Clear()
    {
        
    }

    public static bool IsPresented
    {
        get => false;
    }

    public static bool IsPresentationEnabled
    {
        get => true;
    }

    public static void Present()
    {
        
    }

    public static void Dismiss()
    {
        
    }

    public static void Sample()
    {
        
    }

    public static void Event(string label, string iconCode)
    {
        
    }
    
}