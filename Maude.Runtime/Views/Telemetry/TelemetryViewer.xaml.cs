using System;
using System.Collections.Generic;
using Ansight.Adb.Telemetry;
using Ansight.Studio.Telemetry;
using Ansight.TimeZones;
using Ansight.Utilities;
using Microsoft.Maui.Controls.Xaml;

namespace Maude.Runtime.Views.Telemetry
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TelemetryViewer : Component
    {
        public TelemetryViewer()
        {
            InitializeComponent();

            var defaultLabels = new Dictionary<string, string>()
                            {
                                { "Memory", "KB"},
                                { "CPU", "Percent"},
                                { "Rendering", "Views"},
                            };

            telemetryChartCollection.ChartAxesLabels = new ChartAxesLabels(defaultLabels);
        }
    }
}

