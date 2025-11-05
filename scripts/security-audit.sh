#!/usr/bin/env bash
set -euo pipefail

echo "Running .NET dependency vulnerability audit..."

shopt -s nullglob
projects=(src/**/*.csproj tests/**/*.csproj)

if [ ${#projects[@]} -eq 0 ]; then
  echo "No projects found."
  exit 0
fi

for p in "${projects[@]}"; do
  echo "\n=== Auditing: $p ==="
  dotnet list "$p" package --vulnerable || true
done

echo "\nAudit complete. Review any findings above."

