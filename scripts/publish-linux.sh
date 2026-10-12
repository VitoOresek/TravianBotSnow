#!/usr/bin/env bash
# Builds dist/TravianBotSharp-linux-x64.tar.gz (self contained, no .NET install needed on the target).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/dist/TravianBotSharp-linux-x64"
DOTNET="${DOTNET:-dotnet}"

rm -rf "$OUT"
"$DOTNET" publish "$ROOT/AvaloniaUI/AvaloniaUI.csproj" -c Release -r linux-x64 --self-contained true -o "$OUT" "$@"
cp "$ROOT/scripts/install.sh" "$ROOT/scripts/run.sh" "$ROOT/README-LINUX.md" "$OUT/"
cp "$ROOT/AvaloniaUI/Assets/roman_big.png" "$OUT/icon.png"

python3 - "$OUT" "$ROOT/dist/TravianBotSharp-linux-x64.tar.gz" <<'EOF'
import os, sys, tarfile
src, dst = sys.argv[1], sys.argv[2]
executables = {"TravianBotSharp", "install.sh", "run.sh", "createdump"}
def fix(info):
    name = os.path.basename(info.name)
    info.mode = 0o755 if info.isdir() or name in executables or name.endswith(".sh") else 0o644
    info.uid = info.gid = 0
    info.uname = info.gname = ""
    return info
with tarfile.open(dst, "w:gz") as tar:
    tar.add(src, arcname="TravianBotSharp", filter=fix)
print(dst)
EOF
