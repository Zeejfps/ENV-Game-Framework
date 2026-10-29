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

    /// <summary>Key of the browser's extra info listing the scripts run before each document's own.</summary>
    public const string DocumentStartScriptsKey = "zgf_document_start_scripts";

    private readonly ZgfRenderProcessHandler _renderProcessHandler = new();

    protected override CefRenderProcessHandler GetRenderProcessHandler() => _renderProcessHandler;

    private sealed class ZgfRenderProcessHandler : CefRenderProcessHandler
    {
        private readonly Dictionary<int, string[]> _documentStartScripts = new();

        protected override void OnBrowserCreated(CefBrowser browser, CefDictionaryValue? extraInfo)
        {
            var list = extraInfo?.GetList(DocumentStartScriptsKey);
            if (list == null)
                return;
            var scripts = new string[list.Count];
            for (var i = 0; i < scripts.Length; i++)
                scripts[i] = list.GetString(i);
            _documentStartScripts[browser.Identifier] = scripts;
        }

        protected override void OnBrowserDestroyed(CefBrowser browser) => _documentStartScripts.Remove(browser.Identifier);

        protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
        {
            if (!frame.IsMain)
                return;
            var global = context.GetGlobal();
            var zgf = CefV8Value.CreateObject(null, null);
            zgf.SetValue("postMessage",
                CefV8Value.CreateFunction("postMessage", new PostMessageHandler()),
                CefV8PropertyAttribute.None);
            // The page assigns window.zgf.onmessage = fn; the browser process calls it via injected JS.
            zgf.SetValue("onmessage", CefV8Value.CreateNull(), CefV8PropertyAttribute.None);
            global.SetValue("zgf", zgf, CefV8PropertyAttribute.None);
            if (!_documentStartScripts.TryGetValue(browser.Identifier, out var scripts))
                return;
            foreach (var script in scripts)
                context.TryEval(script, frame.Url, 1, out _, out _);
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
