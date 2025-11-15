using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude;

public static class MaudeAppBuilderExtensions
{

    /// <summary>
    /// Configures the <see cref="MauiAppBuilder"/> to use the specified <typeparamref name="TApp"/> as the main application type.
    /// </summary>
    /// <typeparam name="TApp">The type to use as the application.</typeparam>
    /// <param name="builder">The <see cref="MauiAppBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="MauiAppBuilder"/>.</returns>
    public static MauiAppBuilder UseMaude<TApp>(this MauiAppBuilder builder)
        where TApp : class, IApplication
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
