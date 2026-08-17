#!/usr/bin/env bash
#
# Linux XRecord end-to-end for stpcap: record synthesized input under a headless
# Xvfb and assert the generated playbook captured the click, typed text and Enter.
# Uses SIGTERM to stop stpcap (a bash
# background job has SIGINT set to SIG_IGN, so `kill -INT` wouldn't reach it — a
# real interactive Ctrl+C does).
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

STPCAP="$(find src/stpcap/bin -name stpcap.dll -path '*/net10.0/*' 2>/dev/null | head -1)"
if [ -z "$STPCAP" ]; then echo "SKIP: stpcap not built"; exit 0; fi

export DISPLAY=:99
work="$(mktemp -d)"; out="$work/rec.yaml"
# The stpcap pattern must be `stpcap.dll`, not `stpcap`: `pkill -f` matches whole
# command lines, and this script's own is `bash scripts/linux-stpcap-e2e.sh` — which
# contains "stpcap". The broader pattern makes the EXIT trap SIGTERM the script itself,
# so a fully passing run still exits 143 right after printing ALL PASS.
cleanup() { pkill -f stpcap.dll 2>/dev/null || true; pkill xterm 2>/dev/null || true; pkill -f "Xvfb :99" 2>/dev/null || true; pkill openbox 2>/dev/null || true; rm -rf "$work"; }
trap cleanup EXIT

Xvfb :99 -screen 0 1280x800x24 >"$work/xvfb.log" 2>&1 & sleep 2
openbox >"$work/ob.log" 2>&1 & sleep 1
xterm -T rectest -e "sleep 40" >"$work/xt.log" 2>&1 & sleep 2
xdotool search --name rectest windowactivate 2>/dev/null; sleep 1

dotnet "$STPCAP" --output "$out" >"$work/stpcap.log" 2>&1 &
STPID=$!
sleep 3
xdotool mousemove 400 300 click 1
xdotool type "Hello"
xdotool key Return
sleep 1
kill -TERM "$STPID" 2>/dev/null
for _ in 1 2 3 4 5; do kill -0 "$STPID" 2>/dev/null || break; sleep 1; done
kill -9 "$STPID" 2>/dev/null || true

if [ ! -s "$out" ]; then echo "FAIL: no playbook was written"; cat "$work/stpcap.log"; exit 1; fi
echo "== generated playbook =="; cat "$out"

grep -q -- '--type "Hello"' "$out" || { echo "FAIL: typed text not recorded"; exit 1; }
grep -q -- '--keyboard "ENTER"' "$out" || { echo "FAIL: Enter key not recorded"; exit 1; }
grep -q -- '--leftmouse' "$out" || { echo "FAIL: click not recorded"; exit 1; }
echo "== stpcap XRecord e2e: ALL PASS =="
