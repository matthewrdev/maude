using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Ansight.Adb.Sessions.Data.Repositories;
using Ansight.Adb.Sessions.Data.Utilities;
using SQLite;
using SQLitePCL;

namespace Maude.Runtime.Telemetry.Data
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITelemetryRepository))]
    public class TelemetryRepository : EntityRepository<Telemetry>, ITelemetryRepository
    {
        public IReadOnlyList<string> GetChannelNames(SQLiteConnection connection)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            return connection.QueryScalars<string>($"SELECT DISTINCT {nameof(Telemetry.Channel)} FROM {TableName}");
        }

        public IReadOnlyList<string> GetGroupsForChannel(SQLiteConnection connection, string channel)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            return connection.QueryScalars<string>($"SELECT DISTINCT [{nameof(Telemetry.Group)}] FROM {TableName} WHERE {nameof(Telemetry.Channel)} = ?", channel);
        }

        public IReadOnlyList<TelemetryDataPoint> GetSegmentData(SQLiteConnection connection, Guid segmentId)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var query = $"SELECT {nameof(Telemetry.CapturedAtUtc)}, {nameof(Telemetry.Value)} {nameof(Telemetry.Data)} FROM {TableName} WHERE {nameof(Telemetry.Segment)} = @segmentId ORDER BY {nameof(Telemetry.CapturedAtUtc)} ASC";
            var parameters = new Dictionary<string, object>()
            {
                { "@segmentId", segmentId }
            };

            return SqlHelper.ExecuteCancellableQuery(connection, query, parameters, Map, CancellationToken.None);
        }

        private TelemetryDataPoint Map(sqlite3_stmt statement)
        {
            var capturedAtUtc = SQLite3.ColumnInt64(statement, 0);
            var value = SQLite3.ColumnDouble(statement, 1);
            var data = SQLite3.ColumnString(statement, 2);

            return new TelemetryDataPoint(new DateTime(capturedAtUtc), value, data);
        }

        public IReadOnlyList<Guid> GetSegmentsForChannelAndGroup(SQLiteConnection connection, string channel, string group)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (string.IsNullOrEmpty(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
            }

            return connection.QueryScalars<Guid>($"SELECT DISTINCT {nameof(Telemetry.Segment)} FROM {TableName} WHERE {nameof(Telemetry.Channel)} = ? AND [{nameof(Telemetry.Group)}] = ?", channel, group);
        }

        public void Insert(SQLiteConnection connection, string channel, string group, Guid segment, TelemetryDataPoint dataPoint)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (string.IsNullOrEmpty(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
            }

            var statement = "INSERT INTO Telemetry (Channel, [Group], Segment, CapturedAtUtc, Value, Data) VALUES (?,?,?,?,?,?)";

            connection.Execute(statement, channel, group, segment, dataPoint.DateTimeUtc, dataPoint.Value, dataPoint.Data);
        }

        public void Insert(SQLiteConnection connection, string channel, string group, Guid segment, IReadOnlyList<TelemetryDataPoint> dataPoints)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (string.IsNullOrEmpty(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
            }

            if (dataPoints is null)
            {
                throw new ArgumentNullException(nameof(dataPoints));
            }

            if (!dataPoints.Any())
            {
                return;
            }

            foreach (var dataPoint in dataPoints)
            {
                Insert(connection, channel, group, segment, dataPoint);
            }
        }
    }
}