#!/usr/bin/env python3
"""Package a freshly built Borg Asyut player. Python 3.11+, standard library only."""
import argparse
import hashlib
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
NOTICES = {
    "CREDITS.md": "CREDITS.md",
    "Lemonada-OFL.txt": "Assets/Ma3alem/Fonts/Lemonada-OFL.txt",
    "Noto-OFL.txt": "Assets/Ma3alem/Fonts/OFL.txt",
    "RTLTMPro-LICENSE.txt": "Assets/ThirdParty/RTLTMPro/LICENSE.txt",
    "TMPShaders-LICENSE.txt": "Assets/ThirdParty/TMPShaders/LICENSE.txt",
}

def package(platform, source, output, root=ROOT):
    if platform not in {"windows", "web"}:
        raise ValueError("Unsupported platform: " + platform)
    source, output = source.resolve(), output.resolve()
    if not source.is_dir() or source == output or source in output.parents:
        raise ValueError("Use an existing build folder and an output ZIP outside it.")
    if platform == "windows":
        exe = source / "Borg Asyut.exe"
        if not exe.is_file() or not (source / (exe.stem + "_Data")).is_dir() or not (source / "UnityPlayer.dll").is_file():
            raise ValueError("Incomplete Windows player: exe, matching Data directory and UnityPlayer.dll required.")
    else:
        build = source / "Build"
        if not (source / "index.html").is_file() or not build.is_dir():
            raise ValueError("Web player must have root index.html and Build directory.")
        names = [p.name for p in build.iterdir() if p.is_file()]
        for fragment in (".loader.js", ".framework.js", ".wasm", ".data"):
            if not any(fragment in name for name in names):
                raise ValueError("Missing Web component: " + fragment)
    for path in NOTICES.values():
        if not (root / path).is_file():
            raise ValueError("Missing notice: " + path)
    items = []
    for p in sorted(source.rglob("*")):
        rel = p.relative_to(source)
        if p.is_symlink():
            raise ValueError("Unexpected symlink: " + str(rel))
        if not p.is_file():
            continue
        parts = [x.lower() for x in rel.parts]
        if any("donotship" in x or "dontshipit" in x for x in parts) or p.suffix.lower() in {".pdb", ".log", ".mdb", ".dsym"}:
            continue
        if any("localpreviewonly" in x or "el_melouk" in x or x == "verification" for x in parts):
            raise ValueError("Unexpected local-only/verification artifact: " + str(rel))
        if parts[0] == "third_party_notices":
            raise ValueError("Use a clean build directory.")
        items.append((p, rel.as_posix()))
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for p, rel in items:
            z.write(p, rel)
        for name, path in NOTICES.items():
            z.write(root / path, "Third_Party_Notices/" + name)
    with zipfile.ZipFile(output) as z:
        if z.testzip() is not None:
            raise ValueError("ZIP integrity check failed.")
    with output.open("rb") as f:
        digest = hashlib.file_digest(f, "sha256").hexdigest()
    output.with_suffix(output.suffix + ".sha256").write_text(f"{digest}  {output.name}\n", encoding="ascii")
    print(f"{output}: {output.stat().st_size:,} bytes | SHA-256 {digest}")
    return digest

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=["windows", "web"])
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    try:
        package(args.platform, args.source, args.output)
    except (OSError, ValueError) as e:
        parser.exit(1, f"Packaging failed: {e}\n")

if __name__ == "__main__":
    main()
