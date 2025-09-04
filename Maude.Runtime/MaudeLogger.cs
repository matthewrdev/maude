namespace Maude.Runtime;

internal static class MaudeLogger
{
    private static readonly Lock LoggerLock = new Lock();

    private static readonly List<IMaudeLogCallback> Callbacks = new List<IMaudeLogCallback>();


    public static void RegisterCallback(IMaudeLogCallback callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        
        lock (LoggerLock)
        {
            if (Callbacks.Contains(callback))
            {
                throw new InvalidOperationException($"The {nameof(IMaudeLogCallback)} instance, '{callback.GetHashCode()}', is already registered.");
            }
            
            Callbacks.Add(callback);
        }
    }

    public static void RemoveCallback(IMaudeLogCallback callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));

        lock (LoggerLock)
        {
            Callbacks.Remove(callback);
        }
    }


    public static void Error(string message)
    {
        
    }
    
    public static void Warning(string message)
    {
        
    }
    
    public static void Info(string message)
    {
        
    }
    
    public static void Exception(Exception exception)
    {
        
    }
}