using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SkiaSharp;


namespace Maude.Runtime.Views.Telemetry
{
    public struct ChartRenderingOptions : IChartRenderingOptions
    {
        private readonly List<string> colorPalette = new List<string>()
        {
            "#fc7a57",
            "#90b494",
            "#1f7a8c",
            "#BFDBF7",
            "#ffb7ff",
            "#5cc8ff",
            "#f6c28b",
            "#C879FF",
            "#5aaa95",
        };

        public static readonly SKColor Memory_Others = SKColor.Parse("#fc7a57");
        public static readonly SKColor Memory_System = SKColor.Parse("#90b494");
        public static readonly SKColor Memory_Stack = SKColor.Parse("#1f7a8c");
        public static readonly SKColor Memory_Graphics = SKColor.Parse("#BFDBF7");
        public static readonly SKColor Memory_Native = SKColor.Parse("#ffb7ff");
        public static readonly SKColor Memory_Java = SKColor.Parse("#5cc8ff");
        public static readonly SKColor Memory_Code = SKColor.Parse("#C879FF");
        public static readonly SKColor Memory_Total = SKColors.WhiteSmoke;

        public static readonly SKColor CPU_Percent = SKColors.WhiteSmoke;

        public static readonly SKColor Rendering_ViewsCount = SKColors.WhiteSmoke;

        public ChartRenderingOptions(IChartRenderingOptions options)
        {
            BackgroundColor = options.BackgroundColor;
            IntervalBarColor = options.IntervalBarColor;
            IntervalTextColor = options.IntervalTextColor;
            AxesBarColor = options.AxesBarColor;
            AxesTextColor = options.AxesTextColor;
            PositionBarColor = options.PositionBarColor;
            HoverBarColor = options.PositionTextColor;
            PositionTextColor = options.HoverBarColor;
            TextSize = options.TextSize;
            ValueTextSize = options.ValueTextSize;
            this.groupColors = options.GroupColors?.ToDictionary(kp => kp.Key, kp => kp.Value) ?? new Dictionary<string, SKColor>();
        }

        public ChartRenderingOptions(SKColor intervalBarColor,
                                     SKColor intervalTextColor,
                                     SKColor axesBarColor,
                                     SKColor axesTextColor,
                                     SKColor positionBarColor,
                                     SKColor positionTextColor,
                                     SKColor hoverBarColor,
                                     SKColor backgroundColor,
                                     float textSize,
                                     float valueTextSize,
                                     IReadOnlyDictionary<string, SKColor> groupColors = null)
        {
            IntervalBarColor = intervalBarColor;
            IntervalTextColor = intervalTextColor;
            AxesBarColor = axesBarColor;
            AxesTextColor = axesTextColor;
            PositionBarColor = positionBarColor;
            PositionTextColor = positionTextColor;
            HoverBarColor = hoverBarColor;
            BackgroundColor = backgroundColor;
            TextSize = textSize;
            ValueTextSize = valueTextSize;
            this.groupColors = (groupColors?.ToDictionary(kp => kp.Key, kp => kp.Value)) ?? new Dictionary<string, SKColor>();
        }

        public SKColor IntervalBarColor { get; }

        public SKColor IntervalTextColor { get; }

        public SKColor AxesBarColor { get; }

        public SKColor AxesTextColor { get; }

        public SKColor PositionBarColor { get; }

        public SKColor PositionTextColor { get; }
        public SKColor HoverBarColor { get; }
        public float TextSize { get; }
        public float ValueTextSize { get; }

        private readonly Dictionary<string, SKColor> groupColors = new Dictionary<string, SKColor>();
        public IReadOnlyDictionary<string, SKColor> GroupColors => groupColors;

        public SKColor BackgroundColor { get; }

        public SKColor GetGroupColor(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentException($"'{nameof(groupName)}' cannot be null or empty.", nameof(groupName));
            }

            if (!GroupColors.TryGetValue(groupName, out var color))
            {
                color = groupColors[groupName] = SKColor.Parse(this.colorPalette[0]);
                colorPalette.RemoveAt(0);
            }

            return color;
        }

        public static ChartRenderingOptions Default { get; } = new ChartRenderingOptions(intervalBarColor: SKColors.SlateGray,
                                                                                         intervalTextColor: SKColors.SlateGray,
                                                                                         axesBarColor: SKColors.WhiteSmoke,
                                                                                         axesTextColor: SKColors.WhiteSmoke,
                                                                                         positionBarColor: SKColors.Tomato,
                                                                                         positionTextColor: SKColors.WhiteSmoke,
                                                                                         hoverBarColor: SKColors.CornflowerBlue,
                                                                                         backgroundColor: SKColor.Parse("#1b1811"),
                                                                                         textSize: (float)Device.GetNamedSize(NamedSize.Body, typeof(Label)) * 0.8f,
                                                                                         valueTextSize: (float)Device.GetNamedSize(NamedSize.Micro, typeof(Label)));


        public static ChartRenderingOptions Memory { get; } = new ChartRenderingOptions(intervalBarColor: SKColors.SlateGray,
                                                                                        intervalTextColor: SKColors.SlateGray,
                                                                                        axesBarColor: SKColors.WhiteSmoke,
                                                                                        axesTextColor: SKColors.WhiteSmoke,
                                                                                        positionBarColor: SKColors.Tomato,
                                                                                        positionTextColor: SKColors.WhiteSmoke,
                                                                                        hoverBarColor: SKColors.CornflowerBlue,
                                                                                        backgroundColor: SKColor.Parse("#1b1811"),
                                                                                        textSize: (float)Device.GetNamedSize(NamedSize.Body, typeof(Label)) * 0.8f,
                                                                                        valueTextSize: (float)Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                                                                                        groupColors: new Dictionary<string, SKColor>()
                                                                                        {
                                                                                            { "Others", Memory_Others },
                                                                                            { "System", Memory_System },
                                                                                            { "Stack", Memory_Stack },
                                                                                            { "Graphics", Memory_Graphics },
                                                                                            { "Native", Memory_Native },
                                                                                            { "Java", Memory_Java },
                                                                                            { "Code", Memory_Code },
                                                                                            { "Total", Memory_Total },
                                                                                        });


        public static ChartRenderingOptions CPU { get; } = new ChartRenderingOptions(intervalBarColor: SKColors.SlateGray,
                                                                                     intervalTextColor: SKColors.SlateGray,
                                                                                     axesBarColor: SKColors.WhiteSmoke,
                                                                                     axesTextColor: SKColors.WhiteSmoke,
                                                                                     positionBarColor: SKColors.Tomato,
                                                                                     positionTextColor: SKColors.WhiteSmoke,
                                                                                     hoverBarColor: SKColors.CornflowerBlue,
                                                                                     backgroundColor: SKColor.Parse("#1b1811"),
                                                                                     textSize: (float)Device.GetNamedSize(NamedSize.Body, typeof(Label)) * 0.8f,
                                                                                     valueTextSize: (float)Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                                                                                     groupColors: new Dictionary<string, SKColor>()
                                                                                     {
                                                                                         { "CPU%", CPU_Percent },
                                                                                     });

        public static ChartRenderingOptions Rendering { get; } = new ChartRenderingOptions(intervalBarColor: SKColors.SlateGray,
                                                                                           intervalTextColor: SKColors.SlateGray,
                                                                                           axesBarColor: SKColors.WhiteSmoke,
                                                                                           axesTextColor: SKColors.WhiteSmoke,
                                                                                           positionBarColor: SKColors.Tomato,
                                                                                           positionTextColor: SKColors.WhiteSmoke,
                                                                                           hoverBarColor: SKColors.CornflowerBlue,
                                                                                           backgroundColor: SKColor.Parse("#1b1811"),
                                                                                           textSize: (float)Device.GetNamedSize(NamedSize.Body, typeof(Label)) * 0.8f,
                                                                                           valueTextSize: (float)Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                                                                                           groupColors: new Dictionary<string, SKColor>()
                                                                                           {
                                                                                               { "Views", Rendering_ViewsCount },
                                                                                           });
    }
}

