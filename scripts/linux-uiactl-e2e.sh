#!/usr/bin/env bash
#
# Linux AT-SPI end-to-end for uiactl: drive gnome-calculator's accessibility tree
# under a headless Xvfb with the a11y bus running. Asserts --dump sees the buttons
# and --invoke actions them. Gracefully SKIPS (exit 0) if the a11y stack isn't
# present, so it never blocks CI on environments without accessibility.
#
# Requires: xvfb, openbox, dbus-x11, at-spi2-core, python3-pyatspi, gnome-calculator, dotnet.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

UIACTL="$(find src/uiactl/bin -name uiactl.dll -path '*/net10.0/*' 2>/dev/null | head -1)"
if [ -z "$UIACTL" ]; then echo "SKIP: uiactl not built"; exit 0; fi
if ! command -v gnome-calculator >/dev/null 2>&1 || ! python3 -c "import pyatspi" >/dev/null 2>&1; then
  echo "SKIP: AT-SPI stack (gnome-calculator / python3-pyatspi) not installed"; exit 0
fi

export DISPLAY=:99
export GTK_MODULES=gail:atk-bridge GNOME_ACCESSIBILITY=1
eval "$(dbus-launch --sh-syntax)" 2>/dev/null || true

cleanup() { pkill gnome-calculator 2>/dev/null || true; pkill -f "Xvfb :99" 2>/dev/null || true; pkill openbox 2>/dev/null || true; }
trap cleanup EXIT

Xvfb :99 -screen 0 1280x800x24 >/tmp/uiactl-xvfb.log 2>&1 & sleep 2
openbox >/tmp/uiactl-ob.log 2>&1 & sleep 1
gnome-calculator >/tmp/uiactl-calc.log 2>&1 & sleep 5

echo "== uiactl --dump: read the accessibility tree =="
buttons="$(dotnet "$UIACTL" --window gnome-calculator --dump --max 80 2>/dev/null | grep -c 'push button')"
if [ "${buttons:-0}" -lt 5 ]; then
  echo "FAIL: expected several push buttons, saw ${buttons:-0}"; exit 1
fi
echo "PASS: AT-SPI dump saw $buttons push buttons"

echo "== uiactl --invoke: action buttons via AT-SPI =="
for k in 7 + 7 =; do
  if ! dotnet "$UIACTL" --window gnome-calculator --invoke --name "$k" --control-type Button >/dev/null 2>&1; then
    echo "FAIL: could not invoke button '$k'"; exit 1
  fi
done
echo "PASS: invoked 7 + 7 = through AT-SPI"

echo "== uiactl AT-SPI e2e: ALL PASS =="
