using System;
namespace Maude.Runtime.Views.Telemetry
{
    public class ChartHoverEventArgs : EventArgs
	{
		public ChartHoverEventArgs(ChartHoverEvent @event, Point viewLocation)
		{
            Event = @event;
            ViewLocation = viewLocation;
        }

        public ChartHoverEvent Event { get; }

        public Point ViewLocation { get; }
    }
}

