param(
    [Parameter(Mandatory)]
    [string]$FixtureDirectory,
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$probeProject = Join-Path $repositoryRoot 'tools\WardogsXpTracker.CaptureProbe\WardogsXpTracker.CaptureProbe.csproj'
$probeAssembly = Join-Path $repositoryRoot 'tools\WardogsXpTracker.CaptureProbe\bin\Debug\net10.0-windows10.0.19041.0\WardogsXpTracker.CaptureProbe.dll'
$expectedTotals = @{ Assault=56486L; Medic=58891L; Recon=31175L; Support=48059L; Driver=59653L; Pilot=44702L }
$expectedLevels = @{ Assault=15; Medic=15; Recon=11; Support=14; Driver=15; Pilot=14 }
$expectedThresholds = @{ Assault=64500L; Medic=64500L; Recon=33750L; Support=54750L; Driver=64500L; Pilot=54750L }
$expectedCurrentFiles = @{
    'unlock-assault-current.png'='Assault'
    'unlock-medic-current.png'='Medic'
    'unlock-recon-current.png'='Recon'
    'unlock-support-current.png'='Support'
    'unlock-driver-current.png'='Driver'
    'unlock-pilot-current.png'='Pilot'
}
$liveFixtureCount = 0
$liveFixtures = @{
    'unlock-driver-live-current.png'='Driver'
    'unlock-driver-live-stale.png'='Driver'
    'unlock-recon-live-current.png'='Recon'
}
foreach ($file in $liveFixtures.Keys) {
    if (Test-Path -LiteralPath (Join-Path $FixtureDirectory $file)) {
        $expectedCurrentFiles[$file] = $liveFixtures[$file]
        $liveFixtureCount++
    }
}

Push-Location $repositoryRoot
try {
    & $DotnetPath build $probeProject --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'The screenshot probe did not build.' }
    $probeOutput = & $DotnetPath $probeAssembly --fixtures $FixtureDirectory
    if ($LASTEXITCODE -ne 0) { throw 'The screenshot probe did not run.' }
    $results = ($probeOutput -join [Environment]::NewLine) | ConvertFrom-Json
    $byFile = @{}
    foreach ($result in $results) { $byFile[$result.File] = $result.Analysis.Result }
    $failures = [System.Collections.Generic.List[string]]::new()

    foreach ($file in $expectedCurrentFiles.Keys) {
        $track = $expectedCurrentFiles[$file]
        $result = $byFile[$file]
        $reading = @($result.Readings | Where-Object Class -eq $track)
        if ($result.Status -ne 'Accepted' -or $reading.Count -ne 1 -or $reading[0].CumulativeXp -ne $expectedTotals[$track]) {
            $failures.Add($file + ': expected exact ' + $track + ' XP ' + $expectedTotals[$track])
            continue
        }
        if ($reading[0].NextThreshold -ne $expectedThresholds[$track] -or $reading[0].CurrentLevel -ne $expectedLevels[$track] -or $reading[0].ThresholdLevel -ne ($expectedLevels[$track] + 1)) {
            $failures.Add($file + ': expected threshold ' + $expectedThresholds[$track] + ', current level ' + $expectedLevels[$track] + ', and next level ' + ($expectedLevels[$track] + 1))
        }
    }

    foreach ($file in @('unlock-assault-stale.png','unlock-driver-stale.png')) {
        $result = $byFile[$file]
        if ($result.Status -ne 'NoCurrentXp' -or @($result.Readings).Count -ne 0) {
            $failures.Add($file + ': expected a rejected completed reward page')
        }
    }

    foreach ($file in @('progression-final.png','progression-cropped.png')) {
        $result = $byFile[$file]
        if ($result.Status -ne 'Accepted' -or $result.Screen -ne 'EndProgression' -or @($result.Readings).Count -ne 6) {
            $failures.Add($file + ': expected six final progression cards')
            continue
        }
        foreach ($track in $expectedTotals.Keys) {
            $reading = @($result.Readings | Where-Object Class -eq $track)
            if ($reading.Count -ne 1 -or $reading[0].CumulativeXp -ne $expectedTotals[$track]) {
                $failures.Add($file + ': expected exact ' + $track + ' XP ' + $expectedTotals[$track])
            }
            elseif ($reading[0].NextThreshold -ne $expectedThresholds[$track]) {
                $failures.Add($file + ': expected ' + $track + ' threshold ' + $expectedThresholds[$track])
            }
            elseif ($reading[0].CurrentLevel -ne $expectedLevels[$track] -or $reading[0].ThresholdLevel -ne ($expectedLevels[$track] + 1)) {
                $failures.Add($file + ': expected ' + $track + ' current level ' + $expectedLevels[$track] + ' and next level ' + ($expectedLevels[$track] + 1))
            }
        }
    }

    foreach ($file in @('bonus-chart.png','winning-faction-splash.png')) {
        $result = $byFile[$file]
        if ($result.Status -ne 'UnsupportedScreen' -or @($result.Readings).Count -ne 0) {
            $failures.Add($file + ': an unrelated image must not supply player XP')
        }
    }

    $expectedPlacements = @{ 'score-victory.png'=1; 'score-defeat-second.png'=2; 'score-defeat-third.png'=3 }
    foreach ($file in $expectedPlacements.Keys) {
        $result = $byFile[$file]
        $places = @($result.Placement.PossiblePlaces)
        if ($result.Status -ne 'Accepted' -or $result.Screen -ne 'EndScore' -or $places.Count -ne 1 -or $places[0] -ne $expectedPlacements[$file]) {
            $failures.Add($file + ': expected exact placement ' + $expectedPlacements[$file])
        }
    }

    if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
    Write-Output ('Passed: all ' + (15 + $liveFixtureCount) + ' images, including exact UNLOCK XP/threshold/current-level/next-level values, completed-page rejections, final cards, placements, and unrelated images.')
}
finally { Pop-Location }
