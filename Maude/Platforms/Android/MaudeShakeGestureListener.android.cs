#if ANDROID
using System;
using Android.Content;
using Android.Hardware;

namespace Maude;

internal partial class MaudeShakeGestureListener
{
    private SensorManager? sensorManager;
    private ShakeSensorListener? listener;

    partial void OnEnable()
    {
        var activity = PlatformContext.CurrentActivityProvider?.Invoke();
        if (activity == null)
        {
            MaudeLogger.Warning("Shake gesture enable requested but no current Activity is available.");
            return;
        }

        sensorManager = (SensorManager?)activity.GetSystemService(Context.SensorService);
        var accelerometer = sensorManager?.GetDefaultSensor(SensorType.Accelerometer);
        if (sensorManager == null || accelerometer == null)
        {
            MaudeLogger.Warning("Shake gesture is not supported on this device (no accelerometer).");
            return;
        }

        listener = new ShakeSensorListener(this);
        sensorManager.RegisterListener(listener, accelerometer, SensorDelay.Game);
    }

    partial void OnDisable()
    {
        if (sensorManager != null && listener != null)
        {
            sensorManager.UnregisterListener(listener);
        }

        listener?.Dispose();
        listener = null;
        sensorManager = null;
    }

    private sealed class ShakeSensorListener : Java.Lang.Object, ISensorEventListener
    {
        private const float GravityEarth = 9.80665f;
        private const float ShakeThresholdG = 2.7f;
        private const int ShakeSlopTimeMs = 500;

        private readonly MaudeShakeGestureListener owner;
        private long lastShakeTimestamp;

        public ShakeSensorListener(MaudeShakeGestureListener owner)
        {
            this.owner = owner;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 3)
            {
                return;
            }

            var x = e.Values[0] / GravityEarth;
            var y = e.Values[1] / GravityEarth;
            var z = e.Values[2] / GravityEarth;

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
            owner.HandleShake();
        }
    }
}
#endif
