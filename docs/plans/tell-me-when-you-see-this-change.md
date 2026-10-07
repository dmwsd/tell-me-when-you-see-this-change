# Plan: "Tell Me When You See This Change"

## Context
A small Windows utility for control engineers. They pick one screen pixel (for example an HMI indicator for a prox), arm the alarm, and walk away. While that pixel differs from its captured color, the app sounds a steady tone. The look is minimalist industrial with some steampunk: dark iron, brass, rivets, an indicator lamp.

The folder is empty except for `prompts/`. The machine has no .NET SDK, only runtimes. As the user chose, we build with the **built-in `csc.exe`** (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, C# 5) as **WinForms on .NET Framework 4.x**, so nothing needs installing.

**Portability:** this isn't specific to this PC. Every Windows 10 (1903+) and Windows 11 install includes .NET Framework 4.8 or 4.8.1 as an OS component, and it can't be uninstalled. So the single `.exe` can be copied to any Win10/11 machine and run with no runtime install, no installer, and no admin rights. `csc.exe` lives in that same OS folder, so any of those machines can also rebuild it. We avoid any Framework 4.8-only APIs that would break this. (By contrast, a modern .NET 10 app would need the .NET 10 Desktop Runtime installed, or a self-contained build of about 70 MB+.)

**Alert behavior (user's answer):** the tone is **solid on while the pixel differs** from the baseline. It stops on its own if the pixel goes back to the original color, and it stops when the user disarms.

## Files
- `src/Program.cs`: entry point. Calls `Application.EnableVisualStyles()` and runs `MainForm`.
- `src/NativeMethods.cs`: P/Invoke for `GetDC`/`ReleaseDC`/`GetPixel` (gdi32/user32) and `FlashWindowEx`.
- `src/Theme.cs`: palette and drawing helpers. Iron `#1C1A17`, plate `#2A2622`, brass `#B5873A`, brass-hi `#E0B566`, lamp green/amber/red. Helpers draw a riveted brass-bordered panel, a round "gauge" swatch with a brass bezel, an indicator lamp with a glow, and flat brass buttons (custom-painted `Button` subclass). Fonts: Georgia for the title and Consolas for readouts.
- `src/PickerForm.cs`: the pixel selector with a magnifier (details below).
- `src/ToneGenerator.cs`: builds a 1 s, ~880 Hz sine PCM WAV into a `MemoryStream` with a short fade at the loop edges so it doesn't click. Plays it with `System.Media.SoundPlayer.PlayLooping()` and stops with `Stop()`. This gives a gap-free steady tone without NAudio.
- `src/MainForm.cs`: the main utility window.
- `app.manifest`: PerMonitorV2 DPI awareness, so screen coordinates and `GetPixel` use physical pixels on scaled and multi-monitor setups.
- `build.cmd`: one `csc` call with `/target:winexe /optimize /win32manifest:app.manifest /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:bin\TellMeWhenYouSeeThisChange.exe src\*.cs`.

## Main window (`MainForm`)
A small (~320×420) fixed window with an optional Always-on-top toggle.
- Title plate: "TELL ME WHEN YOU SEE THIS CHANGE" in small caps on brass.
- **Target gauge**: a round swatch showing the captured color, with the hex value and X,Y coordinates in Consolas below it.
- **Live gauge**: a smaller swatch showing the current pixel color, updated while armed.
- **Status lamp**: dim = idle, green = ARMED (watching), red pulsing = CHANGED (tone on).
- Buttons: `SELECT PIXEL` and `ARM` / `DISARM`. ARM stays disabled until a pixel is chosen.
- Monitoring uses a `System.Windows.Forms.Timer` at 100 ms. Each tick reads the pixel with `GetDC(IntPtr.Zero)` + `GetPixel`.
  - `CLR_INVALID` (0xFFFFFFFF) or a failed DC (locked screen, UAC desktop) counts as "no reading". It never trips the alarm.
  - Pixel ≠ baseline (exact match): tone starts looping if it isn't already, the lamp turns red, and the taskbar flashes via `FlashWindowEx`.
  - Pixel = baseline: tone stops and the lamp goes back to green.
  - Disarm: timer stops, tone stops, lamp goes dim.

## Picker (`PickerForm`), Snagit-style
1. Hide `MainForm` and wait briefly so it is gone. Then capture `SystemInformation.VirtualScreen` into a bitmap with `Graphics.CopyFromScreen`. This covers every monitor.
2. Show a borderless, TopMost form that covers the whole virtual screen and paints the frozen screenshot, so the image doesn't move while the user aims. The cursor is a crosshair.
3. **Loupe**: a ~170 px square that follows the cursor, offset so it never covers the target and flips at screen edges. It shows 21×21 source pixels at 8× using `InterpolationMode.NearestNeighbor` and `PixelOffsetMode.Half`. It has faint grid lines, a highlighted center cell, and a brass bezel. Under it, a readout strip shows `X,Y` and `#RRGGBB` with a tiny swatch.
4. Controls:
   - Left-click or Enter selects the pixel.
   - Arrow keys nudge 1 px (Shift = 10 px) using `Cursor.Position`.
   - Esc or right-click cancels.
5. The form returns the screen coordinate (virtual-screen origin offset applied) and the color read from the frozen bitmap. The main window then re-reads the live color as the baseline, to avoid a mismatch from cursor or overlay artifacts.
6. Paint is double-buffered and only invalidates the old and new loupe rectangles, so it stays smooth on large multi-monitor desktops.

## Notes and limits (README not required; mention in the final summary)
- An exclusive-fullscreen DirectX app, the locked screen, or a minimized RDP session can't be read. These count as "no reading", not as a change.
- A window moved on top of the pixel counts as a change, which is intended.

## Verification
1. Run `build.cmd` and confirm `bin\TellMeWhenYouSeeThisChange.exe` builds with no errors.
2. Launch it. Click SELECT PIXEL and check:
   - the loupe magnifies correctly on each monitor;
   - arrow nudging works;
   - Esc cancels;
   - the selected hex matches a known color (for example, open Paint with a solid red fill).
3. Arm on a pixel of a Paint canvas, change that pixel's color, and check that the steady tone starts and the lamp turns red. Undo (Ctrl+Z) and check that the tone stops and the lamp turns green. Disarm while the tone is on and check that it stops.
4. Check DPI: with display scaling at 125–150%, the picked coordinates must still hit the intended pixel.
