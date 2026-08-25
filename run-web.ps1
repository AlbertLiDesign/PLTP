<#
.SYNOPSIS
    Build and start the PLTP web app, then open it in a browser.

.EXAMPLE
    .\run-web.ps1
    .\run-web.ps1 -Port 8080 -NoBrowser
#>
[CmdletBinding()]
param(
    [int]$Port = 5080,
    [switch]$NoBrowser,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'PLTP.Web' 'PLTP.Web.csproj'
$url = "http://localhost:$Port"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is not on PATH. Install .NET 8 or newer from https://dotnet.microsoft.com/download'
}

$run = @('run', '--project', $project, '-c', 'Release', '--urls', $url)

if (-not $SkipBuild) {
    Write-Host 'Building...' -ForegroundColor DarkGray
    dotnet build $project -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $run += '--no-build'
}

if (-not $NoBrowser) {
    # The server takes over this shell, so the browser is opened from a job that
    # waits for the port to start answering.
    Start-Job -ArgumentList $url, $Port -ScriptBlock {
        param($u, $p)
        for ($i = 0; $i -lt 80; $i++) {
            Start-Sleep -Milliseconds 300
            try {
                $c = [Net.Sockets.TcpClient]::new('localhost', $p)
                $c.Close()
                Start-Process $u
                return
            } catch { }
        }
    } | Out-Null
}

Write-Host "PLTP on $url  (ctrl-c to stop)" -ForegroundColor Cyan
dotnet @run
