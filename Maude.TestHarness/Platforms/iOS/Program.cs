using ObjCRuntime;
using UIKit;

namespace Maude.TestHarness;

public class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        var options = MaudeOptions.CreateBuilder()
            .WithAdditionalLogger(new CustomMaudeLogCallback())
            .WithShakeGesture()
            .WithSampleFrequencyMilliseconds(250)
            .WithShakeGestureBehaviour(MaudeShakeGestureBehaviour.Overlay)
            .WithAdditionalChannels(CustomMaudeConfiguration.AdditionalChannels)
            .Build();
        
        MaudeRuntime.InitializeAndActivate(options);
        
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
