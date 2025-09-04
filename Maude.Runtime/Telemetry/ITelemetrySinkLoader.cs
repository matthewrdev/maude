using System;
using Ansight.Adb.Devices;
using Ansight.Adb.Sessions;
using Ansight.Adb.Sessions.Data;
using Ansight.Adb.Sessions.Replayable;

namespace Maude.Runtime.Telemetry
{
    public interface ITelemetrySinkLoader
    {
        ITelemetrySink Load(ISession session);
        ITelemetrySink Load(IDevice device, string packageId, Guid sessionId, ISessionDatabase database);
        ITelemetrySink Load(IDevice device, string packageId, Guid sessionId, SQLite.SQLiteConnection connection);
    }
}

