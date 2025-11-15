using Microsoft.Maui.Controls;

namespace Maude;

public partial class MaudeView : VerticalStackLayout
{
    public MaudeView()
    {
        InitializeComponent();
        
        BindRuntime();
    }

    private void BindRuntime()
    {
        chartView.DataSink = MaudeRuntime.Instance.DataSink;
        eventsView.DataSink = MaudeRuntime.Instance.DataSink;
    }
}
