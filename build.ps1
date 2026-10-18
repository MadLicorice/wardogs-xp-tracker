param(
    [ValidateSet('Build', 'Run', 'Test', 'Publish')]
    [string]$Action = 'Run',
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot
$appProject = Join-Path $repositoryRoot 'src\WardogsXpTracker.App\WardogsXpTracker.App.csproj'
$testProject = Join-Path $repositoryRoot 'tests\WardogsXpTracker.Core.Tests\WardogsXpTracker.Core.Tests.csproj'

if (-not (Get-Command $DotnetPath -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 10 SDK, or pass its dotnet.exe path with -DotnetPath.'
}

Push-Location $repositoryRoot
try {
    switch ($Action) {
        'Build' { & $DotnetPath build $appProject --configuration Debug --nologo }
        'Run' { & $DotnetPath run --project $appProject --configuration Debug }
        'Test' { & $DotnetPath test $testProject --nologo }
        'Publish' {
            $publishDirectory = Join-Path $repositoryRoot 'artifacts\capture-lab-win-x64'
            & $DotnetPath publish $appProject --configuration Release --runtime win-x64 --self-contained true --output $publishDirectory -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false --nologo
            if ($LASTEXITCODE -eq 0) {
                Write-Output ('Executable: ' + (Join-Path $publishDirectory 'WardogsXpTracker.exe'))
            }
        }
    }
    if ($LASTEXITCODE -ne 0) { throw ('.NET command failed with exit code ' + $LASTEXITCODE + '.') }
}
finally { Pop-Location }
