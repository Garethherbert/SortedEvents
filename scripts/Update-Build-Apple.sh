#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
platform="${1:-mac}"; brand="${2:-BigDaySorted}"; branch="${3:-main}"
[[ -z "$(git status --porcelain)" ]] || { echo 'Commit or resolve source changes before updating.' >&2; exit 1; }
git fetch origin
git switch "$branch"
git pull --ff-only origin "$branch"
[[ -f cloudflare/deploy.config.json ]] || { echo 'Copy your non-secret deployment configuration onto this Mac first.' >&2; exit 1; }
bash scripts/Build-Apple.sh "$platform" "$brand"
