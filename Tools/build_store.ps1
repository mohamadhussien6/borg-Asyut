# Run from any directory with Unity closed. Requires Git LFS and Python 3.11+.
param(
    [ValidateSet("Windows", "Web", "Both")][string]$Target = "Both",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $Root
$PreviousOutput = $env:BORG_STORE_OUTPUT
try {
    if (!(Test-Path -LiteralPath $Unity -PathType Leaf)) {
        $StandaloneEditor = "C:\Program Files\Unity 6000.3.2f1\Editor\Unity.exe"
        if (Test-Path -LiteralPath $StandaloneEditor -PathType Leaf) { $Unity = $StandaloneEditor }
    }
    if (!(Test-Path -LiteralPath $Unity -PathType Leaf)) { throw "Unity not found. Pass -Unity with the correct executable." }
    git lfs pull
    if ($LASTEXITCODE -ne 0) { throw "Git LFS pull failed." }
    git lfs fsck
    if ($LASTEXITCODE -ne 0) { throw "Git LFS integrity check failed." }
    $Dirty = git status --porcelain
    if ($LASTEXITCODE -ne 0 -or $Dirty) { throw "Start with a clean Git checkout and commit intended changes first." }
    $Commit = (git rev-parse HEAD).Trim()
    $Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $Output = Join-Path $Root "Builds\Store-$Stamp"
    $Artifacts = Join-Path $Root "Artifacts\Store-$Stamp"
    if ((Test-Path $Output) -or (Test-Path $Artifacts)) { throw "Output already exists." }
    New-Item -ItemType Directory -Path $Output, $Artifacts | Out-Null
    $env:BORG_STORE_OUTPUT = $Output
    python -B -m unittest discover -s Tools -p test_package_store.py
    if ($LASTEXITCODE -ne 0) { throw "Packaging unit tests failed." }
    foreach ($Mode in @("EditMode", "PlayMode")) {
        $Results = Join-Path $Artifacts "$Mode.xml"
        $TestLog = Join-Path $Artifacts "$Mode.log"
        $TestArgs = "-batchmode -projectPath `"$Root`" -runTests -testPlatform $Mode -testResults `"$Results`" -logFile `"$TestLog`""
        $TestProcess = Start-Process -FilePath $Unity -ArgumentList $TestArgs -WindowStyle Hidden -Wait -PassThru
        if ($TestProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $Results)) { throw "$Mode failed; check $TestLog" }
        [xml]$TestReport = Get-Content -LiteralPath $Results -Raw
        if ([int]$TestReport.'test-run'.failed -ne 0 -or [int]$TestReport.'test-run'.passed -eq 0) { throw "$Mode contains failures or no passing tests." }
    }
    $Targets = if ($Target -eq "Both") { @("Windows","Web") } else { @($Target) }
    foreach ($T in $Targets) {
        $BuildTarget = if ($T -eq "Windows") { "Win64" } else { "WebGL" }
        $Log = Join-Path $Artifacts "$T-build.log"
        $Args = "-batchmode -quit -projectPath `"$Root`" -buildTarget $BuildTarget -executeMethod Ma3alem.Editor.StoreBuilds.$T -logFile `"$Log`""
        $Process = Start-Process -FilePath $Unity -ArgumentList $Args -WindowStyle Hidden -Wait -PassThru
        if ($Process.ExitCode -ne 0 -or !(Select-String -LiteralPath $Log -SimpleMatch "Store build succeeded:" -Quiet)) { throw "$T build failed; check $Log" }
        $Platform = $T.ToLowerInvariant()
        $Zip = Join-Path $Artifacts "Borg_Asyut_$T.zip"
        python -B Tools/package_store.py $Platform (Join-Path $Output $T) $Zip
        if ($LASTEXITCODE -ne 0) { throw "$T packaging failed." }
    }
    $Status = git status --porcelain
    $UnityVersion = (Get-Item -LiteralPath $Unity).VersionInfo.ProductVersion
    @("source_commit=$Commit", "unity=$UnityVersion", "targets=$($Targets -join ',')", "validation=EditMode, PlayMode, build and packaging; human QA pending", "working_tree_after_build=$($Status -join '; ')") | Set-Content -LiteralPath (Join-Path $Artifacts "BUILD_INFO.txt") -Encoding UTF8
    Write-Host "Packages and logs: $Artifacts"
    Write-Host "Do not upload until the launch checklist is signed off."
} finally {
    $env:BORG_STORE_OUTPUT = $PreviousOutput
    Pop-Location
}
