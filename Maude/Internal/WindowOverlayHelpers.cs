#if ANDROID || IOS
namespace Maude;

internal static class WindowOverlayHelpers
{
    public static bool TryAddOverlay(Window? window, WindowOverlay overlay)
    {
        if (window == null) return false;

        // Prefer official API if present
        var addOverlay = window.GetType().GetMethod("AddOverlay", new[] { typeof(WindowOverlay) });
        if (addOverlay != null)
        {
            addOverlay.Invoke(window, new object[] { overlay });
            return true;
        }

        var handler = window.Handler;
        var addMethod = handler?.GetType().GetMethod("AddOverlay", new[] { typeof(WindowOverlay) });
        if (addMethod != null)
        {
            addMethod.Invoke(handler, new object[] { overlay });
            return true;
        }

        return false;
    }

    public static bool TryRemoveOverlay(Window? window, WindowOverlay overlay)
    {
        if (window == null) return false;

        var removeOverlay = window.GetType().GetMethod("RemoveOverlay", new[] { typeof(WindowOverlay) });
        if (removeOverlay != null)
        {
            removeOverlay.Invoke(window, new object[] { overlay });
            return true;
        }

        var handler = window.Handler;
        var removeMethod = handler?.GetType().GetMethod("RemoveOverlay", new[] { typeof(WindowOverlay) });
        if (removeMethod != null)
        {
            removeMethod.Invoke(handler, new object[] { overlay });
            return true;
        }

        return false;
    }
}
#endif