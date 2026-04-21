#!/usr/bin/env pwsh
# Wrapper script to run AccessManagement coverage with Podman configuration

$env:DOCKER_HOST = ''
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'

$projects = @(
    'src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj',
    'src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Altinn.AccessMgmt.Core.Tests.csproj',
    'src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.PersistenceEF.Tests/Altinn.AccessMgmt.PersistenceEF.Tests.csproj',
    'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/Altinn.AccessManagement.Api.Tests.csproj',
    'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Altinn.AccessManagement.Enduser.Api.Tests.csproj',
    'src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/Altinn.AccessManagement.ServiceOwner.Api.Tests.csproj'
)

Write-Host "Running AccessManagement tests with Podman Desktop..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'run-coverage.ps1') -Projects $projects @args
