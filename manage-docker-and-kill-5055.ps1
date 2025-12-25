# Force-start/stop Docker Desktop and kill any process bound to port 5055.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Start-DockerDesktop {
    $dockerExe = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    if (-not (Test-Path $dockerExe)) {
        throw "Docker Desktop not found at: $dockerExe"
    }

    $alreadyRunning = Get-Process -Name "Docker Desktop" -ErrorAction SilentlyContinue
    if (-not $alreadyRunning) {
        Start-Process -FilePath $dockerExe | Out-Null
    }
}

function Stop-DockerDesktop {
    $procNames = @("Docker Desktop", "com.docker.backend", "com.docker.service")
    foreach ($name in $procNames) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

function Kill-Port5055 {
    $pids = Get-NetTCPConnection -LocalPort 5055 -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($pid in $pids) {
        if ($pid -gt 0) {
            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
        }
    }
}

# Example usage:
# Start-DockerDesktop
# Stop-DockerDesktop
# Kill-Port5055
