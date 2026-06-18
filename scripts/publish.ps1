param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path "$PSScriptRoot\.."
$uiDirectory = "$root\src\TanoDevClip.UI"
$appProject = "$root\src\TanoDevClip.App\TanoDevClip.App.csproj"
$publishDirectory = "$root\artifacts\publish"
$releasesDirectory = "$root\artifacts\releases"

if (-not (Test-Path $appProject)) {
    throw "App project not found: $appProject"
}

Remove-Item $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $releasesDirectory -Recurse -Force -ErrorAction SilentlyContinue

Push-Location $uiDirectory
npm ci
npm run lint
npm run build
Pop-Location

dotnet test "$root\TanoDevClip.sln" -c Release

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:Version=$Version `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o $publishDirectory

New-Item "$publishDirectory\ui" -ItemType Directory -Force | Out-Null

Copy-Item `
    "$uiDirectory\dist\*" `
    "$publishDirectory\ui" `
    -Recurse `
    -Force

dotnet tool run vpk pack `
    --packId ProtanoSoftware.TanoDevClip `
    --packVersion $Version `
    --packDir $publishDirectory `
    --mainExe TanoDevClip.App.exe `
    --packTitle "TanoDev Clip" `
    --icon "$root\src\TanoDevClip.App\Assets\AppIcon.ico" `
    --framework webview2 `
    --outputDir $releasesDirectory