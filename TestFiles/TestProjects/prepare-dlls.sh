#!/bin/bash

# 把构建产物复制到包夹具的 dll/ 目录，模拟「第三方包自带 DLL」的布局。
# 这些 DLL 不入库（见根目录 .gitignore），由本脚本按需生成。

set -e

CONFIG="${1:-Debug}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

LIBS=("Old8LangLib" "Old8Lang.NetLib")

for lib in "${LIBS[@]}"; do
    src="$ROOT/$lib/bin/$CONFIG/net10.0/$lib.dll"
    if [ ! -f "$src" ]; then
        echo "未找到构建产物: $src" >&2
        echo "请先执行: dotnet build Old8Lang.sln -c $CONFIG" >&2
        exit 1
    fi
done

for fixture in PackageIntegrationTest ThirdPartyLibTest; do
    target="$ROOT/TestFiles/TestProjects/$fixture/packages/OldLib/dll"
    mkdir -p "$target"
    for lib in "${LIBS[@]}"; do
        cp "$ROOT/$lib/bin/$CONFIG/net10.0/$lib.dll" "$target/"
    done
    echo "已生成: $target"
done
