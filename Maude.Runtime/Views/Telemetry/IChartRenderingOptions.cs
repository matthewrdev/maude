using System;
using System.Collections.Generic;
using SkiaSharp;


namespace Maude.Runtime.Views.Telemetry
{
    public interface IChartRenderingOptions
    {
        IReadOnlyDictionary<string, SKColor> GroupColors { get; }

        SKColor GetGroupColor(string groupName);

        SKColor BackgroundColor { get; }

        SKColor IntervalBarColor { get; }

        SKColor IntervalTextColor { get; }

        SKColor AxesBarColor { get; }

        SKColor AxesTextColor { get; }

        SKColor PositionBarColor { get; }

        SKColor PositionTextColor { get; }

        SKColor HoverBarColor { get; }

        float TextSize { get; }

        float ValueTextSize { get; }
    }
}

