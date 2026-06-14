// .NET WASM browser-app bootstrap. Loads the runtime, runs the font spike,
// initializes the WebGL2 canvas, attaches DOM input, and drives the render loop.
// Follows the `dotnet new wasmbrowser` layout.
import { dotnet } from './_framework/dotnet.js'
import { registerFiles } from './files.js'

const { getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
await runMain();

const P = exports.ZGF.Gui.Web.Program;
const I = exports.ZGF.Gui.Web.Input.WebInput;

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

// ---- DOM input -> WebInput (coords converted to canvas-logical, Y-up GUI space) ----
function guiCoords(ev) {
    const rect = canvas.getBoundingClientRect();
    const x = ev.clientX - rect.left;
    const yTop = ev.clientY - rect.top;
    return { x: x, y: LOGICAL_H - yTop };
}
function modBits(ev) {
    return (ev.shiftKey ? 1 : 0) | (ev.ctrlKey ? 2 : 0) | (ev.altKey ? 4 : 0) | (ev.metaKey ? 8 : 0);
}

canvas.addEventListener('pointermove', e => { const p = guiCoords(e); I.PointerMove(p.x, p.y); });
canvas.addEventListener('pointerdown', e => {
    const p = guiCoords(e);
    I.PointerDown(e.button, p.x, p.y, modBits(e));
    if (canvas.setPointerCapture) canvas.setPointerCapture(e.pointerId);
});
canvas.addEventListener('pointerup', e => { const p = guiCoords(e); I.PointerUp(e.button, p.x, p.y, modBits(e)); });
canvas.addEventListener('pointerenter', () => I.PointerEnter());
canvas.addEventListener('pointerleave', () => I.PointerLeave());
canvas.addEventListener('wheel', e => { I.Wheel(e.deltaX, e.deltaY); e.preventDefault(); }, { passive: false });
canvas.addEventListener('contextmenu', e => e.preventDefault());
window.addEventListener('keydown', e => I.KeyDown(e.code, modBits(e)));
window.addEventListener('keyup', e => I.KeyUp(e.code, modBits(e)));
window.addEventListener('blur', () => I.Blur());

// ---- file upload: picker (click) + drag-and-drop ----
const D = exports.ZGF.Gui.Web.Files.WebFileDrop;

// The click must stay inside the user gesture so the picker can open: call straight
// through to C#, which triggers input.click() synchronously before any await.
canvas.addEventListener('click', e => { const p = guiCoords(e); P.HandleClick(p.x, p.y); });

canvas.addEventListener('dragenter', e => { e.preventDefault(); });
canvas.addEventListener('dragover', e => {
    e.preventDefault();
    if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy';
    const p = guiCoords(e);
    D.DragOver(p.x, p.y);
});
canvas.addEventListener('dragleave', () => D.DragLeave());
canvas.addEventListener('drop', e => {
    e.preventDefault();
    const p = guiCoords(e);
    const json = registerFiles(e.dataTransfer ? e.dataTransfer.files : []);
    D.Drop(p.x, p.y, json);
});

// ---- render loop ----
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
