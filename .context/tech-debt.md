# Tech Debt

## Test coverage gaps

- Tests for audcap, vidcap, and playbk are Windows-only and require real hardware (audio device, display, ffmpeg)
- No integration tests for cross-platform paths (macOS/Linux ffmpeg capturers)
- outcap tests only cover option parsing, not actual capture/merge logic
- inpctl tests only cover window matching and key mapping, not actual input sending
- waitfr, imgfnd, spkbak test projects exist but have no tests yet (require live desktop / hardware)

## Cross-platform (Linux) — known debt & un-closable gaps

The whole toolkit runs on Linux (X11). Status of the known items:

- ~~**Window-find was triplicated**~~ — **RESOLVED**: `inpctl`, `scrcap` and the window
  locator now share one X11 search (`IdleOps.Shared.Windowing.LinuxX11Windows.SearchId`);
  `IWindowLocator.Resolve` exposes the raw handle. Verified behind the Linux e2e scripts.
- ~~**playbk not self-contained on a Linux bundle**~~ — **RESOLVED**: copy-targets pick the
  right sibling TFM per build, and the CI publish co-locates the sibling tools into
  `publish/playbk` so the downloaded bundle is self-contained.
- **stpcap records coordinates, not semantic steps on Linux** — *deliberately deferred.*
  The Windows recorder uses UIA element-at to emit resilient `invoke`/`click-text` steps.
  Doing the same on Linux means resolving AT-SPI element-at per click, which needs the
  accessibility bus running *during recording*, couples stpcap to uiactl's helper (or a
  move-the-helper-into-shared refactor), adds per-click latency, and AT-SPI element-at is
  flaky. Poor effort/value/risk for a niche gain when coordinates already work — revisit
  only if resilient Linux recordings become a real need.

**Can't be closed on X11 (documented per tool, not debt to pay down):**

- **inpctl `--background` is best-effort** — X `XSendEvent` is ignored by many apps for
  security; there is no true no-focus-steal equivalent to Windows PostMessage.
- **Wayland is unsupported** — xdotool/XTEST need X11; Wayland has no global window
  addressing. Would require a Wayland-native approach (compositor protocols).
- **macOS backends** are stubs (input, per-window capture, AT-SPI/AX) — the next frontier,
  needs a Mac to build against.

## CI

`.github/workflows/build.yml` builds+tests the full solution on Windows and the
cross-platform tools on ubuntu + macOS, with Linux e2e scripts (`scripts/linux-*.sh`)
running the tools under Xvfb. No GitLab pipeline yet.

## playbk `wait-window` silently ignores `text:` field

The `wait-window` action in a playbk YAML accepts a `text:` field (based on
the API shape of the underlying `waitfr.exe` CLI which has a `--text`
flag), but playbk's implementation only waits for the window handle to
exist and never does the OCR check. The field is silently ignored — no
warning, no error.

Reproduction:

```yaml
steps:
  - name: Wait for main menu to render
    action: wait-window
    window: "MyApp*"
    text: "Start"         # ← silently no-op
    timeout: 45
```

Runtime log output:

```
Step: Wait for main menu to render
  Waiting for window 'MyApp*' (timeout 45s)...
  Window 'MyApp*' found.
```

Note the absence of any text-check line. `waitfr.exe --text ...` would
normally poll OCR until the text matches; `wait-window` action calls a
handle-only code path and succeeds as soon as the window exists.

**Impact:** discovered by a consumer writing playbooks against Town Spirit
— the playbook took screenshots too early (while the Godot splash screen
was still visible instead of the main menu) and clicks landed on the
wrong surface. The consumer had to replace every `wait-window` with a
raw `exec: waitfr.exe --window "..." --text "..."` which works
correctly.

**Fixes (any of):**

1. Make `wait-window` honor the `text:` field by shelling out to
   `waitfr.exe --text` when set — matches user expectations
2. Fail loudly if `text:` is provided but OCR isn't implemented, so
   the user sees "action wait-window does not support text: field, use
   exec: waitfr.exe --text instead" and knows to switch paths
3. Remove the `text:` field from the action's schema entirely and
   document that users should use `exec: waitfr.exe` for OCR waits

Option 1 is the least surprising. The examples in
`src/playbk/inputs/notepad-hello-world.idleops.yaml` already use
`wait-window` without `text:` — they don't hit this bug — but a new
playbook author would reasonably assume the field works.

## playbk OCR misses stylized text

`txtfnd` (wrapped by both `click-text` and `waitfr --text`) uses
`Windows.Media.Ocr`, which has known gaps for stylized/decorative fonts.
Encountered on Town Spirit's main menu: every menu item OCR'd cleanly
except "Start", which was in the same font as every other label but at
a slightly smaller size or with a different kerning pass.

Workaround the consumer found: pick a different on-screen text as the
landmark (OCR'd `Continue` instead of `Start`, then clicked Start at a
computed offset). This is a consumer-side workaround, not an idleops
bug — but it's worth documenting alongside the `imgfnd` fallback so
users hit by the same issue know the escape hatches.

Possible improvements to the toolkit:

- `click-text` could accept a `fallback_coords: "x,y"` field that fires
  when the OCR text isn't found, so playbook authors don't have to drop
  into raw `exec` for this case
- `txtfnd` could try multiple OCR engines (Tesseract fallback) when the
  Windows engine returns zero matches
