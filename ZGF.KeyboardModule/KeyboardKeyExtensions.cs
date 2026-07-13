namespace ZGF.KeyboardModule;

// KeyboardKey.ToChar used to live here: a US-QWERTY lookup that turned a physical key into a
// character. It was the app's only text-entry path, which made every non-US layout untypable —
// pressing Й (the physical Q key) inserted 'q'. Text now comes from the OS text-input event
// (TextInputEvent, fed by GLFW's character callback), which is already layout-, modifier- and
// dead-key-resolved. Don't reintroduce a key→char decoder.
