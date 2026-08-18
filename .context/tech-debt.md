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

## Python helpers on Linux

The Linux backends originally shelled out to two bundled Python scripts, justified as "the
same external-tool model we use for ffmpeg/xdotool/tesseract". That analogy was weak: those
are pre-existing tools the user installs, whereas these were *our own code* shipped in
another language, carrying an install-time dependency and blocking single-binary packaging.

- **stpcap — resolved.** XRecord is a plain C API in libXtst (no D-Bus, no GObject), and
  this repo already hand-rolls COM vtables for UIA, so P/Invoke was well within its normal
  interop. `LinuxInputRecorder` now calls libXtst directly and `python3-xlib` is gone.
  Verified under Xvfb producing byte-identical playbooks to the Python version.
- **uiactl — kept, deliberately.** AT-SPI2 is a D-Bus protocol; the native equivalent means
  reimplementing the `org.a11y.atspi.*` surface over a D-Bus client, or P/Invoking libatspi
  (GObject: refcounting, `GError**`, introspection types). `pyatspi` collapses that into
  ~190 lines, which is real leverage rather than laziness. The script is now an
  **embedded resource** unpacked to a temp path on first use, so uiactl still publishes as
  a single executable — but `python3-pyatspi` remains a runtime dependency, and the backend
  degrades to "element not found" without it.

**Can't be closed on X11 (documented per tool, not debt to pay down):**

- **inpctl `--background` is best-effort** — X `XSendEvent` is ignored by many apps for
  security; there is no true no-focus-steal equivalent to Windows PostMessage.
- **Wayland is unsupported** — xdotool/XTEST need X11; Wayland has no global window
  addressing. Would require a Wayland-native approach (compositor protocols).

## Cross-platform (macOS) — verified, with remaining gaps

The macOS backends were written blind and have now been run on real hardware (macOS 26,
Apple silicon, Retina). Everything except `stpcap` works end-to-end; the bugs found while
verifying are fixed and covered below. Remaining debt:

- **stpcap has no macOS recorder** — `InputRecorderFactory` returns null and the CLI exits
  with a clear message. Recording needs a CGEventTap; unlike injection (where `cliclick`
  serves as a CLI shim) there is no off-the-shelf CLI, so this means either a native helper
  or P/Invoke into ApplicationServices. The one genuinely unimplemented backend.
- **uiactl `--element-at` is unsupported on macOS** — System Events exposes no hit-test by
  screen point. Would need AXUIElementCopyElementAtPosition via P/Invoke, i.e. the same
  native-helper decision as stpcap. `--dump` + name/role selection covers most uses.
- **uiactl `--invoke` cannot confirm its effect** — AppleScript `click` returns success even
  when the target ignores it (verified: raw `click` on a TextEdit menu button reports success
  and opens nothing), so unlike Windows' InvokePattern a successful exit is weaker evidence.
  Assert the resulting state rather than trusting the verb.
- **avfoundation device warm-up is charged to `--timer`** — opening an avfoundation input
  takes ~1.5–2.5s, and `AudcapService`/`VidcapService` start the countdown with
  `cts.CancelAfter` before ffmpeg has opened the device, so `--timer 8` yields ~5.5s of
  audio. *Deliberately not fixed here*: the countdown lives above the capturer abstraction
  and is shared with outcap's A/V sync, so moving it to "start when ffmpeg reports the input
  is open" is a cross-platform timing change that cannot be validated on Windows from a Mac.
  Documented in the audcap/vidcap READMEs as "ask for a little more than you need".

**Fixed while verifying** (each was a silent-wrong-answer bug, not a crash):

- macOS Retina captures are native pixels while input is points, so every OCR/template
  coordinate was 2× too large — `CaptureOutcome.Scale` now carries the factor and
  `ImageTextFinder`/`imgfnd` convert before emitting.
- One inaccessible process (a sandboxed/virtualization app) raised `-25211` and aborted the
  *entire* System Events window enumeration, hiding every window belonging to an app that
  happened to sort after it. Both enumeration loops are now guarded.
- `uiactl` reported `ok` for actions on windows and elements that did not exist; element
  lookup used a `whose` filter over `entire contents` (a list — raises at runtime), iterated
  a live specifier instead of a materialized list, and dropped every row whose `name` was
  `missing value`.
- `inpctl --hold` emitted `kd:<letter>`, which `cliclick` rejects — only modifiers can be
  held, and that is now what the code claims.
- `spkbak --output` passed `--file-format=WAVE`, which `say` rejects outright; it needs a
  `--data-format`.
- avfoundation device indices were hardcoded (`1` for screen, `:0` for audio) despite being
  machine-specific; both are resolved by device name now.
- `spkbak.Tests` could not restore off Windows (NETSDK1100), and `WindowingTests` still
  asserted macOS had no window locator — together these were failing the macOS/Linux CI job.

## CI

`.github/workflows/build.yml` builds+tests the full solution on Windows and the
cross-platform tools on ubuntu + macOS, with Linux e2e scripts (`scripts/linux-*.sh`)
running the tools under Xvfb. No GitLab pipeline yet.

A fifth Linux e2e (`scripts/linux-demo-e2e.sh`) is a *recorded* session rather than a
tool-by-tool smoke test: vidcap records the X display while uiactl drives gnome-calculator
through the accessibility tree and txtfnd reads the answer back off the screen with OCR.
Each tool is used where it is actually good — AT-SPI presses the keys because OCR cannot
read the small key glyphs (measured: tesseract returns only the window chrome), OCR reads
the large result display. The recording is attached to the rolling `latest-main` release,
which is recreated on every push, so exactly one current demo video exists at a stable URL.

**No macOS e2e, and there can't easily be one.** Every macOS backend needs a TCC grant
(Accessibility / Screen Recording / Microphone) against the *app* running the tools, which
a hosted runner cannot give — an unattended `screencapture` just fails, and `cliclick`
succeeds while doing nothing. So the macOS job is build + unit tests only, and the
end-to-end behaviour is verified by hand on real hardware. The unit suites are the guard
that matters here: keep the pure logic (key translation, device-listing parse, OCR
coordinate maths) tested, because it is the only macOS-relevant thing CI can check.

Note the macOS/Linux CI job was red from the cross-platform commit until the `spkbak.Tests`
restore and `WindowingTests` fixes above — worth checking that job actually passes before
trusting a green-looking run.

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
