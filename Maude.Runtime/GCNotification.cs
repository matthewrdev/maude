namespace Maude.Runtime;

internal static class GCNotification
{
    public static event Action<int>? GCDone;

    private class ReRegister
    {
        private int gen;
        public ReRegister(int generation) => gen = generation;
        ~ReRegister()
        {
            if (GCDone != null)
                GCDone(gen);
            // Re-register to keep getting callbacks
            if (!AppDomain.CurrentDomain.IsFinalizingForUnload() &&
                !Environment.HasShutdownStarted)
            {
                new ReRegister(gen);
            }
        }
    }

    public static void Start()
    {
        new ReRegister(0);
        new ReRegister(2);
    }
}
//
// // Usage:
// GCNotification.GCDone += g => Console.WriteLine($"GC occurred on Gen {g}");
// GCNotification.Start();
