#!/usr/bin/env python3
"""
X11 input-recording helper for stpcap's Linux backend. Uses the XRecord extension
(python-xlib) to capture key/button presses globally and emits one JSON line per
event to stdout, which the .NET LinuxInputRecorder consumes live. Same external
helper model as the AT-SPI backend.

Each line: {"t":"key"|"button","detail":N,"x":rootX,"y":rootY,"sym":"a","win":"Title"}
Runs until stdin closes or SIGINT.
"""
import sys, json, threading

try:
    from Xlib import display, X, XK
    from Xlib.ext import record
    from Xlib.protocol import rq
except Exception as e:
    sys.stderr.write(f"stprec: python3-xlib not available ({e})\n")
    sys.exit(2)

local_d = display.Display()
record_d = display.Display()

if not record_d.has_extension("RECORD"):
    sys.stderr.write("stprec: X server has no RECORD extension\n")
    sys.exit(2)

# Detect modifiers by keysym VALUE (keysym_to_string returns None for them).
# getattr guards keysyms that aren't in python-xlib's default groups.
def _ks(*names):
    return {getattr(XK, n) for n in names if hasattr(XK, n)}

MOD_SHIFT = _ks("XK_Shift_L", "XK_Shift_R")
MOD_SKIP = MOD_SHIFT | _ks(
    "XK_Control_L", "XK_Control_R", "XK_Alt_L", "XK_Alt_R",
    "XK_Super_L", "XK_Super_R", "XK_Meta_L", "XK_Meta_R",
    "XK_Caps_Lock", "XK_ISO_Level3_Shift", "XK_Num_Lock",
)
shift_down = False


def _title_of(w):
    for atom_name in ("_NET_WM_NAME", "WM_NAME"):
        try:
            p = w.get_full_property(local_d.intern_atom(atom_name), X.AnyPropertyType)
            if p and p.value:
                v = p.value
                return v.decode("utf-8", "ignore") if isinstance(v, (bytes, bytearray)) else str(v)
        except Exception:
            continue
    return ""


def active_window_title():
    try:
        root = local_d.screen().root
        prop = root.get_full_property(local_d.intern_atom("_NET_ACTIVE_WINDOW"), X.AnyPropertyType)
        if prop and prop.value:
            return _title_of(local_d.create_resource_object("window", prop.value[0]))
    except Exception:
        pass
    try:  # fall back to the focus window, walking up to the titled top-level
        w = local_d.get_input_focus().focus
        for _ in range(8):
            if w is None or not hasattr(w, "get_full_property"):
                break
            t = _title_of(w)
            if t:
                return t
            w = w.query_tree().parent
    except Exception:
        pass
    return ""


# Reverse map keysym value -> name (e.g. 0xff0d -> "Return") for special keys.
REVERSE = {}
for _n in dir(XK):
    if _n.startswith("XK_"):
        REVERSE.setdefault(getattr(XK, _n), _n[3:])


def base_keysym(keycode):
    return local_d.keycode_to_keysym(keycode, 0)


def classify(keycode, shifted):
    """Return (kind, sym): ("char", printable) or ("special", KeysymName)."""
    ks = local_d.keycode_to_keysym(keycode, 1 if shifted else 0) or local_d.keycode_to_keysym(keycode, 0)
    ch = XK.keysym_to_string(ks)
    if ch and ch.isprintable():
        return "char", ch
    return "special", REVERSE.get(base_keysym(keycode), "")


def emit(t, detail, x, y, sym, kind=""):
    line = json.dumps({"t": t, "detail": detail, "x": x, "y": y, "sym": sym, "kind": kind, "win": active_window_title()})
    sys.stdout.write(line + "\n")
    sys.stdout.flush()


# One context; the device_events range (KeyPress=2 .. ButtonPress=4) also delivers
# KeyRelease=3, so a single handler tracks shift and emits key/button presses.
ctx = record_d.record_create_context(0, [record.AllClients], [{
    "core_requests": (0, 0), "core_replies": (0, 0),
    "ext_requests": (0, 0, 0, 0), "ext_replies": (0, 0, 0, 0),
    "delivered_events": (0, 0), "device_events": (X.KeyPress, X.ButtonPress),
    "errors": (0, 0), "client_started": False, "client_died": False}])


def handler(reply):
    global shift_down
    if reply.category != record.FromServer or not reply.data:
        return
    data = reply.data
    while len(data):
        ev, data = rq.EventField(None).parse_binary_value(data, record_d.display, None, None)
        if ev.type == X.KeyPress:
            ks = base_keysym(ev.detail)
            if ks in MOD_SHIFT:
                shift_down = True
                continue
            if ks in MOD_SKIP:
                continue
            kind, sym = classify(ev.detail, shift_down)
            if sym:
                emit("key", ev.detail, ev.root_x, ev.root_y, sym, kind)
        elif ev.type == X.KeyRelease:
            if base_keysym(ev.detail) in MOD_SHIFT:
                shift_down = False
        elif ev.type == X.ButtonPress:
            emit("button", ev.detail, ev.root_x, ev.root_y, "")


# Stop cleanly when stdin closes (parent .NET process stops recording).
def watch_stdin():
    try:
        for _ in sys.stdin:
            pass
    except Exception:
        pass
    try:
        local_d.record_disable_context(ctx)
        local_d.flush()
    except Exception:
        pass

threading.Thread(target=watch_stdin, daemon=True).start()
record_d.record_enable_context(ctx, handler)
record_d.record_free_context(ctx)
