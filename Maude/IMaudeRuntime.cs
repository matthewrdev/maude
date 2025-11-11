namespace Maude;

public interface IMaudeRuntime
{
    IMaudeDataSink DataSink { get; }
    
    bool IsActive { get; }

    event EventHandler OnActivated;
    
    event EventHandler OnDeactivated;

    void Activate();
    
    void Deactivate();
    
    // hide/show
    // Add event
}

