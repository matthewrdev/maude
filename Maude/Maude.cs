namespace Maude;

public static class Maude
{
    private static readonly Lock Lock = new Lock();
    private static IMaudeRuntime runtime = null;

    public static IMaudeRuntime Runtime
    {
        get
        {
            lock (Lock)
            {
                runtime = runtime ?? new MaudeRuntime();
                return runtime;
            }
        }
    }
    
    public static void Activate()
    {
        Runtime.Activate();
    }

    public static void Deactivate()
    {
        Runtime.Deactivate();
    }

    public static void Clear()
    {
        Runtime.Clear();
    }

    public static bool IsActive()
    {
        return Runtime.IsActive;
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

    public static void Event(string label)
    {
        
    }
    
    public static void Event(string label, string icon)
    {
        
    }
    
}