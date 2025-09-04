using System;
using System.Collections.Generic;

namespace Maude.Runtime.Telemetry
{
    public interface IMutableTelemetryChannel : ITelemetryChannel
    {
        bool IsEditable { get; }

        void CloseEditing();

        IMutableTelemetrySegment GetCurrentSegment(string group);

        IMutableTelemetrySegment OpenSegmentForGroup(string group, DateTime startUtc);

        void CloseSegmentForGroup(string group);

        void CloseSegmentForGroups(IReadOnlyList<string> groups);
    }
}

