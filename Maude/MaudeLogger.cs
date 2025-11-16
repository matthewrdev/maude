namespace Maude;

/// <summary>
/// The logging framework used by Maude, which allows the integrating software to subscribe 
/// </summary>
public static class MaudeLogger
{
    private static readonly Lock LoggerLock = new Lock();

    private static readonly List<IMaudeLogCallback> Callbacks = new List<IMaudeLogCallback>();


    /// <summary>
    /// Registers a <see cref="IMaudeLogCallback"/> into the logger, which will receive all log messages recorded by Maude.
    /// </summary>
    public static void RegisterCallback(IMaudeLogCallback callback)
    {
        if (callback == null) throw new ArgumentNullException(nameof(callback));

        if (string.IsNullOrWhiteSpace(callback.Name))
        {
            throw new ArgumentException($"The provided logging callback must have a name.", nameof(callback));
        }
        
        lock (LoggerLock)
        {
            if (Callbacks.Contains(callback))
            {
                throw new InvalidOperationException($"The {nameof(IMaudeLogCallback)} instance, '{callback.GetHashCode()}', is already registered.");
            }

            if (Callbacks.Any(a => string.Equals(a.Name, callback.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidOperationException($"The {nameof(IMaudeLogCallback)} instance must have a unique name. A callback with the name {callback.Name} already exists.");
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

    private static void Broadcast(Action<IMaudeLogCallback> invoke)
    {
        lock (LoggerLock)
        {
            for (int i = Callbacks.Count - 1; i >= 0; i--)
            {
                var callback = Callbacks[i];
                if (callback is null)
                {
                    Callbacks.RemoveAt(i); 
                    continue;
                }

                try
                {
                    invoke(callback);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{MaudeConstants.LoggingPrefix} 🚨 The logging callback {callback.Name}|{callback.GetHashCode()} failed {e}. This callback will be automatically de-registered");

                    try
                    {
                        Callbacks.RemoveAt(i);
                    }
                    catch (Exception removeEx)
                    {
                        Console.WriteLine($"{MaudeConstants.LoggingPrefix} ⚠️ Failed to deregister callback {callback.Name}|{callback.GetHashCode()}: {removeEx}");
                    }
                }
            }
        }
    }

    public static void Error(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;    
        }
        
        Broadcast(cb => cb.Error(message));
    }

    public static void Warning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;    
        }
        
        Broadcast(cb => cb.Warning(message));
    }

    public static void Info(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;    
        }
        
        Broadcast(cb => cb.Info(message));
    }

    public static void Exception(Exception exception)
    {
        if (exception == null)
        {
            return;
        }
        
        Broadcast(cb => (cb).Exception(exception));
    }

}
