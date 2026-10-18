#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$DotnetPath = 'dotnet',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$OutputDirectory,
    [ValidateRange(10, 600)]
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:OS -ne 'Windows_NT') { throw 'The native viewport checks require Windows and an interactive desktop.' }

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $runName = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssfffZ') + '-' + [Guid]::NewGuid().ToString('N')
    $OutputDirectory = Join-Path $artifactsRoot ('layout-checks\' + $runName)
}
elseif (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}
$reportDirectory = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
$artifactsPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $reportDirectory.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The report directory must be inside this repository''s artifacts directory.'
}

# Do not route reports through a junction or symbolic link outside the repository.
$ancestorPath = $reportDirectory
while ($ancestorPath.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase) -or $ancestorPath -eq $artifactsRoot) {
    if (Test-Path -LiteralPath $ancestorPath) {
        $ancestor = Get-Item -LiteralPath $ancestorPath -Force
        if (($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'The report directory cannot contain a junction or symbolic link.'
        }
        if (-not $ancestor.PSIsContainer) { throw 'A report-directory component is a file.' }
    }
    $ancestorPath = Split-Path -Parent $ancestorPath
}

$project = Join-Path $repositoryRoot 'tests\WardogsXpTracker.App.LayoutChecks\WardogsXpTracker.App.LayoutChecks.csproj'
if (-not (Test-Path -LiteralPath $project)) { throw 'The viewport-check project is missing from this repository.' }
$dotnetCommand = Get-Command -Name $DotnetPath -CommandType Application -ErrorAction Stop
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

Push-Location $repositoryRoot
try {
    & $dotnetCommand.Source build $project --configuration $Configuration --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'The viewport-check project did not build.' }

    $checkExe = Join-Path $repositoryRoot ('tests\WardogsXpTracker.App.LayoutChecks\bin\' + $Configuration + '\net10.0-windows10.0.19041.0\win-x64\WardogsXpTracker.App.LayoutChecks.exe')
    if (-not (Test-Path -LiteralPath $checkExe)) { throw 'The viewport-check executable is missing after the build.' }
    $stdoutPath = Join-Path $reportDirectory 'stdout.txt'
    $stderrPath = Join-Path $reportDirectory 'stderr.txt'
    $process = Start-Process -FilePath $checkExe -ArgumentList ('"' + $reportDirectory + '"') -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        throw ('The viewport checks exceeded ' + $TimeoutSeconds + ' seconds. Reports: ' + $reportDirectory)
    }
    $resultsPath = Join-Path $reportDirectory 'results.json'
    if (-not (Test-Path -LiteralPath $resultsPath)) {
        if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Encoding UTF8 | Write-Output }
        throw ('The viewport checks did not write results.json. Reports: ' + $reportDirectory)
    }
    $results = Get-Content -LiteralPath $resultsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    Write-Output ('Native viewport checks: ' + $results.Passed + ' passed, ' + $results.Failed + ' failed. App ' + $results.Version + '.')
    Write-Output ('Reports: ' + $reportDirectory)
    if ($process.ExitCode -ne 0 -or $results.Failed -gt 0) {
        $results.Checks | Where-Object { -not $_.Passed } | Select-Object -First 15 | ForEach-Object { Write-Output ('FAIL ' + $_.Id + ': ' + $_.Detail) }
        if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Encoding UTF8 | Write-Output }
        throw 'The native viewport checks failed.'
    }
}
finally { Pop-Location }
