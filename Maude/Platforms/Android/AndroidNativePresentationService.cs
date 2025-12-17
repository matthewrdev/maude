#if ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using SkiaSharp.Views.Android;

namespace Maude;

internal sealed class AndroidNativePresentationService : IMaudePresentationService
{
    private readonly MaudeOptions options;
    private readonly IMaudeDataSink dataSink;
    private readonly Func<Activity?> activityProvider;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
    private BottomSheetDialog? sheet;
    private NativeOverlayHost? overlayHost;

    public AndroidNativePresentationService(MaudeOptions options, IMaudeDataSink dataSink, Func<Activity?> activityProvider)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.dataSink = dataSink ?? throw new ArgumentNullException(nameof(dataSink));
        this.activityProvider = activityProvider ?? throw new ArgumentNullException(nameof(activityProvider));
    }

    public bool IsPresentationEnabled => true;

    public bool IsSheetPresented => sheet != null && sheet.IsShowing;

    public bool IsOverlayPresented => overlayHost?.IsVisible == true;

    public void PresentSheet()
    {
        var activity = activityProvider();
        if (activity == null)
        {
            MaudeLogger.Error("PresentSheet failed: no current Activity registered.");
            return;
        }

        activity.RunOnUiThread(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                DismissSheetInternal();

                sheet = new BottomSheetDialog(activity);
                var root = BuildSheetLayout(activity);
                sheet.SetContentView(root);
                sheet.SetOnDismissListener(new OnDismissListener(() =>
                {
                    CleanupSheet(root);
                    sheet = null;
                }));

                sheet.Show();
                ExpandSheet(sheet);
            }
            finally
            {
                semaphore.Release();
            }
        });
    }

    public void DismissSheet()
    {
        var activity = activityProvider();
        if (activity == null)
        {
            return;
        }

        activity.RunOnUiThread(async () =>
        {
            await semaphore.WaitAsync();
            try
            {
                DismissSheetInternal();
            }
            finally
            {
                semaphore.Release();
            }
        });
    }

    public void PresentOverlay(MaudeOverlayPosition position)
    {
        var activity = activityProvider();
        if (activity == null)
        {
            MaudeLogger.Error("PresentOverlay failed: no current Activity registered.");
            return;
        }

        activity.RunOnUiThread(() =>
        {
            overlayHost ??= new NativeOverlayHost(activity);
            overlayHost.Show(dataSink, position);
        });
    }

    public void DismissOverlay()
    {
        var activity = activityProvider();
        if (activity == null || overlayHost == null)
        {
            return;
        }

        activity.RunOnUiThread(() =>
        {
            overlayHost.Hide();
        });
    }

    private ViewGroup BuildSheetLayout(Activity activity)
    {
        var metrics = activity.Resources?.DisplayMetrics ?? new DisplayMetrics();
        var paddingPx = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 12, metrics);
        var buttonPadding = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, metrics);
        var buttonCorner = (float)TypedValue.ApplyDimension(ComplexUnitType.Dip, 10, metrics);

        var root = new LinearLayout(activity)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(paddingPx, paddingPx, paddingPx, paddingPx);

        var headerRow = new LinearLayout(activity)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        headerRow.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(activity)
        {
            Text = "MEMORY OVERVIEW",
            TextSize = 18f,
            Typeface = Android.Graphics.Typeface.DefaultBold,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
        };
        headerRow.AddView(title);

        Button? copyButton = null;
        if (options.SaveSnapshotAction != null)
        {
            var action = options.SaveSnapshotAction;
            copyButton = CreatePillButton(activity, action.Label, buttonPadding, buttonCorner);
            copyButton.Text = string.IsNullOrWhiteSpace(action.Label) ? "COPY" : action.Label;
            copyButton.Click += async (_, _) => await ExecuteSaveSnapshotAsync(dataSink, action);
            headerRow.AddView(copyButton);
        }

        var overlayButton = CreatePillButton(activity, "OVERLAY", buttonPadding, buttonCorner);
        overlayButton.Click += (_, _) =>
        {
            if (MaudeRuntime.IsChartOverlayPresented)
            {
                MaudeRuntime.DismissOverlay();
            }
            else
            {
                MaudeRuntime.PresentOverlay();
            }
        };
        headerRow.AddView(overlayButton);

        root.AddView(headerRow);

        var chart = new MaudeNativeChartViewAndroid(activity)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, DpToPx(activity, 220)),
            RenderMode = MaudeChartRenderMode.Inline,
            WindowDuration = TimeSpan.FromSeconds(60),
            DataSink = dataSink
        };
        root.AddView(chart);

        var eventsList = new RecyclerView(activity)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f)
        };
        var adapter = new MaudeEventAdapter(dataSink);
        eventsList.SetAdapter(adapter);
        eventsList.SetLayoutManager(new LinearLayoutManager(activity));
        root.AddView(eventsList);

        return root;
    }

    private void CleanupSheet(ViewGroup root)
    {
        foreach (var view in root.GetChildren())
        {
            if (view is MaudeNativeChartViewAndroid chart)
            {
                chart.Detach();
            }
        }
    }

    private void DismissSheetInternal()
    {
        if (sheet == null)
        {
            return;
        }

        try
        {
            sheet.Dismiss();
        }
        catch { }
        finally
        {
            sheet = null;
        }
    }

    private static void ExpandSheet(BottomSheetDialog dialog)
    {
        var sheet = dialog.FindViewById(Resource.Id.design_bottom_sheet);
        if (sheet is FrameLayout frameLayout)
        {
            var behavior = BottomSheetBehavior.From(frameLayout);
            if (behavior is BottomSheetBehavior b)
            {
                b.State = BottomSheetBehavior.StateExpanded;
                b.SkipCollapsed = true;
                b.SetPeekHeight(frameLayout.Resources?.DisplayMetrics?.HeightPixels ?? 0, true);
            }
        }
    }

    public void Dispose()
    {
        DismissSheetInternal();
        overlayHost?.Hide();
        overlayHost = null;
        semaphore.Dispose();
    }

    private static int DpToPx(Context context, float dp) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources?.DisplayMetrics);

    private static Button CreatePillButton(Context context, string text, int paddingPx, float cornerRadiusPx)
    {
        var button = new Button(context)
        {
            Text = text,
            TextSize = 14f,
            Typeface = Android.Graphics.Typeface.DefaultBold,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        };
        button.SetTextColor(Android.Graphics.Color.White);
        var drawable = new Android.Graphics.Drawables.GradientDrawable();
        drawable.SetColor(ToColor(MaudeConstants.MaudeBrandColor_Faded));
        drawable.SetCornerRadius(cornerRadiusPx);
        button.Background = drawable;
        button.SetPadding(paddingPx, paddingPx / 2, paddingPx, paddingPx / 2);
        return button;
    }

    private static async Task ExecuteSaveSnapshotAsync(IMaudeDataSink dataSink, MaudeSaveSnapshotAction action)
    {
        try
        {
            var snapshot = dataSink.Snapshot();
            await action.CopyDelegate(snapshot);
        }
        catch (Exception ex)
        {
            MaudeLogger.Error("Failed to execute save snapshot action.");
            MaudeLogger.Exception(ex);
        }
    }

    private static Android.Graphics.Color ToColor(Color color) =>
        Android.Graphics.Color.Argb(color.Alpha, color.Red, color.Green, color.Blue);

    private sealed class OnDismissListener : Java.Lang.Object, IDialogInterfaceOnDismissListener
    {
        private readonly Action callback;
        public OnDismissListener(Action callback) => this.callback = callback;
        public void OnDismiss(IDialogInterface? dialog) => callback();
    }
}

internal sealed class NativeOverlayHost
{
    private readonly Activity activity;
    private MaudeNativeChartViewAndroid? chart;

    public NativeOverlayHost(Activity activity) => this.activity = activity;

    public bool IsVisible { get; private set; }

    public void Show(IMaudeDataSink sink, MaudeOverlayPosition position)
    {
        chart ??= new MaudeNativeChartViewAndroid(activity)
        {
            RenderMode = MaudeChartRenderMode.Overlay,
            WindowDuration = TimeSpan.FromMinutes(1)
        };
        chart.DataSink = sink;

        var decor = activity.Window?.DecorView as ViewGroup;
        if (decor == null)
        {
            return;
        }

        if (chart.Parent is ViewGroup parent)
        {
            parent.RemoveView(chart);
        }

        var lp = new FrameLayout.LayoutParams(DpToPx(activity, 320), DpToPx(activity, 180))
        {
            Gravity = ToGravity(position),
            LeftMargin = DpToPx(activity, 12),
            RightMargin = DpToPx(activity, 12),
            TopMargin = DpToPx(activity, 12),
            BottomMargin = DpToPx(activity, 12),
        };

        decor.AddView(chart, lp);
        chart.BringToFront();
        IsVisible = true;
    }

    public void Hide()
    {
        if (chart?.Parent is ViewGroup parent)
        {
            parent.RemoveView(chart);
        }
        IsVisible = false;
    }

    private static int DpToPx(Context context, float dp) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources?.DisplayMetrics);

    private static GravityFlags ToGravity(MaudeOverlayPosition position) => position switch
    {
        MaudeOverlayPosition.TopLeft => GravityFlags.Top | GravityFlags.Left,
        MaudeOverlayPosition.TopRight => GravityFlags.Top | GravityFlags.Right,
        MaudeOverlayPosition.BottomLeft => GravityFlags.Bottom | GravityFlags.Left,
        _ => GravityFlags.Bottom | GravityFlags.Right
    };
}

internal sealed class MaudeEventAdapter : RecyclerView.Adapter
{
    private readonly IMaudeDataSink sink;
    private List<MaudeEventDisplay> cached = new();

    public MaudeEventAdapter(IMaudeDataSink sink)
    {
        this.sink = sink;
        sink.OnEventsUpdated += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        var channelLookup = sink.Channels?.ToDictionary(c => c.Id) ?? new Dictionary<byte, MaudeChannel>();
        cached = sink.Events
                     .OrderByDescending(e => e.CapturedAtUtc)
                     .Take(50)
                     .Select(e => new MaudeEventDisplay
                     {
                         Label = e.Label,
                         Symbol = MaudeEventLegend.GetSymbol(e.Type),
                         ChannelColor = channelLookup.TryGetValue(e.Channel, out var channel) ? channel.Color : new Color(0, 0, 0),
                         Details = e.Details,
                         HasDetails = !string.IsNullOrWhiteSpace(e.Details),
                         Timestamp = e.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss")
                     })
                     .ToList();

        NotifyDataSetChanged();
    }

    public override int ItemCount => cached.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is EventViewHolder vh && position < cached.Count)
        {
            vh.Bind(cached[position]);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var ctx = parent.Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var symbol = new TextView(ctx) { TextSize = 18 };
        var title = new TextView(ctx) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        var details = new TextView(ctx) { TextSize = 14 };
        var timestamp = new TextView(ctx) { TextSize = 12 };

        root.AddView(symbol);
        root.AddView(title);
        root.AddView(details);
        root.AddView(timestamp);

        return new EventViewHolder(root, title, details, timestamp, symbol);
    }

    private sealed class EventViewHolder : RecyclerView.ViewHolder
    {
        private readonly TextView title;
        private readonly TextView details;
        private readonly TextView timestamp;
        private readonly TextView symbol;

        public EventViewHolder(View itemView, TextView title, TextView details, TextView timestamp, TextView symbol) : base(itemView)
        {
            this.title = title;
            this.details = details;
            this.timestamp = timestamp;
            this.symbol = symbol;
        }

        public void Bind(MaudeEventDisplay display)
        {
            title.Text = display.Label;
            details.Text = display.Details;
            details.Visibility = display.HasDetails ? ViewStates.Visible : ViewStates.Gone;
            timestamp.Text = display.Timestamp;
            symbol.Text = display.Symbol;
        }
    }
}

internal static class ViewGroupExtensions
{
    public static IEnumerable<View> GetChildren(this ViewGroup viewGroup)
    {
        for (int i = 0; i < viewGroup.ChildCount; i++)
        {
            var child = viewGroup.GetChildAt(i);
            if (child != null)
            {
                yield return child;
                if (child is ViewGroup vg)
                {
                    foreach (var nested in vg.GetChildren())
                    {
                        yield return nested;
                    }
                }
            }
        }
    }
}
#endif
