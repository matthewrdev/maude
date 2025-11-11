namespace Maude;

public interface IMaudeLogCallback
{
    string Name { get; }
    
    void Error(string message);
    
    void Warning(string message);
    
    void Info(string message);
    
    void Exception(Exception exception);
}