// .NET WASM browser-app bootstrap. Loads the runtime, runs the font spike, then
// initializes the WebGL2 canvas and drives a requestAnimationFrame render loop.
// Follows the `dotnet new wasmbrowser` layout.
import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
await runMain();

const P = exports.ZGF.Gui.Web.Program;

// Font spike panel.
try {
    document.getElementById('out').textContent = P.RunFontSpike();
} catch (e) {
    document.getElementById('out').textContent = 'spike threw on the JS boundary: ' + e;
}

// Canvas: logical CSS size (from the style attribute) with a device-pixel backing store.
const canvas = document.getElementById('zgf-canvas');
const LOGICAL_W = parseInt(canvas.style.width, 10) || 800;
const LOGICAL_H = parseInt(canvas.style.height, 10) || 600;

function applyBacking(dpr) {
    canvas.width = Math.round(LOGICAL_W * dpr);
    canvas.height = Math.round(LOGICAL_H * dpr);
}

let dpr = window.devicePixelRatio || 1;
applyBacking(dpr);
await P.StartAsync('#zgf-canvas', LOGICAL_W, LOGICAL_H, dpr);

function frame(ts) {
    P.Tick(ts);
    requestAnimationFrame(frame);
}
requestAnimationFrame(frame);

// Handle DPI changes (e.g. moving the window between monitors).
window.addEventListener('resize', () => {
    const next = window.devicePixelRatio || 1;
    if (next === dpr) return;
    dpr = next;
    applyBacking(dpr);
    P.Resize(LOGICAL_W, LOGICAL_H, dpr);
});
