[CmdletBinding()]
param(
    [switch]$SkipInstall,
    [switch]$NoHotReload,
    [switch]$NoWait,
    [int]$VitePort = 5173
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$uiRoot = Join-Path $repoRoot "src\TanoDevClip.UI"
$appProject = Join-Path $repoRoot "src\TanoDevClip.App\TanoDevClip.App.csproj"
$runtimeDir = Join-Path $repoRoot ".dev-runtime"
$pidFile = Join-Path $runtimeDir "dev-local-pids.json"
$stopScript = Join-Path $PSScriptRoot "dev-stop.ps1"
$viteUrl = "http://localhost:$VitePort"
$skipCleanup = $false

function Assert-CommandExists {
    param([string]$CommandName)

    if ($null -eq (Get-Command $CommandName -ErrorAction SilentlyContinue)) {
        throw "Required command '$CommandName' was not found in PATH."
    }
}

function Wait-ForVite {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "Vite did not respond at $Url within $TimeoutSeconds seconds."
}

function Test-TcpPortInUse {
    param([int]$Port)

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $asyncResult = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
        if (-not $asyncResult.AsyncWaitHandle.WaitOne(200)) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

New-Item -ItemType Directory -Force -Path $runtimeDir | Out-Null

Assert-CommandExists -CommandName "dotnet"
Assert-CommandExists -CommandName "npm.cmd"

if (Test-Path $pidFile) {
    Write-Host "Existing local debug state found. Stopping previous processes first..."
    & $stopScript -PidFile $pidFile
}

if (Test-TcpPortInUse -Port $VitePort) {
    Write-Host "Port $VitePort is already in use. Trying to stop repo-local debug processes..."
    & $stopScript
}

if (Test-TcpPortInUse -Port $VitePort) {
    throw "Port $VitePort is still in use. Close the process using it or choose another port with -VitePort."
}

if (-not $SkipInstall -and -not (Test-Path (Join-Path $uiRoot "node_modules"))) {
    Write-Host "node_modules not found. Running npm install..."
    Push-Location $uiRoot
    try {
        npm install
    }
    finally {
        Pop-Location
    }
}

$viteOut = Join-Path $runtimeDir "vite.out.log"
$viteErr = Join-Path $runtimeDir "vite.err.log"
$appOut = Join-Path $runtimeDir "app.out.log"
$appErr = Join-Path $runtimeDir "app.err.log"

Write-Host "Starting React UI at $viteUrl..."
$viteProcess = Start-Process `
    -FilePath "npm.cmd" `
    -ArgumentList @("run", "dev", "--", "--host", "localhost", "--port", "$VitePort", "--strictPort") `
    -WorkingDirectory $uiRoot `
    -RedirectStandardOutput $viteOut `
    -RedirectStandardError $viteErr `
    -WindowStyle Hidden `
    -PassThru

try {
    @{
        startedAt = (Get-Date).ToString("o")
        viteUrl = $viteUrl
        processes = @(
            @{ name = "vite"; pid = $viteProcess.Id }
        )
        logs = @{
            viteOut = $viteOut
            viteErr = $viteErr
            appOut = $appOut
            appErr = $appErr
        }
    } | ConvertTo-Json -Depth 4 | Set-Content -Path $pidFile -Encoding UTF8

    Wait-ForVite -Url $viteUrl

    $appArguments = if ($NoHotReload) {
        @("run", "--project", $appProject)
    }
    else {
        @("watch", "--project", $appProject, "run")
    }
    $appProcessName = if ($NoHotReload) { "desktop" } else { "desktop-watch" }
    $appStartLabel = if ($NoHotReload) { "Starting WPF app..." } else { "Starting WPF app with dotnet watch..." }

    Write-Host $appStartLabel
    $previousRestartOnRudeEdit = $env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT
    $previousSuppressEmojis = $env:DOTNET_WATCH_SUPPRESS_EMOJIS
    $env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT = "1"
    $env:DOTNET_WATCH_SUPPRESS_EMOJIS = "1"

    try {
        $appProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList $appArguments `
            -WorkingDirectory $repoRoot `
            -RedirectStandardOutput $appOut `
            -RedirectStandardError $appErr `
            -WindowStyle Hidden `
            -PassThru
    }
    finally {
        $env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT = $previousRestartOnRudeEdit
        $env:DOTNET_WATCH_SUPPRESS_EMOJIS = $previousSuppressEmojis
    }

    @{
        startedAt = (Get-Date).ToString("o")
        viteUrl = $viteUrl
        processes = @(
            @{ name = "vite"; pid = $viteProcess.Id }
            @{ name = $appProcessName; pid = $appProcess.Id }
        )
        logs = @{
            viteOut = $viteOut
            viteErr = $viteErr
            appOut = $appOut
            appErr = $appErr
        }
    } | ConvertTo-Json -Depth 4 | Set-Content -Path $pidFile -Encoding UTF8

    Write-Host "Local debug started."
    Write-Host "Vite PID: $($viteProcess.Id). Desktop PID: $($appProcess.Id)."
    Write-Host "Logs are in $runtimeDir."
    if (-not $NoHotReload) {
        Write-Host "React uses Vite HMR. C# uses dotnet watch Hot Reload and restarts on rude edits."
    }

    if ($NoWait) {
        Write-Host "Processes will keep running. Stop them with scripts\dev-stop.ps1."
        $skipCleanup = $true
        return
    }

    Write-Host "Press Ctrl+C to stop both processes. Closing the desktop app also stops Vite."

    while ($true) {
        $viteAlive = $null -ne (Get-Process -Id $viteProcess.Id -ErrorAction SilentlyContinue)
        $appAlive = $null -ne (Get-Process -Id $appProcess.Id -ErrorAction SilentlyContinue)

        if (-not $viteAlive -or -not $appAlive) {
            break
        }

        Start-Sleep -Seconds 2
    }
}
finally {
    if (-not $skipCleanup) {
        & $stopScript -PidFile $pidFile
    }
}
