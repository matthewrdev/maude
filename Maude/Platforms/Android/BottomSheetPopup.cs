using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace Maude;

/// <summary>
/// Material Design bottom-sheet dialog that plugs into the shared IPopup abstraction.
/// </summary>
public class MaudePopup : BottomSheetDialog, IMaudePopup
{
    private bool isClosed;

    protected MaudePopup(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }
    
    public MaudePopup(Context context)
        : base(context)
    {
    }

    protected MaudePopup(Context context, bool cancelable,
        IDialogInterfaceOnCancelListener? cancelListener)
        : base(context, cancelable, cancelListener) { }

    public MaudePopup(Context context, int theme) : base(context, theme) { }
    
    public MaudeView PopupView { get; set; }

    /// <inheritdoc />
    public void Close()
    {
        this.Dismiss();
    }

    public event EventHandler? OnOpened;
    public event EventHandler? OnClosed;

    public override void Show()
    {
        base.Show();

        OnOpened?.Invoke(this, EventArgs.Empty);
        
        var sheet = FindViewById<FrameLayout>(Resource.Id.design_bottom_sheet);
        if (sheet == null)
        {
            return;
        }

        sheet.Post(() =>
        {
            try
            {
                // 1) Allow full height
                var lp = sheet.LayoutParameters;
                lp.Height = ViewGroup.LayoutParams.MatchParent;
                sheet.LayoutParameters = lp;
            
                var behavior = BottomSheetBehavior.From(sheet);

                if (behavior is BottomSheetBehavior sheetBehavior)
                {
                    sheetBehavior.State = BottomSheetBehavior.StateExpanded;

                    // Optional: Make sure it can go full screen
                    sheetBehavior.SkipCollapsed = true;
                    sheetBehavior.SetPeekHeight(Context.Resources.DisplayMetrics.HeightPixels, true);
                }
            }
            catch (Exception e)
            {
            }

        });
    }

    public override void Dismiss()
    {
        if (isClosed || this.Handle == IntPtr.Zero)
        {
            return;
        }

        isClosed = true;
        base.Dismiss();
        
        OnClosed?.Invoke(this, EventArgs.Empty);
    }
    
    public void Expand()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(Expand);
            return;
        }
        
        var bottomSheet = this.FindViewById(Resource.Id.design_bottom_sheet);

        if (bottomSheet is FrameLayout sheetLayout)
        {
            var behavior = BottomSheetBehavior.From(sheetLayout);

            if (behavior is BottomSheetBehavior sheetBehavior)
            {
                sheetBehavior.State = BottomSheetBehavior.StateExpanded;

                // Optional: Make sure it can go full screen
                sheetBehavior.SkipCollapsed = true;
                sheetBehavior.SetPeekHeight(Context.Resources.DisplayMetrics.HeightPixels, true);
            }
        }
    }
}