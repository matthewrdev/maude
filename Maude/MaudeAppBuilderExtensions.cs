using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude;

/// <summary>
/// Extension methods for wiring Maude into a MAUI application builder.
/// </summary>
public static class MaudeAppBuilderExtensions
{
    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies. 
    /// </summary>
    public static MauiAppBuilder UseMaude(this MauiAppBuilder builder)
    {
        if (!MaudeRuntime.IsInitialized)
        {
            MaudeRuntime.Initialize();
        }

        builder.Services.AddSingleton<IMaudeRuntime>(_ => MaudeRuntime.Instance);
        builder.Services.AddSingleton<IMaudeDataSink>(_ => MaudeRuntime.Instance.DataSink);
        
        return builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("MaterialSymbolsOutlined.ttf", MaudeConstants.MaterialSymbolsFontName);
        }).UseSkiaSharp();
    }
}
