#!/usr/bin/env bash
# Starts TravianBotSharp. Data (TBS.db, logs, chrome profiles, screenshots) is kept in this folder.
APP_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$APP_DIR" || exit 1

if [ -z "${DISPLAY:-}" ] && [ -z "${WAYLAND_DISPLAY:-}" ]; then
    echo "No graphical session found (DISPLAY is empty)."
    echo "Start the bot from the VPS desktop (VNC / RDP session), not from a plain SSH shell."
    exit 1
fi

exec "$APP_DIR/TravianBotSharp" "$@"
