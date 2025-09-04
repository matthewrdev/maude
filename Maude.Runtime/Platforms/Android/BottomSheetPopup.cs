using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.View;
using AndroidX.Core.Content;
using Google.Android.Material.BottomSheet;
using Redpoint.Mobile.Navigation;
using Redpoint.Mobile.Popups;

namespace Redpoint.Mobile;

/// <summary>
/// Material Design bottom-sheet dialog that plugs into the shared IPopup abstraction.
/// </summary>
public class BottomSheetPopup : BottomSheetDialog, IPopup
{
    private static readonly Redpoint.Infrastructure.Logging.ILogger log = Infrastructure.Logging.Logger.Create();
    
    private bool isClosed;

    protected BottomSheetPopup(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }
    
    public BottomSheetPopup(Context context)
        : base(context)
    {
    }

    protected BottomSheetPopup(Context context, bool cancelable,
        IDialogInterfaceOnCancelListener? cancelListener)
        : base(context, cancelable, cancelListener) { }

    public BottomSheetPopup(Context context, int theme) : base(context, theme) { }
    
    public PopupView PopupView { get; set; }

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

        if (PopupView is IPopupAware popupAware)
        {
            popupAware.OnPopupOpened();
        }
        
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
                log.Exception(e);
            }

        });
    }

    public override void Dismiss()
    {
        if (isClosed || !this.IsAlive())
        {
            return;
        }

        isClosed = true;
        base.Dismiss();
        
        if (PopupView is IPopupAware popupAware)
        {
            popupAware.OnPopupClosed();
        }
        
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