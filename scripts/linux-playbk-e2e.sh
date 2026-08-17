#!/usr/bin/env bash
#
# Linux end-to-end for the playbk orchestrator: run a real playbook under a headless
# Xvfb (launch a window, wait for it, type into it, screenshot it) and assert the
# screenshot was produced. Proves playbk drives the cross-platform tools on Linux.
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

PLAYBK="$(find src/playbk/bin -name playbk.dll -path '*/net10.0/*' 2>/dev/null | head -1)"
if [ -z "$PLAYBK" ]; then echo "SKIP: playbk not built for net10.0"; exit 0; fi

# playbk shells out to inpctl (type/click) and txtfnd (OCR); find them on PATH.
for d in inpctl scrcap txtfnd; do
  b="$(find "src/$d/bin" -name "$d.dll" -path '*/net10.0/*' 2>/dev/null | head -1)"
  [ -n "$b" ] && export PATH="$(dirname "$b"):$PATH"
done
have_ocr=0; command -v tesseract >/dev/null 2>&1 && [ -n "$(find src/txtfnd/bin -name txtfnd.dll -path '*/net10.0/*' 2>/dev/null | head -1)" ] && have_ocr=1

export DISPLAY=:99
work="$(mktemp -d)"
cleanup() { pkill xterm 2>/dev/null || true; pkill -f "Xvfb :99" 2>/dev/null || true; pkill openbox 2>/dev/null || true; rm -rf "$work"; }
trap cleanup EXIT

Xvfb :99 -screen 0 1280x800x24 >"$work/xvfb.log" 2>&1 & sleep 2
openbox >"$work/ob.log" 2>&1 & sleep 1

# Helper the xterm runs (avoids nested quotes in the exec args, which playbk's
# sh -c wrapping would mangle).
printf '#!/usr/bin/env bash\necho PlaybkClickTarget\nsleep 40\n' > "$work/show.sh"
chmod +x "$work/show.sh"

# Include an OCR-driven click-text step only when Tesseract is available.
clicktext_step=""
if [ "$have_ocr" = 1 ]; then
  clicktext_step="  - name: Click the on-screen word (OCR)
    action: click-text
    window: pbtest
    text: PlaybkClickTarget"
fi

cat > "$work/test.idleops.yaml" <<YAML
steps:
  - id: term
    name: Launch xterm
    action: exec
    args: xterm -fa Monospace -fs 24 -T pbtest -e $work/show.sh
    wait: false
  - name: Wait for window
    action: wait-window
    window: pbtest
    timeout: 10
  - name: Type text
    action: type
    window: pbtest
    text: hello-playbk
$clicktext_step
  - name: Screenshot the window
    action: screenshot
    window: pbtest
    output: $work/pb-shot.png
YAML

cd "$work"
dotnet "$repo_root/$PLAYBK" -i test.idleops.yaml -o "$work/out" 2>&1 | tail -20

if [ -s "$work/pb-shot.png" ]; then
  echo "== playbk Linux e2e: ALL PASS ($(identify -format '%wx%h' "$work/pb-shot.png")) =="
else
  echo "FAIL: playbk did not produce the screenshot"; exit 1
fi
