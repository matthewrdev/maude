using System;
using System.Collections.Generic;

namespace Maude.Runtime.Views.Telemetry
{
    public class ChartAxesLabels
    {
        public ChartAxesLabels(IReadOnlyDictionary<string, string> labels)
        {
            Labels = labels ?? throw new ArgumentNullException(nameof(labels));
        }

        IReadOnlyDictionary<string, string> Labels { get; }

        public string GetAxisLabelForChannel(string channel)
        {
            if (string.IsNullOrEmpty(channel))
            {
                return String.Empty;
            }

            Labels.TryGetValue(channel, out var label);
            return label;
        }

        public static readonly ChartAxesLabels Empty = new ChartAxesLabels(new Dictionary<string, string>());
    }
}

