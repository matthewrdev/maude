using UIKit;
using Foundation;
using Redpoint.Mobile.Navigation;
using Redpoint.Mobile.Popups;
using UIModalPresentationStyle = UIKit.UIModalPresentationStyle;

namespace Redpoint.Mobile;

public sealed class MaudePopup : NSObject
{
    private readonly UIViewController hostViewController; 
    private readonly UIViewController sheetViewController;
    private readonly SheetDelegate sheetDelegate;
    private bool isClosed;

    public MaudePopup(PopupView popupContent, UIView platformView, UIViewController hostController)
    {
        this.popupContent = popupContent ?? throw new ArgumentNullException(nameof(popupContent));
        hostViewController   = hostController ?? throw new ArgumentNullException(nameof(hostController));
        sheetDelegate         = new SheetDelegate(this);

        // — Build the sheet —
        sheetViewController = new UIViewController
        {
            ModalPresentationStyle = UIModalPresentationStyle.PageSheet,
            View = platformView ?? throw new ArgumentNullException(nameof(platformView))
        };

        if (sheetViewController.SheetPresentationController is UISheetPresentationController sheet)
        {
            sheet.Detents = new[]
            {
                UISheetPresentationControllerDetent.CreateMediumDetent(),
                UISheetPresentationControllerDetent.CreateLargeDetent()
            };
            sheet.PrefersGrabberVisible = true;
            sheet.Delegate = sheetDelegate;          // Capture dismiss events
        }
    }
    
    public void Expand()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (sheetViewController.SheetPresentationController is UISheetPresentationController sheet)
            {
                // You can use a predefined Large detent, or a custom full screen one.
                sheet.SelectedDetentIdentifier = UISheetPresentationControllerDetentIdentifier.Large;
            }
        });
    }

    
    public event EventHandler OnOpened;
    public event EventHandler OnClosed;

    /// <summary>
    /// Presents the sheet.  (Not part of <see cref="IPopup"/>, but symmetrical with Android’s Show().)
    /// </summary>
    public void Show()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            hostViewController.PresentViewController(sheetViewController, true, () =>
            {
            });
        });
    }

    /// <inheritdoc />
    public void Close()
    {
        MainThread.BeginInvokeOnMainThread(SafeDismiss);
    }
    
    internal void SafeDismiss()
    {
        if (isClosed)
        {
            return;
        }
        
        isClosed = true;

        sheetViewController.DismissViewController(true, NotifyClosed);
    }

    // Internal delegate that bridges UIKit’s dismissal callback back to us.
    private sealed class SheetDelegate : UISheetPresentationControllerDelegate
    {
        private readonly SlideSheetPopup _owner;
        internal SheetDelegate(SlideSheetPopup owner) => _owner = owner;

        public override void WillPresent(UIPresentationController presentationController, UIModalPresentationStyle style, IUIViewControllerTransitionCoordinator transitionCoordinator)
        {
            _owner.NotifyOpened();
        }

        public override void DidDismiss(UIPresentationController presentationController)
        {
            _owner.NotifyClosed();
        }
    }

    private void NotifyOpened()
    {
        if (this.popupContent is IPopupAware popupAware)
        {
            popupAware.OnPopupOpened();
        }
        
        this.OnOpened?.Invoke(this, EventArgs.Empty);
    }

    private void NotifyClosed()
    {
        if (this.popupContent is IPopupAware popupAware)
        {
            popupAware.OnPopupClosed();
        }
        
        this.OnClosed?.Invoke(this, EventArgs.Empty);
    }
}
