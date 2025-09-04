using System;
using System.ComponentModel.Composition;
using Ansight.Adb.Devices;
using Ansight.Adb.Sessions;
using Ansight.Adb.Sessions.Data;
using Ansight.Adb.Sessions.Data.Utilities;
using Ansight.Adb.Telemetry;
using Ansight.Adb.Telemetry.Data;
using Ansight.Utilities;
using SQLite;


namespace Maude.Runtime.Telemetry
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITelemetrySinkLoader))]
    public class TelemetrySinkLoader : ITelemetrySinkLoader
    {
        private readonly Lazy<ITelemetryRepository> telemetryRepository;
        public ITelemetryRepository TelemetryRepository => telemetryRepository.Value;

        [ImportingConstructor]
        public TelemetrySinkLoader(Lazy<ITelemetryRepository> telemetryRepository)
        {
            this.telemetryRepository = telemetryRepository;
        }

        public ITelemetrySink Load(ISession session)
        {
            if (session is null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return Load(session.Device, session.AppId, session.Id, session.Database);
        }

        public ITelemetrySink Load(IDevice device,
                                   string packageId,
                                   Guid sessionId,
                                   ISessionDatabase database)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (database is null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (!database.DatabaseExists)
            {
                return TelemetrySink.Empty(device.Serial, packageId);
            }

            return Load(device, packageId, sessionId, database.Connection);
        }

        public ITelemetrySink Load(IDevice device,
                                   string packageId,
                                   Guid sessionId,
                                   SQLiteConnection connection)
        {
            if (device is null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (!SqlHelper.TableExists(connection, TelemetryRepository.TableName))
            {
                return TelemetrySink.Empty(device.Serial, packageId);
            }

            using (Profiler.Profile($"Loading Telemetry Sink (Session='{sessionId}',Device='{device.Serial}',Package='{packageId}')"))
            {
                var sink = new TelemetrySink(device.Serial, packageId, isEditable: true);

                var channelNames = TelemetryRepository.GetChannelNames(connection);

                foreach (var channelName in channelNames)
                {
                    var channel = sink.CreateChannel(channelName);

                    var groupNames = TelemetryRepository.GetGroupsForChannel(connection, channelName);
                    foreach (var groupName in groupNames)
                    {
                        var segmentIds = TelemetryRepository.GetSegmentsForChannelAndGroup(connection, channelName, groupName);

                        foreach (var segmentId in segmentIds)
                        {
                            var segmentData = TelemetryRepository.GetSegmentData(connection, segmentId);

                            if (segmentData.Count == 0)
                            {
                                continue;
                            }

                            var segment = channel.OpenSegmentForGroup(groupName, segmentData[0].DateTimeUtc);
                            segment.AddData(segmentData);
                            segment.CloseEditing();
                        }
                    }
                }

                sink.CloseEditing();

                return sink;
            }
        }
    }
}