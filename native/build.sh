#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

OUT="libinterop.dylib"
build() { clang++ -std=c++17 -O2 -shared -fPIC -o "$OUT" interop.cpp; }

# An explicit SDKROOT from the user always wins.
if [[ -n "${SDKROOT:-}" ]]; then
    build
    echo "Built native/$OUT"
    exit 0
fi

# Normal path: let clang pick the SDK.
log="$(mktemp)"
trap 'rm -f "$log"' EXIT
if build 2>"$log"; then
    echo "Built native/$OUT"
    exit 0
fi

# The default SDK failed. This usually means the installed SDK is newer than the
# installed linker (a partial Command Line Tools update). Retry with the other
# installed macOS SDKs, newest first, rather than pinning a specific version.
tail -n 3 "$log" >&2
default_sdk="$(xcrun --sdk macosx --show-sdk-path 2>/dev/null || true)"
sdk_dir="$(dirname "$default_sdk")"

for sdk in $(find "$sdk_dir" -maxdepth 1 -type d -name 'MacOSX*.sdk' | sort -rV); do
    [[ "$sdk" == "$default_sdk" ]] && continue
    echo "warning: default SDK failed, retrying with $(basename "$sdk")" >&2
    if SDKROOT="$sdk" build; then
        echo "Built native/$OUT (with $(basename "$sdk"))"
        echo "hint: your Command Line Tools look out of sync with $(basename "$default_sdk");" \
             "run 'softwareupdate --list' and install the Command Line Tools update." >&2
        exit 0
    fi
done

echo "error: could not build native/$OUT with any installed macOS SDK." >&2
exit 1
