namespace Maude;

public interface IMaudeLogCallback
{
    void Error(string message);
    
    void Warning(string message);
    
    void Info(string message);
    
    void Exception(Exception exception);
}