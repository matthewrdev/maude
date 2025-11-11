namespace Maude;

public static class MaudeAppBuilderExtensions
{

    /// <summary>
    /// Configures the <see cref="MauiAppBuilder"/> to use the specified <typeparamref name="TApp"/> as the main application type.
    /// </summary>
    /// <typeparam name="TApp">The type to use as the application.</typeparam>
    /// <param name="builder">The <see cref="MauiAppBuilder"/> to configure.</param>
    /// <param name="implementationFactory">A factory to create the specified <typeparamref name="TApp"/> using the services provided in a <see cref="IServiceProvider"/>.</param>
    /// <returns>The configured <see cref="MauiAppBuilder"/>.</returns>
    public static MauiAppBuilder UseMaude<TApp>(this MauiAppBuilder builder, Func<IServiceProvider, TApp> implementationFactory)
        where TApp : class, IApplication
    {
        builder.ConfigureFonts(fonts =>
        {
            fonts.AddFont("MaterialSymbolsOutlined.ttf", MaudeConstants.MaterialSymbolsFontName);
        });

        builder.Services.AddSingleton(Maude.Runtime);
        
        return builder;
    }

    
}