// Browser clipboard shim for ZGF.Gui.Web (see Input/WebClipboard.cs). Reads
// require a user gesture + permission; both calls are guarded so a missing or
// blocked clipboard API degrades to a no-op / empty string rather than throwing.
export function writeText(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).catch(() => {});
    }
}

export async function readText() {
    try {
        if (navigator.clipboard && navigator.clipboard.readText) {
            return await navigator.clipboard.readText();
        }
    } catch {
        // ignore — denied or unavailable
    }
    return '';
}
