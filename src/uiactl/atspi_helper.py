#!/usr/bin/env python3
"""
AT-SPI2 helper for uiactl's Linux backend. Locates accessible elements by name
and/or role within a window and drives them via AT-SPI interfaces (Action, Value,
EditableText, Selection). The .NET LinuxUiAutomation shells out to this — the same
"external tool" model IdleOps uses for ffmpeg/xdotool/tesseract.

Usage:
  atspi_helper.py <verb> --window "<pattern>" [--name N] [--role R] [--value V] [--max M] [--point x,y]

Verbs: dump | invoke | get-value | set-value | toggle | select | expand | collapse | element-at

Output: get-value/dump/element-at print to stdout; actions report on stderr.
Exit 0 = success, 1 = element/window not found or action failed, 2 = AT-SPI unavailable.
"""
import sys, argparse

try:
    import pyatspi
except Exception as e:
    sys.stderr.write(f"atspi: pyatspi not available ({e}); install python3-pyatspi\n")
    sys.exit(2)


def matches(pattern, text):
    """Wildcard-ish contains match, case-insensitive (mirrors WindowMatcher)."""
    if not pattern:
        return True
    p = pattern.strip("*").lower()
    return p in (text or "").lower()


def find_app(window):
    try:
        desktop = pyatspi.Registry.getDesktop(0)
    except Exception as e:
        sys.stderr.write(f"atspi: no accessibility bus ({e})\n")
        sys.exit(2)
    for app in desktop:
        if app is None:
            continue
        if matches(window, app.name):
            return app
        # also match a top-level frame's title under the app
        for i in range(app.childCount):
            frame = app.getChildAtIndex(i)
            if frame is not None and matches(window, frame.name):
                return app
    return None


def walk(node, depth, maxdepth, out, limit):
    if node is None or depth > maxdepth or len(out) >= limit:
        return
    try:
        out.append((depth, node.getRoleName(), node.name or ""))
    except Exception:
        return
    for i in range(node.childCount):
        if len(out) >= limit:
            return
        walk(node.getChildAtIndex(i), depth + 1, maxdepth, out, limit)


def find_element(root, name, role, depth=0):
    if root is None or depth > 25:
        return None
    try:
        ok_name = (name is None) or matches(name, root.name)
        ok_role = (role is None) or (role.lower() in root.getRoleName().lower())
        if (name is not None or role is not None) and ok_name and ok_role:
            return root
    except Exception:
        pass
    for i in range(root.childCount):
        found = find_element(root.getChildAtIndex(i), name, role, depth + 1)
        if found is not None:
            return found
    return None


def do_action(el, *preferred):
    """Invoke a named action (or the first) via the Action interface."""
    try:
        action = el.queryAction()
    except Exception:
        return False
    names = [action.getName(i).lower() for i in range(action.nActions)]
    for want in preferred:
        if want in names:
            action.doAction(names.index(want))
            return True
    if action.nActions > 0:
        action.doAction(0)
        return True
    return False


def get_value(el):
    # Prefer the Value interface; fall back to the accessible text/name.
    try:
        return str(el.queryValue().currentValue)
    except Exception:
        pass
    try:
        txt = el.queryText()
        return txt.getText(0, txt.characterCount)
    except Exception:
        pass
    return el.name or ""


def set_value(el, value):
    try:
        et = el.queryEditableText()
        et.setTextContents(value)
        return True
    except Exception:
        pass
    try:
        el.queryValue().currentValue = float(value)
        return True
    except Exception:
        return False


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("verb")
    ap.add_argument("--window", default="")
    ap.add_argument("--name")
    ap.add_argument("--role")
    ap.add_argument("--value")
    ap.add_argument("--max", type=int, default=200)
    ap.add_argument("--point")
    args = ap.parse_args()

    if args.verb == "element-at" and args.point:
        x, y = (int(v) for v in args.point.split(","))
        try:
            desktop = pyatspi.Registry.getDesktop(0)
        except Exception as e:
            sys.stderr.write(f"atspi: no accessibility bus ({e})\n"); sys.exit(2)
        for app in desktop:
            if app is None:
                continue
            try:
                comp = app.queryComponent()
                hit = comp.getAccessibleAtPoint(x, y, pyatspi.DESKTOP_COORDS)
                if hit is not None:
                    print(f"[{hit.getRoleName()}] name=\"{hit.name}\"")
                    sys.exit(0)
            except Exception:
                continue
        sys.stderr.write("atspi: no element at that point\n"); sys.exit(1)

    app = find_app(args.window)
    if app is None:
        sys.stderr.write(f"atspi: window '{args.window}' not found\n"); sys.exit(1)

    if args.verb == "dump":
        out = []
        walk(app, 0, 25, out, args.max)
        for depth, role, name in out:
            print(f"{'  '*depth}[{role}] name=\"{name}\"")
        sys.exit(0)

    el = find_element(app, args.name, args.role)
    if el is None:
        sys.stderr.write(f"atspi: element (name={args.name!r} role={args.role!r}) not found\n"); sys.exit(1)

    if args.verb == "get-value":
        print(get_value(el)); sys.exit(0)
    if args.verb == "set-value":
        sys.exit(0 if set_value(el, args.value or "") else 1)
    if args.verb == "invoke":
        sys.exit(0 if do_action(el, "click", "press", "activate") else 1)
    if args.verb == "toggle":
        sys.exit(0 if do_action(el, "toggle", "click", "press") else 1)
    if args.verb == "select":
        sys.exit(0 if do_action(el, "select", "click") else 1)
    if args.verb == "expand":
        sys.exit(0 if do_action(el, "expand", "expand or contract", "click") else 1)
    if args.verb == "collapse":
        sys.exit(0 if do_action(el, "collapse", "expand or contract", "click") else 1)

    sys.stderr.write(f"atspi: unknown verb '{args.verb}'\n"); sys.exit(1)


if __name__ == "__main__":
    main()
