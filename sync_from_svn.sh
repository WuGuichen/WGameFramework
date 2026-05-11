#!/usr/bin/env bash
set -euo pipefail

mirror_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source_root="${WGAMEFRAMEWORK_SOURCE:-/Users/vincent/Documents/WGameFramework}"

if [[ ! -d "$source_root/Assets/Scripts/MxFramework" ]]; then
  echo "Source framework directory not found: $source_root/Assets/Scripts/MxFramework" >&2
  exit 1
fi

if [[ ! -d "$source_root/Docs" ]]; then
  echo "Source docs directory not found: $source_root/Docs" >&2
  exit 1
fi

cd "$mirror_root"

mkdir -p Assets/Scripts

rsync -a --delete \
  --exclude='.git/' \
  --exclude='.svn/' \
  --exclude='.gitnexus/' \
  --exclude='.claude/' \
  --exclude='.DS_Store' \
  "$source_root/Assets/Scripts/MxFramework" \
  "$mirror_root/Assets/Scripts/"

rsync -a --delete \
  --exclude='.DS_Store' \
  "$source_root/Docs/" \
  "$mirror_root/Docs/"

cp "$source_root/AGENTS.md" "$mirror_root/AGENTS.md"
cp "$source_root/Assets/Scripts/MxFramework.meta" "$mirror_root/Assets/Scripts/MxFramework.meta"

git add -A

if git diff --cached --quiet; then
  echo "Mirror already up to date."
  exit 0
fi

message="${1:-Sync framework source and docs}"
git commit -m "$message"

if [[ "${PUSH:-0}" == "1" ]]; then
  git push -u origin main
fi
