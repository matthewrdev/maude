namespace Maude;

public partial class MaudeRuntime : IMaudeRuntime
{
    public IMaudeDataSink DataSink { get; }
    
    public bool IsActive { get; }
    
    // TODO: 
    
    public event EventHandler? OnActivated;
    public event EventHandler? OnDeactivated;
    public void Activate()
    {
    }

    public void Deactivate()
    {
        throw new NotImplementedException();
    }
}