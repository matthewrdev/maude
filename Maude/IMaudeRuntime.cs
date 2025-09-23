namespace Maude;

public interface IMaudeRuntime
{
    IMaudeDataSink DataSink { get; }
    
    bool IsActive { get; }

    void Activate();
    
    void Deactivate();
    
}

