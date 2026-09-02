using System;

namespace System.BluetoothLe
{
    /// <summary>
    /// The library's diagnostic sink.
    /// </summary>
    /// <remarks>
    /// Until version 4.0 this had no default implementation, and the per-platform types that were supposed to
    /// install one were static classes that nothing ever touched - a static constructor only runs on first
    /// access, so <see cref="TraceImplementation"/> stayed null and every trace call in the library was a no-op
    /// in every shipped configuration. A default is now installed here, on every target framework, so that
    /// tracing works out of the box and no platform file is needed to enable it.
    /// </remarks>
    public static class Trace
    {
        /// <summary>
        /// Receives every trace message the library emits. Assign your own to bridge to a logging framework,
        /// or set it to <see langword="null"/> to silence the library entirely.
        /// </summary>
        /// <remarks>
        /// The first argument is the finished message: <see cref="Message"/> has already applied
        /// <see cref="string.Format(string, object[])"/> when there were arguments to substitute. The second
        /// argument carries the original arguments for a sink that wants to log them structurally, and an
        /// implementation that only writes text should ignore it. Formatting deliberately happens before the
        /// call rather than inside the sink, because most call sites in this library pass an already
        /// interpolated string; handing those to a sink that formats again throws <see cref="FormatException"/>
        /// on any message containing a literal brace.
        /// </remarks>
        public static Action<string, object[]> TraceImplementation { get; set; } = DefaultSink;

        /// <summary>
        /// Emits a diagnostic message. Never throws: a failing sink must not take down a GATT callback.
        /// </summary>
        public static void Message(string format, params object[] args)
        {
            try
            {
                var implementation = TraceImplementation;
                if (implementation == null)
                    return;

                // Most call sites interpolate the string themselves and pass no arguments. Those messages
                // routinely contain literal braces (device names, JSON fragments, byte arrays), so running them
                // through string.Format would throw rather than trace.
                var message = args is { Length: > 0 } ? string.Format(format, args) : format;

                implementation(message, args);
            }
            catch { /* a diagnostic sink must never be able to fail an operation */ }
        }

        // System.Diagnostics.Trace rather than Debug.WriteLine: Debug.WriteLine carries
        // [Conditional("DEBUG")], which is evaluated where it is written, so in a Release build of this
        // library the call would be removed and the default sink would be a no-op again - exactly the
        // defect this default exists to fix. Fully qualified because the enclosing type is also named Trace.
        private static void DefaultSink(string message, object[] args)
            => global::System.Diagnostics.Trace.WriteLine(message, "BluetoothLe");
    }
}
