#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

OUT="libinterop.dylib"
clang++ -std=c++17 -O2 -shared -fPIC -o "$OUT" interop.cpp

echo "Built native/$OUT"
