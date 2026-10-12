#!/usr/bin/env bash
# One-time setup for TravianBotSharp on Ubuntu / Debian (x86_64).
# Run it from the extracted TravianBotSharp folder:   ./install.sh
#   --no-msfonts   skip the Microsoft core fonts (Arial, Verdana, ...). They make the
#                  font fingerprint look like a Windows PC, which matches the default identity.
set -euo pipefail

APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_MSFONTS=1
for arg in "$@"; do
    case "$arg" in
        --no-msfonts) INSTALL_MSFONTS=0 ;;
        *) echo "unknown option: $arg"; exit 1 ;;
    esac
done

case "$(uname -m)" in
    x86_64) MACHINE_ARCH="x64" ;;
    aarch64|arm64) MACHINE_ARCH="arm64" ;;
    *) echo "Google ships Chrome for x86_64 and arm64 Linux only, this machine is $(uname -m)."; exit 1 ;;
esac
# ELF e_machine at byte 18: 3e = x86-64, b7 = aarch64
case "$(od -An -tx1 -j18 -N1 "$APP_DIR/TravianBotSharp" | tr -d ' ')" in
    3e) BUILD_ARCH="x64" ;;
    b7) BUILD_ARCH="arm64" ;;
    *) BUILD_ARCH="unknown" ;;
esac
if [ "$BUILD_ARCH" != "$MACHINE_ARCH" ]; then
    echo "This is the $BUILD_ARCH build but this machine is $(uname -m)."
    echo "Download TravianBotSharp-linux-$MACHINE_ARCH.tar.gz instead."
    exit 1
fi

if ! command -v apt-get >/dev/null; then
    echo "This script supports apt based distributions (Ubuntu, Debian, Mint)."
    echo "On other distributions install: Google Chrome, libicu, libX11, libICE, libSM, fontconfig."
    exit 1
fi

SUDO=""
if [ "$(id -u)" -ne 0 ]; then SUDO="sudo"; fi

echo "==> Installing libraries for the bot window"
$SUDO apt-get update
ICU_PACKAGE="$(apt-cache pkgnames libicu | grep -E '^libicu[0-9]+$' | sort -V | tail -n 1)"
$SUDO apt-get install -y wget ca-certificates fontconfig fonts-liberation \
    libx11-6 libice6 libsm6 libfontconfig1 libxrandr2 libxcursor1 libxi6 "${ICU_PACKAGE:-libicu-dev}"

if ! command -v google-chrome-stable >/dev/null && ! command -v google-chrome >/dev/null; then
    echo "==> Installing Google Chrome (also adds Google's repository so apt keeps it updated)"
    TMP_DEB="$(mktemp --suffix=.deb)"
    wget -q -O "$TMP_DEB" "https://dl.google.com/linux/direct/google-chrome-stable_current_$(dpkg --print-architecture).deb"
    $SUDO apt-get install -y "$TMP_DEB"
    rm -f "$TMP_DEB"
else
    echo "==> Google Chrome already installed: $(google-chrome --version 2>/dev/null || google-chrome-stable --version)"
fi

if [ "$INSTALL_MSFONTS" -eq 1 ]; then
    echo "==> Installing Microsoft core fonts"
    if grep -qi ubuntu /etc/os-release; then
        $SUDO apt-get install -y software-properties-common
        $SUDO add-apt-repository -y multiverse
        $SUDO apt-get update
    fi
    echo "ttf-mscorefonts-installer msttcorefonts/accept-mscorefonts-eula select true" | $SUDO debconf-set-selections
    $SUDO apt-get install -y ttf-mscorefonts-installer || echo "!! Microsoft fonts could not be installed, continuing without them"
    fc-cache -f >/dev/null || true
fi

echo "==> Creating menu and desktop shortcuts"
chmod +x "$APP_DIR/TravianBotSharp" "$APP_DIR/run.sh"
APPLICATIONS_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
mkdir -p "$APPLICATIONS_DIR"
DESKTOP_FILE="$APPLICATIONS_DIR/travianbotsharp.desktop"
cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Type=Application
Name=TravianBotSharp
Comment=Travian bot
Exec="$APP_DIR/run.sh"
Path=$APP_DIR
Icon=$APP_DIR/icon.png
Terminal=false
Categories=Game;
EOF
chmod +x "$DESKTOP_FILE"

DESKTOP_DIR="$(xdg-user-dir DESKTOP 2>/dev/null || echo "$HOME/Desktop")"
if [ -d "$DESKTOP_DIR" ]; then
    cp "$DESKTOP_FILE" "$DESKTOP_DIR/"
    chmod +x "$DESKTOP_DIR/travianbotsharp.desktop"
    # GNOME asks to "Allow Launching" otherwise
    gio set "$DESKTOP_DIR/travianbotsharp.desktop" metadata::trusted true 2>/dev/null || true
fi

if [ "$(id -u)" -eq 0 ]; then
    echo "!! You are root. It works (Chrome runs with --no-sandbox) but a normal user account is safer."
fi

echo
echo "Done. Start the bot from the app menu / desktop icon, or with:  $APP_DIR/run.sh"
