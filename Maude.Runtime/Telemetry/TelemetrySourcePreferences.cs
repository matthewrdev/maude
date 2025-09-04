using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Ansight.Adb.Telemetry;
using Ansight.Adb.Telemetry.Sampling;
using Ansight.Configuration;
using Ansight.Utilities;

namespace Maude.Runtime.Telemetry
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITelemetryPreferences))]
    public class TelemetrySourcePreferences : ITelemetryPreferences, IApplicationLifecycleHandler
    {
        [ImportingConstructor]
        public TelemetrySourcePreferences(Lazy<IUserOptions> userOptions,
                                          Lazy<ITelemetryStreamFactoryRepository> telemetrySources)
        {
            this.userOptions = userOptions;
            this.telemetrySources = telemetrySources;
        }

        private const string packageChannelsKeySuffix = "com.ansight.preferences.telemetry_channels.";

        private const string channelGroupsKeySuffix = "com.ansight.preferences.telemetry_groups.";

        private readonly Lazy<IUserOptions> userOptions;
        public IUserOptions UserOptions => userOptions.Value;

        private readonly Lazy<ITelemetryStreamFactoryRepository> telemetrySources;
        public ITelemetryStreamFactoryRepository TelemetrySources => telemetrySources.Value;

        string BuildPackageChannelsPreferencesKey(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            return $"{packageChannelsKeySuffix}{packageId}";
        }

        string BuildChannelGroupsPreferencesKey(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            return $"{channelGroupsKeySuffix}{packageId}";
        }

        public event EventHandler<TelemetryChannelPreferencesChangedEventArgs> TelemetryChannelPreferencesChanged;
        public event EventHandler<TelemetryExcludedGroupsPreferencesChangedEventArgs> TelemetryGroupPreferencesChanged;
        public event EventHandler<TelemetryExcludedGroupsPreferencesChangedEventArgs> TelemetryExcludedGroupPreferencesChanged;

        public IReadOnlyList<string> GetChannelsForPackageId(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or whitespace.", nameof(packageId));
            }

            var preferencesKey = BuildPackageChannelsPreferencesKey(packageId);
            if (!UserOptions.Contains(preferencesKey))
            {
                return TelemetrySources.Channels;
            }

            var value = UserOptions.Get(preferencesKey, string.Empty);
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value.ParseStringCSV();
        }

        public void AddChannel(string packageId, string channel)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            var preferencesKey = BuildPackageChannelsPreferencesKey(packageId);
            if (!UserOptions.Contains(preferencesKey))
            {
                // All channels are activated already when default value is present, no need to add.
                return;
            }

            List<string> channels = new List<string>();
            var value = UserOptions.Get(preferencesKey, string.Empty);
            if (string.IsNullOrWhiteSpace(value))
            {
                channels.Add(channel);
            }
            else
            {
                channels = value.ParseStringCSV().ToList();
                if (channels.Contains(channel))
                {
                    // Already exists, no need to update.
                    return;
                }


                channels.Add(channel);
            }

            UserOptions.Set(preferencesKey, channels.CreateCsv());
        }

        public void RemoveChannel(string packageId, string channel)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            var preferencesKey = BuildPackageChannelsPreferencesKey(packageId);
            RemoveCsvValue(preferencesKey, channel);
        }

        private void RemoveCsvValue(string key, string value)
        {
            List<string> values = new List<string>();
            if (!UserOptions.Contains(key))
            {
                values = this.TelemetrySources.Channels.ToList();
            }
            else
            {
                values = UserOptions.GetCsv(key, Array.Empty<string>()).ToList();
            }

            if (!values.Contains(value))
            {
                // Channel does not exist in preferences, no need to update.
                return;
            }

            values.Remove(value);
            UserOptions.Set(key, values.CreateCsv());
        }

        public bool HasChannelEnabled(string packageId, string channel)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                throw new ArgumentException($"'{nameof(packageId)}' cannot be null or empty.", nameof(packageId));
            }

            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            return GetChannelsForPackageId(packageId).Contains(channel);
        }

        public void Startup()
        {
            this.UserOptions.OnUserOptionChanged += UserOptions_OnUserOptionChanged;
        }

        private void UserOptions_OnUserOptionChanged(object sender, UserOptionChangedEventArgs e)
        {
            if (e.Key.StartsWith(packageChannelsKeySuffix))
            {
                var channels = UserOptions.GetCsv(e.Key, TelemetrySources.Channels);
                var packageId = e.Key.Remove(0, packageChannelsKeySuffix.Length);
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    // Sanity check but shouldn't happen.
                    return;
                }

                this.TelemetryChannelPreferencesChanged?.Invoke(this, new TelemetryChannelPreferencesChangedEventArgs(packageId, channels));
            }
            else if (e.Key.StartsWith(channelGroupsKeySuffix))
            {
                var excludedGroups = UserOptions.GetCsv(e.Key, Array.Empty<string>());
                var channel = e.Key.Remove(0, channelGroupsKeySuffix.Length);
                if (string.IsNullOrWhiteSpace(channel))
                {
                    // Sanity check but shouldn't happen.
                    return;
                }

                this.TelemetryExcludedGroupPreferencesChanged?.Invoke(this, new TelemetryExcludedGroupsPreferencesChangedEventArgs(channel, excludedGroups));
            }
        }

        public void Shutdown()
        {
            this.UserOptions.OnUserOptionChanged -= UserOptions_OnUserOptionChanged;
        }

        public IReadOnlyList<string> GetExcludedGroupsForChannel(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            var preferencesKey = BuildChannelGroupsPreferencesKey(channel);
            return UserOptions.GetCsv(preferencesKey, Array.Empty<string>());
        }

        public void SetExcludedGroupsForChannel(string channel, IReadOnlyList<string> groups)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or whitespace.", nameof(channel));
            }

            var preferencesKey = BuildChannelGroupsPreferencesKey(channel);
            UserOptions.SetCsv(preferencesKey, groups);
        }

        public void AddExcludedGroup(string channel, string group)
        {
            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (string.IsNullOrEmpty(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
            }

            var groups = GetExcludedGroupsForChannel(channel).ToList();
            groups.Add(group);
            SetExcludedGroupsForChannel(channel, groups.Distinct().ToList());
        }

        public void RemoveExcludedGroup(string channel, string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or whitespace.", nameof(group));
            }

            var groups = GetExcludedGroupsForChannel(channel).ToList();
            groups.Remove(group);
            SetExcludedGroupsForChannel(channel, groups);
        }

        public bool IsExcludedGroup(string channel, string group)
        {
            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException($"'{nameof(channel)}' cannot be null or empty.", nameof(channel));
            }

            if (string.IsNullOrEmpty(group))
            {
                throw new ArgumentException($"'{nameof(group)}' cannot be null or empty.", nameof(group));
            }

            return GetExcludedGroupsForChannel(channel).Contains(group);
        }
    }
}