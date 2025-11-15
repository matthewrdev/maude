using System;

namespace Maude;

public interface IMaudePopup
{
    void Close();

    event EventHandler OnClosed;
}
