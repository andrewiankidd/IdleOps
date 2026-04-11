# Tech Debt

## Test coverage gaps

- Tests for audcap, vidcap, and playbk are Windows-only and require real hardware (audio device, display, ffmpeg)
- No integration tests for cross-platform paths (macOS/Linux ffmpeg capturers)
- outcap tests only cover option parsing, not actual capture/merge logic
- inpctl tests only cover window matching and key mapping, not actual input sending
- waitfr, imgfnd, spkbak test projects exist but have no tests yet (require live desktop / hardware)

## No CI/CD pipeline

No GitHub Actions, Azure Pipelines, or other CI configuration exists. Tests must be run manually.

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
`src/playbk/inputs/crosspose-gui-screenshots.idleops.yaml` already use
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
