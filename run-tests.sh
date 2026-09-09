#!/usr/bin/env bash
# EPOCH V1 - the single command that runs every automated gate.
# No third-party test runner: each suite is a console app that exits non-zero on failure.
set -uo pipefail
cd "$(dirname "$0")"

SUITES=(
  Epoch.Architecture.Tests
  Epoch.Core.Tests
  Epoch.Content.Tests
  Epoch.Oracle.Tests
)

echo "Building solution..."
if ! dotnet build EPOCH.sln -c Release --nologo -v quiet; then
  echo "BUILD FAILED"
  exit 1
fi

failed=0
for suite in "${SUITES[@]}"; do
  echo
  if ! dotnet run --project "tests/$suite" -c Release --no-build --nologo; then
    failed=1
  fi
done

echo
if [ "$failed" -ne 0 ]; then
  echo "RESULT: FAILED"
  exit 1
fi
echo "RESULT: all suites passed"
