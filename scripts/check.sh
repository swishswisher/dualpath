#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${DUALPATH_TEST_DATABASE:?Set DUALPATH_TEST_DATABASE to a dedicated PostgreSQL database (never production)}"
if find .github/workflows -type f \( -name '*.yml' -o -name '*.yaml' \) 2>/dev/null | read -r _; then
  echo 'GitHub Actions workflows are prohibited by this project.' >&2
  exit 1
fi
dotnet restore DualPath.sln --locked-mode
dotnet build DualPath.sln -c Release --no-restore
dotnet test tests/DualPath.Tests/DualPath.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=platform.trx' --results-directory artifacts/test-results
dotnet publish src/DualPath.Web/DualPath.Web.csproj -c Release --no-restore -o artifacts/publish /p:UseAppHost=false
