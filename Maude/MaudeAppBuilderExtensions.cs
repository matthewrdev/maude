using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude;

public static class MaudeAppBuilderExtensions
{

    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies. 
    /// </summary>
    public static MauiAppBuilder UseMaude(this MauiAppBuilder builder, bool activate =  true)
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("MaterialSymbolsOutlined.ttf", MaudeConstants.MaterialSymbolsFontName);
        });

        var didInitialise = false;
        if (!MaudeRuntime.IsInitialized)
        {
            MaudeRuntime.Initialize();
            didInitialise = true;
        }

        if (activate)
        {
            MaudeRuntime.Activate();
        }
        
        builder.Services.AddSingleton<IMaudeRuntime>(_ => MaudeRuntime.Instance);
        builder.Services.AddSingleton<IMaudeDataSink>(_ => MaudeRuntime.Instance.DataSink);
        
        return builder.UseSkiaSharp();
    }

    
}
