#if MACCATALYST
using System;

namespace Maude;

/// <summary>
/// macCatalyst does not support shake gestures; this implementation is a no-op.
/// </summary>
internal partial class MaudeShakeGestureListener
{
    partial void OnEnable() { }
    partial void OnDisable() { }
}
#endif
