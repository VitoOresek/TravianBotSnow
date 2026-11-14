#!/usr/bin/env bash
# Updates TravianBotSharp to the latest release on GitHub.
# Usage: ./update.sh [path/to/TravianBotSharp]   default: the folder this script is in
#        ./update.sh --shortcut-only             only (re)create the "Update TravianBotSharp" shortcut
# Keeps TBS.db, logs, Chrome profiles and screenshots; only the program files are replaced.
# TBS.db is backed up to backups/ first.
#
# Everything runs inside main(), which bash reads completely before running it,
# because the update overwrites this very file.
set -euo pipefail

REPO="VitoOresek/TravianBotSnow"

pause() {
    if [ -t 0 ]; then read -r -p "Press Enter to close this window..." _ || true; fi
}

fail() {
    echo
    echo "!! $*"
    pause
    exit 1
}

create_shortcut() {
    local app_dir="$1"
    [ -f "$app_dir/update.sh" ] || return 0
    chmod +x "$app_dir/update.sh"
    local applications_dir="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
    local desktop_file="$applications_dir/travianbotsharp-update.desktop"
    mkdir -p "$applications_dir"
    cat > "$desktop_file" <<EOF
[Desktop Entry]
Type=Application
Name=Update TravianBotSharp
Comment=Download the latest TravianBotSharp release from GitHub
Exec="$app_dir/update.sh"
Path=$app_dir
Icon=system-software-update
Terminal=true
Categories=Game;
EOF
    chmod +x "$desktop_file"
    local desktop_dir
    desktop_dir="$(xdg-user-dir DESKTOP 2>/dev/null || echo "$HOME/Desktop")"
    if [ -d "$desktop_dir" ]; then
        cp "$desktop_file" "$desktop_dir/"
        chmod +x "$desktop_dir/travianbotsharp-update.desktop"
        # GNOME / XFCE ask to "Allow Launching" otherwise
        gio set "$desktop_dir/travianbotsharp-update.desktop" metadata::trusted true 2>/dev/null || true
    fi
}

main() {
    local shortcut_only=0 app_dir="" arg
    for arg in "$@"; do
        case "$arg" in
            --shortcut-only) shortcut_only=1 ;;
            *) app_dir="$arg" ;;
        esac
    done
    if [ -z "$app_dir" ]; then app_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; fi
    [ -f "$app_dir/TravianBotSharp" ] || fail "No TravianBotSharp found in '$app_dir'. Usage: $0 /path/to/TravianBotSharp"
    app_dir="$(cd "$app_dir" && pwd)"

    if [ "$shortcut_only" -eq 1 ]; then
        create_shortcut "$app_dir"
        return 0
    fi

    local arch
    case "$(uname -m)" in
        x86_64) arch="x64" ;;
        aarch64|arm64) arch="arm64" ;;
        *) fail "Unsupported machine: $(uname -m)" ;;
    esac

    # The bot must be closed, otherwise its files are in use and TBS.db may be mid-write.
    local exe
    for exe in /proc/[0-9]*/exe; do
        if [ "$(readlink "$exe" 2>/dev/null)" = "$app_dir/TravianBotSharp" ]; then
            fail "TravianBotSharp is still running. Close the bot window, then run the update again."
        fi
    done

    echo "==> Checking the latest release of $REPO"
    local release tag
    release="$(wget -q -O- "https://api.github.com/repos/$REPO/releases/latest")" \
        || fail "Could not reach GitHub to find the latest release."
    tag="$(printf '%s\n' "$release" | sed -nE 's/.*"tag_name": *"([^"]+)".*/\1/p' | sed -n 1p)"
    [ -n "$tag" ] || fail "GitHub did not return a release for $REPO."

    local current
    current="$(cat "$app_dir/VERSION" 2>/dev/null || echo unknown)"
    echo "    installed: $current"
    echo "    latest:    $tag"
    if [ "$current" = "$tag" ]; then
        echo "Already up to date."
        create_shortcut "$app_dir"
        pause
        return 0
    fi

    local archive="TravianBotSharp-linux-$arch.tar.gz"
    # global, so the EXIT trap still sees it after main() returns
    UPDATE_TMP="$(mktemp -d)"
    trap 'rm -rf -- "$UPDATE_TMP"' EXIT
    local tmp="$UPDATE_TMP"

    echo "==> Downloading $archive"
    wget -q --show-progress -O "$tmp/$archive" "https://github.com/$REPO/releases/download/$tag/$archive" \
        || fail "Download failed."
    # list to a file first: `tar | grep -q` would fail on SIGPIPE under pipefail
    tar tzf "$tmp/$archive" > "$tmp/contents" || fail "The download is not a valid archive."
    grep -qx 'TravianBotSharp/TravianBotSharp' "$tmp/contents" \
        || fail "The download is not a TravianBotSharp build."
    tar xzf "$tmp/$archive" -C "$tmp"

    if [ -f "$app_dir/TBS.db" ]; then
        echo "==> Backing up TBS.db"
        local backup_dir="$app_dir/backups" stamp f
        stamp="$(date +%Y%m%d-%H%M%S)"
        mkdir -p "$backup_dir"
        for f in TBS.db TBS.db-wal TBS.db-shm; do
            if [ -f "$app_dir/$f" ]; then cp -p "$app_dir/$f" "$backup_dir/$f.$stamp"; fi
        done
        echo "    $backup_dir/TBS.db.$stamp"
        # keep the 10 newest backups
        { ls -1t "$backup_dir"/TBS.db.[0-9]* 2>/dev/null | tail -n +11 | xargs -r rm -f; } || true
    fi

    echo "==> Installing $tag"
    cp -a "$tmp/TravianBotSharp/." "$app_dir/"
    echo "$tag" > "$app_dir/VERSION"
    create_shortcut "$app_dir"

    echo
    echo "Updated to $tag. Start the bot again from its desktop icon or with $app_dir/run.sh"
    pause
}

main "$@"; exit $?
