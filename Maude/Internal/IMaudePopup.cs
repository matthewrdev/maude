using System;

namespace Maude;

internal interface IMaudePopup
{
    /// <summary>
    /// Closes the popup.
    /// </summary>
    void Close();

    /// <summary>
    /// Raised when the popup is closed or dismissed.
    /// </summary>
    event EventHandler OnClosed;
}
