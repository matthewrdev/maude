#if IOS
using System;
using CoreMotion;
using Foundation;

namespace Maude;

internal partial class MaudeShakeGestureListener
{
    private CMMotionManager? motionManager;
    private const double ShakeThresholdG = 2.7;
    private const int ShakeSlopTimeMs = 500;
    private long lastShakeTimestamp;

    partial void OnEnable()
    {
        motionManager = new CMMotionManager();
        if (!motionManager.AccelerometerAvailable)
        {
            MaudeLogger.Warning("Shake gesture is not supported on this device (no accelerometer).");
            return;
        }

        motionManager.AccelerometerUpdateInterval = 0.05; // 20 Hz
        motionManager.StartAccelerometerUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
        {
            if (data == null)
            {
                return;
            }

            var x = data.Acceleration.X;
            var y = data.Acceleration.Y;
            var z = data.Acceleration.Z;

            var gForce = Math.Sqrt(x * x + y * y + z * z);
            if (gForce < ShakeThresholdG)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (lastShakeTimestamp + ShakeSlopTimeMs > now)
            {
                return;
            }

            lastShakeTimestamp = now;
            HandleShake();
        });
    }

    partial void OnDisable()
    {
        if (motionManager == null)
        {
            return;
        }

        if (motionManager.AccelerometerActive)
        {
            motionManager.StopAccelerometerUpdates();
        }

        motionManager.Dispose();
        motionManager = null;
    }
}
#endif
