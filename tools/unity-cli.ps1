param(
    [ValidateSet("open", "compile", "validate", "setup-save-binder", "test-editmode", "test-playmode")]
    [string]$Command = "compile",

    [string]$UnityPath = "",
    [string]$ProjectPath = "",
    [string]$LogPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $versionFile = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
    $projectVersion = Select-String -Path $versionFile -Pattern "m_EditorVersion:\s*(.+)" | ForEach-Object { $_.Matches[0].Groups[1].Value.Trim() }
    $UnityPath = "C:\Program Files\Unity\Hub\Editor\$projectVersion\Editor\Unity.exe"
}

if (!(Test-Path -LiteralPath $UnityPath)) {
    throw "Unity.exe not found: $UnityPath"
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    $logsDir = Join-Path $ProjectPath "Logs"
    New-Item -ItemType Directory -Force -Path $logsDir | Out-Null
    $LogPath = Join-Path $logsDir "unity-cli-$Command.log"
}

$baseArgs = @(
    "-projectPath", $ProjectPath,
    "-logFile", $LogPath
)

switch ($Command) {
    "open" {
        & $UnityPath @baseArgs
        exit $LASTEXITCODE
    }
    "compile" {
        & $UnityPath @baseArgs "-batchmode" "-quit" "-accept-apiupdate"
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0 -and (Select-String -Path $LogPath -Pattern "Scripts have compiler errors|error CS" -Quiet)) {
            $exitCode = 1
        }
        exit $exitCode
    }
    "validate" {
        & $UnityPath @baseArgs "-batchmode" "-quit" "-accept-apiupdate" "-executeMethod" "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.ValidateProject"
        $exitCode = $LASTEXITCODE
        if (!(Select-String -Path $LogPath -Pattern "Unity CLI validation finished" -Quiet)) {
            Write-Error "Unity exited before running ValidateProject. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-save-binder" {
        & $UnityPath @baseArgs "-batchmode" "-quit" "-accept-apiupdate" "-executeMethod" "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupSaveRuntimeBinder"
        $exitCode = $LASTEXITCODE
        if (!(Select-String -Path $LogPath -Pattern "Save runtime binder setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupSaveRuntimeBinder. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "test-editmode" {
        $resultsPath = Join-Path $ProjectPath "Logs\unity-editmode-results.xml"
        & $UnityPath @baseArgs "-batchmode" "-quit" "-runTests" "-testPlatform" "EditMode" "-testResults" $resultsPath
        exit $LASTEXITCODE
    }
    "test-playmode" {
        $resultsPath = Join-Path $ProjectPath "Logs\unity-playmode-results.xml"
        & $UnityPath @baseArgs "-batchmode" "-quit" "-runTests" "-testPlatform" "PlayMode" "-testResults" $resultsPath
        exit $LASTEXITCODE
    }
}
