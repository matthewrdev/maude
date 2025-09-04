using System;
using Ansight.Studio.Telemetry;

namespace Maude.Runtime.Views.Telemetry
{
	public class ChartClickedEventArgs : EventArgs
	{
		public ChartClickedEventArgs(ChartPosition chartPosition)
		{
            ChartPosition = chartPosition;
        }

        public ChartPosition ChartPosition { get; }
    }
}

