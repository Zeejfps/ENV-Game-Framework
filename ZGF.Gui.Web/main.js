// .NET WASM browser-app bootstrap. Loads the runtime, runs the font spike, and
// wires a requestAnimationFrame loop as the seam for the future WebGL2 render
// loop. Follows the `dotnet new wasmbrowser` layout.
import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

// Start the runtime (invokes Program.Main).
await runMain();

// Step 1: run + show the font validation spike result.
const out = document.getElementById('out');
try {
    out.textContent = exports.ZGF.Gui.Web.Program.RunFontSpike();
} catch (e) {
    out.textContent = 'spike threw on the JS boundary: ' + e;
}

// Step 2: render-loop seam. The WebGL2 backend will draw into #zgf-canvas from
// inside Tick(); today it's a no-op heartbeat.
function frame(ts) {
    exports.ZGF.Gui.Web.Program.Tick(ts);
    requestAnimationFrame(frame);
}
requestAnimationFrame(frame);
