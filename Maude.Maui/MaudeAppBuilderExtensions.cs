using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Maude;

/// <summary>
/// Extension methods for wiring Maude into a MAUI application builder.
/// </summary>
public static class MaudeAppBuilderExtensions
{
    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies.
    /// Does not activate the memory tracking, please use <see cref="MaudeRuntime.Activate"/> to start tracking memory usage.
    /// </summary>
    public static MauiAppBuilder UseMaude(this MauiAppBuilder builder, MaudeOptions? maudeOptions = null)
    {
        maudeOptions ??= MaudeOptions.CreateBuilder()
            .WithMauiWindowProvider()
            .Build();

#if ANDROID
        if (maudeOptions.PresentationWindowProvider == null)
        {
            throw new InvalidOperationException("MaudeOptions.PresentationWindowProvider is required on Android. Call WithMauiWindowProvider or WithPresentationWindowProvider.");
        }
#endif

        if (!MaudeRuntime.IsInitialized)
        {
            MaudeRuntime.Initialize(maudeOptions);
        }

        builder.Services.AddSingleton<IMaudeRuntime>(_ => MaudeRuntime.Instance);
        builder.Services.AddSingleton<IMaudeDataSink>(_ => MaudeRuntime.Instance.DataSink);
        
        return builder.UseSkiaSharp();
    }

    /// <summary>
    /// Set's up the <see cref="MauiAppBuilder"/> to initialise the <see cref="MaudeRuntime"/> and registers required fonts and dependencies.
    /// Immediately activates Maudes memory tracking.
    /// </summary>
    public static MauiAppBuilder UseMaudeAndActivate(this MauiAppBuilder builder, MaudeOptions? maudeOptions = null)
    {
        builder = builder.UseMaude(maudeOptions);
        
        MaudeRuntime.Activate();
        
        return builder;
    }
}
