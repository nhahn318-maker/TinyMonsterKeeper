param(
    [ValidateSet("open", "compile", "validate", "setup-save-binder", "setup-fog-unlock-visuals", "setup-save-reset-tool", "setup-garden-monster-save", "setup-kabuto-monster", "setup-antie-monster", "reorganize-scene-hierarchy", "test-editmode", "test-playmode")]
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

function Invoke-UnityBatch {
    param(
        [string[]]$UnityArgs
    )

    $process = Start-Process -FilePath $UnityPath -ArgumentList $UnityArgs -Wait -PassThru
    return $process.ExitCode
}

switch ($Command) {
    "open" {
        Start-Process -FilePath $UnityPath -ArgumentList $baseArgs | Out-Null
        exit 0
    }
    "compile" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate"))
        if ($exitCode -eq 0 -and (Select-String -Path $LogPath -Pattern "Scripts have compiler errors|error CS" -Quiet)) {
            $exitCode = 1
        }
        exit $exitCode
    }
    "validate" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.ValidateProject"))
        if (!(Select-String -Path $LogPath -Pattern "Unity CLI validation finished" -Quiet)) {
            Write-Error "Unity exited before running ValidateProject. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-save-binder" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupSaveRuntimeBinder"))
        if (!(Select-String -Path $LogPath -Pattern "Save runtime binder setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupSaveRuntimeBinder. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-fog-unlock-visuals" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupFogUnlockVisuals"))
        if (!(Select-String -Path $LogPath -Pattern "Fog unlock visuals setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupFogUnlockVisuals. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-save-reset-tool" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.AddSaveAccountResetTool"))
        if (!(Select-String -Path $LogPath -Pattern "Save account reset tool setup finished" -Quiet)) {
            Write-Error "Unity exited before running AddSaveAccountResetTool. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-garden-monster-save" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupGardenMonsterSaveManager"))
        if (!(Select-String -Path $LogPath -Pattern "Garden monster save manager setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupGardenMonsterSaveManager. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-kabuto-monster" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupKabutoMonster"))
        if (!(Select-String -Path $LogPath -Pattern "Kabuto monster setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupKabutoMonster. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "setup-antie-monster" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.SetupAntieMonster"))
        if (!(Select-String -Path $LogPath -Pattern "Antie monster setup finished" -Quiet)) {
            Write-Error "Unity exited before running SetupAntieMonster. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "reorganize-scene-hierarchy" {
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-accept-apiupdate", "-executeMethod", "TinyMonsterKeeper.EditorAutomation.UnityCliTasks.ReorganizeSceneHierarchy"))
        if (!(Select-String -Path $LogPath -Pattern "Scene hierarchy reorganization finished" -Quiet)) {
            Write-Error "Unity exited before running ReorganizeSceneHierarchy. Check log: $LogPath"
            exit 1
        }
        exit $exitCode
    }
    "test-editmode" {
        $resultsPath = Join-Path $ProjectPath "Logs\unity-editmode-results.xml"
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-runTests", "-testPlatform", "EditMode", "-testResults", $resultsPath))
        exit $exitCode
    }
    "test-playmode" {
        $resultsPath = Join-Path $ProjectPath "Logs\unity-playmode-results.xml"
        $exitCode = Invoke-UnityBatch ($baseArgs + @("-batchmode", "-quit", "-runTests", "-testPlatform", "PlayMode", "-testResults", $resultsPath))
        exit $exitCode
    }
}
