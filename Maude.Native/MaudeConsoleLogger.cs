namespace Maude;

/// <summary>
/// Simple console-based logger for Maude diagnostics.
/// </summary>
public class MaudeConsoleLogger : IMaudeLogCallback
{
    public string Name { get; } = "Maude Console Logger";
    
    public void Error(string message)
    {
        Console.WriteLine($"{MaudeConstants.LoggingPrefix} (Error)" + message);
    }

    public void Warning(string message)
    {
        Console.WriteLine($"{MaudeConstants.LoggingPrefix} (Warning)" + message);
    }

    public void Info(string message)
    {
        Console.WriteLine($"{MaudeConstants.LoggingPrefix} " + message);
    }

    public void Exception(Exception exception)
    {
        Console.WriteLine($"{MaudeConstants.LoggingPrefix} (Exception)" + exception);
    }
}
