#!/usr/bin/env bash
#
# Linux (X11) end-to-end smoke test for the cross-platform desktop tools.
# Drives a real xterm under a headless Xvfb display:
#   1. inpctl types text + a Return chord into the window (XTEST foreground path)
#   2. scrcap captures the window and the whole screen (ImageMagick import)
# Asserts the typed text round-tripped and the PNGs are non-empty, exits non-zero
# on any failure. Safe to run in CI (GitHub Actions / GitLab) on ubuntu.
#
# Requires: xdotool, imagemagick (import/identify), xvfb, openbox, xterm, dotnet.
# Uses whatever inpctl/scrcap build is present under bin/ (Debug or Release).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# Match the plain net10.0 output, not net10.0-windows (txtfnd is multi-targeted).
find_dll() { find "src/$1/bin" -name "$1.dll" -path "*/net10.0/*" 2>/dev/null | head -1; }
INPCTL="$(find_dll inpctl)"
SCRCAP="$(find_dll scrcap)"
TXTFND="$(find_dll txtfnd)"
WAITFR="$(find_dll waitfr)"
IMGFND="$(find_dll imgfnd)"
[ -n "$INPCTL" ] || { echo "e2e: inpctl not built (run 'dotnet build src/inpctl')"; exit 1; }
[ -n "$SCRCAP" ] || { echo "e2e: scrcap not built (run 'dotnet build src/scrcap')"; exit 1; }

work="$(mktemp -d)"
typed="$work/typed.txt"
export DISPLAY=:99

# Copy evidence somewhere durable before the work dir is torn down. CI sets
# E2E_ARTIFACTS; unset (a local run) this is a no-op, so nothing is left lying around.
collect_artifacts() {
  [ -n "${E2E_ARTIFACTS:-}" ] || return 0
  dest="$E2E_ARTIFACTS/x11"
  mkdir -p "$dest"
  cp -r "$work"/. "$dest/" 2>/dev/null || true
}
cleanup() { collect_artifacts; pkill -f "Xvfb :99" 2>/dev/null || true; pkill openbox 2>/dev/null || true; rm -rf "$work"; }
trap cleanup EXIT

Xvfb :99 -screen 0 1280x800x24 >"$work/xvfb.log" 2>&1 &
sleep 2
openbox >"$work/openbox.log" 2>&1 &
sleep 1

# An xterm that records the first line typed into it.
xterm -T e2e-demo -e bash -c "read line; echo \"GOT:\$line\" > '$typed'; sleep 5" >"$work/xterm.log" 2>&1 &
sleep 2

echo "== inpctl: type + Return into the window =="
dotnet "$INPCTL" --window "*e2e-demo*" --type "hello-from-idleops"
dotnet "$INPCTL" --window "*e2e-demo*" --keyboard "Return"
sleep 1

got="$(cat "$typed" 2>/dev/null || true)"
if [ "$got" != "GOT:hello-from-idleops" ]; then
  echo "FAIL: input did not round-trip (got: '${got:-<nothing>}')"; exit 1
fi
echo "PASS: input round-tripped ($got)"

echo "== scrcap: capture window + whole screen =="
dotnet "$SCRCAP" --window "*e2e-demo*" --output "$work/window.png"
dotnet "$SCRCAP" --window screen --output "$work/screen.png"
for png in window screen; do
  if ! identify "$work/$png.png" >/dev/null 2>&1; then
    echo "FAIL: $png.png was not a valid image"; exit 1
  fi
  echo "PASS: captured $png.png -> $(identify -format '%wx%h' "$work/$png.png")"
done

if [ -n "$IMGFND" ]; then
  echo "== imgfnd: pure-managed template match =="
  coords="$(dotnet "$IMGFND" --window "*e2e-demo*" --image "$work/window.png" --threshold 0.6 2>/dev/null || true)"
  if printf '%s' "$coords" | grep -qE '^[0-9]+,[0-9]+$'; then
    echo "PASS: imgfnd matched at $coords"
  else
    echo "FAIL: imgfnd found no match (got: '${coords:-<nothing>}')"; exit 1
  fi
else
  echo "SKIP: imgfnd (not built)"
fi

# txtfnd OCR round-trip (only if built for net10.0 and tesseract is present).
if [ -n "$TXTFND" ] && command -v tesseract >/dev/null 2>&1; then
  echo "== txtfnd: OCR-locate on-screen text (tesseract) =="
  xterm -fa Monospace -fs 28 -T ocr-demo -e bash -c "echo IdleOpsWordmark; sleep 15" >"$work/ocr.log" 2>&1 &
  sleep 2
  coords="$(dotnet "$TXTFND" --window "*ocr-demo*" --text "IdleOps" 2>/dev/null || true)"
  if ! printf '%s' "$coords" | grep -qE '^[0-9]+,[0-9]+$'; then
    echo "FAIL: txtfnd did not locate 'IdleOps' (got: '${coords:-<nothing>}')"; exit 1
  fi
  echo "PASS: txtfnd located text at $coords"
else
  echo "SKIP: txtfnd OCR (not built for net10.0 or tesseract missing)"
fi

if [ -n "$WAITFR" ]; then
  echo "== waitfr: window presence polling =="
  if ! dotnet "$WAITFR" --window "*e2e-demo*" --timeout 5 >/dev/null 2>&1; then
    echo "FAIL: waitfr did not detect the present window"; exit 1
  fi
  echo "PASS: waitfr detected the window"
  # A window that never appears must time out (exit 1).
  if dotnet "$WAITFR" --window "*no-such-window-zzz*" --timeout 2 >/dev/null 2>&1; then
    echo "FAIL: waitfr should have timed out on a missing window"; exit 1
  fi
  echo "PASS: waitfr timed out on a missing window"
else
  echo "SKIP: waitfr (not built for net10.0)"
fi

echo "== Linux e2e: ALL PASS =="
