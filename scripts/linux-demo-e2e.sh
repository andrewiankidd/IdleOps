#!/usr/bin/env bash
#
# Linux demo end-to-end: a recorded automation session driving a real GUI app.
#
# Unlike the other e2e scripts, which prove a single tool starts and does not crash, this
# one composes the suite the way a user would and leaves a watchable artifact behind:
#
#   vidcap  records the X display for the whole session
#   uiactl  drives gnome-calculator through the accessibility tree (7 + 7 =)
#   scrcap  captures the window
#   txtfnd  reads the result back off the screen with OCR and asserts it says 14
#
# Each tool plays to its strength: AT-SPI for pressing buttons (OCR cannot read the small
# key glyphs reliably — measured), OCR for the large high-contrast result display.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

find_dll() { find "src/$1/bin" -name "$1.dll" -path '*/net10.0/*' 2>/dev/null | head -1; }
UIACTL="$(find_dll uiactl)"; TXTFND="$(find_dll txtfnd)"
SCRCAP="$(find_dll scrcap)"; VIDCAP="$(find_dll vidcap)"
for t in UIACTL TXTFND SCRCAP VIDCAP; do
  [ -n "${!t}" ] || { echo "SKIP: ${t,,} not built"; exit 0; }
done
command -v gnome-calculator >/dev/null 2>&1 || { echo "SKIP: gnome-calculator not installed"; exit 0; }

export DISPLAY=:99
export GTK_MODULES=gail:atk-bridge GNOME_ACCESSIBILITY=1
eval "$(dbus-launch --sh-syntax)" 2>/dev/null || true

work="$(mktemp -d)"

# Keep the recording and stills for CI. E2E_ARTIFACTS unset (a local run) is a no-op.
collect_artifacts() {
  [ -n "${E2E_ARTIFACTS:-}" ] || return 0
  dest="$E2E_ARTIFACTS/demo"
  mkdir -p "$dest"
  cp -r "$work"/. "$dest/" 2>/dev/null || true
}
cleanup() {
  collect_artifacts
  pkill -f vidcap.dll 2>/dev/null || true
  pkill gnome-calculator 2>/dev/null || true
  pkill -f "Xvfb :99" 2>/dev/null || true
  pkill openbox 2>/dev/null || true
  rm -rf "$work"
}
trap cleanup EXIT

Xvfb :99 -screen 0 1280x800x24 >"$work/xvfb.log" 2>&1 & sleep 2
openbox >"$work/openbox.log" 2>&1 & sleep 1

# Record the whole session. --timer bounds it so the run cannot hang waiting on ffmpeg;
# the demo below finishes well inside it and we wait the remainder out.
RECORD_SECONDS=35
echo "== vidcap: recording the session =="
dotnet "$VIDCAP" --timer "$RECORD_SECONDS" -o "$work/demo.mp4" >"$work/vidcap.log" 2>&1 &
VIDCAP_PID=$!
sleep 3   # let ffmpeg attach to the display before anything interesting happens

gnome-calculator >"$work/calc.log" 2>&1 &

# Poll for the accessibility tree rather than sleeping at it: registration is ~2s idle but
# unbounded on a loaded runner.
echo "== waiting for the accessibility tree =="
buttons=0
for _ in $(seq 1 30); do
  buttons="$(dotnet "$UIACTL" --window gnome-calculator --dump --max 80 2>/dev/null | grep -c 'push button' || true)"
  [ "${buttons:-0}" -ge 5 ] && break
  sleep 1
done
[ "${buttons:-0}" -ge 5 ] || { echo "FAIL: accessibility tree never appeared (saw ${buttons:-0})"; exit 1; }
echo "PASS: $buttons buttons exposed"

echo "== uiactl: pressing 7 + 7 = =="
for k in 7 + 7 =; do
  dotnet "$UIACTL" --window gnome-calculator --invoke --name "$k" --control-type Button >/dev/null 2>&1 \
    || { echo "FAIL: could not invoke '$k'"; exit 1; }
  sleep 1
done

echo "== scrcap: capturing the result =="
dotnet "$SCRCAP" --window "*Calculator*" --output "$work/result.png" >/dev/null 2>&1 \
  || { echo "FAIL: could not capture the calculator window"; exit 1; }

echo "== txtfnd: reading the answer back off the screen =="
if coords="$(dotnet "$TXTFND" --window "*Calculator*" --text "14" 2>/dev/null)"; then
  echo "PASS: OCR read 14 from the display at ${coords}"
else
  echo "FAIL: OCR did not find '14' on the calculator display"
  exit 1
fi

echo "== waiting for the recording to finish =="
wait "$VIDCAP_PID" 2>/dev/null || true

if [ ! -s "$work/demo.mp4" ]; then
  echo "FAIL: no recording was produced"; cat "$work/vidcap.log"; exit 1
fi
if command -v ffprobe >/dev/null 2>&1; then
  info="$(ffprobe -v error -show_entries stream=width,height,nb_frames -of csv=p=0 "$work/demo.mp4" 2>/dev/null | head -1)"
  echo "PASS: recording is valid ($info, $(du -h "$work/demo.mp4" | cut -f1))"
fi

echo "== demo e2e: ALL PASS =="
