using Xilium.CefGlue;

namespace ZGF.WebView.Cef;

/// <summary>
/// The <see cref="CefApp"/> that runs inside CEF's render process — i.e. inside the
/// <c>zgf-cef-helper</c> subprocess, into which this file is linked as shared source. It installs the
/// renderer half of the JS↔C# bridge: a <c>window.zgf</c> object whose <c>postMessage(string)</c>
/// hops to the browser process (page → C#), and an <c>onmessage</c> slot the browser process calls
/// back into (C# → page).
/// <para>Self-contained on purpose (only CefGlue types) so the helper never drags in the rest of
/// ZGF.WebView.Cef.</para>
/// </summary>
public sealed class ZgfCefRenderApp : CefApp
{
    /// <summary>Process message the page's <c>window.zgf.postMessage</c> sends to the browser process.</summary>
    public const string QueryMessage = "zgf_query";

    private readonly ZgfRenderProcessHandler _renderProcessHandler = new();

    protected override CefRenderProcessHandler GetRenderProcessHandler() => _renderProcessHandler;

    private sealed class ZgfRenderProcessHandler : CefRenderProcessHandler
    {
        protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
            var global = context.GetGlobal();
            var zgf = CefV8Value.CreateObject(null, null);
            zgf.SetValue("postMessage",
                CefV8Value.CreateFunction("postMessage", new PostMessageHandler()),
                CefV8PropertyAttribute.None);
            // The page assigns window.zgf.onmessage = fn; the browser process calls it via injected JS.
            zgf.SetValue("onmessage", CefV8Value.CreateNull(), CefV8PropertyAttribute.None);
            global.SetValue("zgf", zgf, CefV8PropertyAttribute.None);
        }
    }

    private sealed class PostMessageHandler : CefV8Handler
    {
        protected override bool Execute(
            string name, CefV8Value obj, CefV8Value[] arguments,
            out CefV8Value returnValue, out string exception)
        {
            returnValue = CefV8Value.CreateUndefined();
            exception = null!;

            if (arguments.Length >= 1 && arguments[0].IsString)
            {
                var context = CefV8Context.GetCurrentContext();
                var frame = context.GetFrame();
                using var message = CefProcessMessage.Create(QueryMessage);
                message.Arguments.SetString(0, arguments[0].GetStringValue());
                frame.SendProcessMessage(CefProcessId.Browser, message);
            }

            return true;
        }
    }
}
