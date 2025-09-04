using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
	public interface ITelemetryPreferences
	{
		/// <summary>
		/// Occurs when the observed channels for a specific package ID changes. 
		/// </summary>
		event EventHandler<TelemetryChannelPreferencesChangedEventArgs> TelemetryChannelPreferencesChanged;

		/// <summary>
		/// Occurs when the visible groups for a specific channel changes.
		/// </summary>
        event EventHandler<TelemetryExcludedGroupsPreferencesChangedEventArgs> TelemetryExcludedGroupPreferencesChanged;

		/// <summary>
		/// Returns the 
		/// </summary>
		/// <param name="packageId"></param>
		/// <returns></returns>
        IReadOnlyList<string> GetChannelsForPackageId(string packageId);

		/// <summary>
		/// Checks if the <paramref name="channel"/> is enabled for the given <paramref name="packageId"/>.
		/// </summary>
		bool HasChannelEnabled(string packageId, string channel);

		void AddChannel(string packageId, string channel);

		void RemoveChannel(string packageId, string channel);

		/// <summary>
		/// Gets the excluded groups for a telemetry channel.
		/// </summary>
		IReadOnlyList<string> GetExcludedGroupsForChannel(string channel);

		/// <summary>
		/// Changes the enabled/visible <paramref name="groups"/> for the <paramref name="channel"/>.
		/// </summary>
		void SetExcludedGroupsForChannel(string channel, IReadOnlyList<string> groups);

		/// <summary>
		/// Add the given <paramref name="group"/> to the excluded groups for the <paramref name="channel"/>.
		/// </summary>
        void AddExcludedGroup(string channel, string group);

        /// <summary>
        /// Removes the given <paramref name="group"/> from the excluded groups for the <paramref name="channel"/>.
        /// </summary>
        void RemoveExcludedGroup(string channel, string group);

		/// <summary>
		/// Checks if the <paramref name="group"/> is within the exclusion list for the <paramref name="channel"/>.
		/// </summary>
		bool IsExcludedGroup(string channel, string group);
    }
}

