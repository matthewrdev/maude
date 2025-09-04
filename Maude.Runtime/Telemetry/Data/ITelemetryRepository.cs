using System;
using System.Collections.Generic;
using Ansight.Adb.Sessions.Data.Repositories;
using SQLite;

namespace Maude.Runtime.Telemetry.Data
{
    public interface ITelemetryRepository : IEntityRepository<Telemetry>
    {
        void Insert(SQLiteConnection connection, string channel, string group, Guid segment, TelemetryDataPoint dataPoint);

        void Insert(SQLiteConnection connection, string channel, string group, Guid segment, IReadOnlyList<TelemetryDataPoint> dataPoint);

        IReadOnlyList<string> GetChannelNames(SQLiteConnection connection);

        IReadOnlyList<string> GetGroupsForChannel(SQLiteConnection connection, string channel);

        IReadOnlyList<Guid> GetSegmentsForChannelAndGroup(SQLiteConnection connection, string channel, string group);

        IReadOnlyList<TelemetryDataPoint> GetSegmentData(SQLiteConnection connection, Guid segmentId);
    }
}

