using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude;

public static class MaudeAppBuilderExtensions
{

    /// <summary>
    /// Configures the <see cref="MauiAppBuilder"/> to use the specified <typeparamref name="TApp"/> as the main application type
    /// </summary>
    public static MauiAppBuilder UseMaude(this MauiAppBuilder builder)
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("MaterialSymbolsOutlined.ttf", MaudeConstants.MaterialSymbolsFontName);
        });

        if (!MaudeRuntime.IsInitialized)
        {
            MaudeRuntime.Initialize();
        }
        
        builder.Services.AddSingleton<IMaudeRuntime>(_ => MaudeRuntime.Instance);
        builder.Services.AddSingleton<IMaudeDataSink>(_ => MaudeRuntime.Instance.DataSink);
        
        return builder.UseSkiaSharp();
    }

    
}
