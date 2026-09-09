# EPOCH V1 - Windows entry point. Mirrors run-tests.sh exactly.
$ErrorActionPreference = "Continue"
Set-Location $PSScriptRoot

$suites = @(
  "Epoch.Architecture.Tests",
  "Epoch.Core.Tests",
  "Epoch.Content.Tests",
  "Epoch.Oracle.Tests"
)

Write-Host "Building solution..."
dotnet build EPOCH.sln -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED"; exit 1 }

$failed = 0
foreach ($suite in $suites) {
  Write-Host ""
  dotnet run --project "tests/$suite" -c Release --no-build --nologo
  if ($LASTEXITCODE -ne 0) { $failed = 1 }
}

Write-Host ""
if ($failed -ne 0) { Write-Host "RESULT: FAILED"; exit 1 }
Write-Host "RESULT: all suites passed"
