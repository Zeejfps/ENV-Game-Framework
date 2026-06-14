// File access shim for ZGF.Gui.Web: a hidden <input type=file> for the picker
// plus a handle table that holds picked/dropped File objects so the .NET side can
// read their bytes later. main.js owns the DOM drag-drop listeners (it has the
// assembly exports) and uses registerFiles() here to stash the dropped files.
//
// Key browser constraint: openPicker() calls input.click() synchronously, so it
// MUST be invoked from within a user gesture (a click handler) or the browser
// blocks the dialog. See docs / Files/WebFilePicker.cs.
//
// STATUS: scaffolding — never run here.

let hiddenInput = null;
const files = [];      // handle -> File
const buffers = [];    // handle -> Uint8Array (after loadFile)

function ensureInput() {
    if (hiddenInput) return hiddenInput;
    hiddenInput = document.createElement('input');
    hiddenInput.type = 'file';
    hiddenInput.style.display = 'none';
    document.body.appendChild(hiddenInput);
    return hiddenInput;
}

function meta(list) {
    const out = [];
    for (let i = 0; i < list.length; i++) {
        const f = list[i];
        const h = files.push(f) - 1;
        out.push({ h: h, name: f.name, size: f.size, type: f.type || '' });
    }
    return JSON.stringify(out);
}

// Opens the OS file picker. MUST be called synchronously inside a user gesture.
// Resolves with a JSON metadata array ([] if cancelled).
export function openPicker(multiple, accept) {
    const input = ensureInput();
    input.multiple = !!multiple;
    input.accept = accept || '';
    input.value = ''; // allow re-picking the same file

    return new Promise(resolve => {
        let settled = false;
        const done = json => { if (!settled) { settled = true; cleanup(); resolve(json); } };
        const onChange = () => done(meta(input.files || []));
        const onCancel = () => done('[]');
        function cleanup() {
            input.removeEventListener('change', onChange);
            input.removeEventListener('cancel', onCancel);
        }
        input.addEventListener('change', onChange, { once: true });
        input.addEventListener('cancel', onCancel, { once: true }); // not in every browser
        input.click(); // synchronous — relies on the live user gesture
    });
}

// Stash a DataTransfer FileList (from a drop) and return its JSON metadata.
// Called from main.js inside the DOM 'drop' handler.
export function registerFiles(fileList) {
    return meta(fileList || []);
}

// Read the whole file into a JS-side buffer; returns its byte length. Kept
// separate from readInto so the .NET MemoryView copy below is fully synchronous
// (a MemoryView must not be held across an await — wasm memory can move).
export async function loadFile(handle) {
    const f = files[handle];
    if (!f) return 0;
    const buf = new Uint8Array(await f.arrayBuffer());
    buffers[handle] = buf;
    return buf.length;
}

// Copy the previously-loaded bytes into the .NET span (synchronous).
export function readInto(handle, dest) {
    const buf = buffers[handle];
    if (buf) dest.set(buf);
}

export function freeFile(handle) {
    delete files[handle];
    delete buffers[handle];
}
