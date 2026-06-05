# linux-x64 GLFW native

Drop `libglfw.so.3` here. The csproj bundles it into `linux-x64` (and RID-less
Linux dev) builds, and `NativeLibraryResolver` loads it by its soname.

GLFW's upstream release archives only ship Windows/macOS binaries, so take the
Linux `.so` from the Ubuntu package and commit the resolved file:

```bash
sudo apt-get update && sudo apt-get install -y libglfw3
# copy the real file the soname symlink points at, named as the soname:
cp "$(readlink -f /usr/lib/x86_64-linux-gnu/libglfw.so.3)" \
   framework/Glfw.NET/Native/linux-x64/libglfw.so.3
```

Built against Ubuntu's system X11/Wayland libs, so it runs on the matching
Ubuntu release line and newer. Rebuild from a newer base if you bump the
minimum supported Ubuntu version.
