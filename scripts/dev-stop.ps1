[CmdletBinding()]
param(
    [string]$PidFile,
    [switch]$PidOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$runtimeDir = Join-Path $repoRoot ".dev-runtime"

if ([string]::IsNullOrWhiteSpace($PidFile)) {
    $PidFile = Join-Path $runtimeDir "dev-local-pids.json"
}

function Get-ChildProcessIds {
    param([int]$ParentProcessId)

    Get-CimInstance Win32_Process -Filter "ParentProcessId = $ParentProcessId" |
        ForEach-Object {
            [int]$_.ProcessId
            Get-ChildProcessIds -ParentProcessId ([int]$_.ProcessId)
        }
}

function Stop-ProcessTree {
    param(
        [int]$ProcessId,
        [string]$Name
    )

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return
    }

    $childIds = @(Get-ChildProcessIds -ParentProcessId $ProcessId)
    [array]::Reverse($childIds)

    foreach ($childId in $childIds) {
        $child = Get-Process -Id $childId -ErrorAction SilentlyContinue
        if ($null -ne $child) {
            Stop-Process -Id $childId -Force -ErrorAction SilentlyContinue
        }
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    Write-Host "Stopped $Name (PID $ProcessId)."
}

function Get-RepoDebugProcesses {
    Get-CimInstance Win32_Process |
        Where-Object {
            $commandLine = [string]$_.CommandLine
            $executablePath = [string]$_.ExecutablePath
            $inRepo = $commandLine.Contains($repoRoot) -or $executablePath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)
            $looksLikeDebugProcess = $commandLine -match "TanoDevClip\.UI|vite|TanoDevClip\.App|TanoDevClip\.App\.csproj" -or
                $executablePath -match "TanoDevClip\.App"

            $_.ProcessId -ne $PID -and $inRepo -and $looksLikeDebugProcess
        }
}

$stoppedAny = $false

if (Test-Path $PidFile) {
    $state = Get-Content $PidFile -Raw | ConvertFrom-Json

    foreach ($trackedProcess in $state.processes) {
        Stop-ProcessTree -ProcessId ([int]$trackedProcess.pid) -Name ([string]$trackedProcess.name)
        $stoppedAny = $true
    }

    Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
}
elseif ($PidOnly) {
    Write-Host "No local debug PID file found at $PidFile."
    return
}

if (-not $PidOnly) {
    foreach ($repoProcess in Get-RepoDebugProcesses) {
        Stop-ProcessTree -ProcessId ([int]$repoProcess.ProcessId) -Name ([string]$repoProcess.Name)
        $stoppedAny = $true
    }
}

if ($stoppedAny) {
    Write-Host "Local debug processes stopped."
}
else {
    Write-Host "No local debug processes found."
}
