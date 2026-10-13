#Requires -Version 7.0
<#
.SYNOPSIS
  Review-first extraction of Lagosystem.Modules and its unit-test project.

.DESCRIPTION
  Preserves original Git authors, dates and branch ancestry by using
  git-filter-repo, which cannot be simulated accurately with GitHub's file API.
  DEFAULT: local dry run, no push. Use -Publish explicitly to publish.
  No build, tests, GitHub Actions or NuGet publishing will be executed.

.EXAMPLE
  ./scripts/Import-FoundationModules.ps1
.EXAMPLE
  ./scripts/Import-FoundationModules.ps1 -Publish
#>
[CmdletBinding()]
param(
    [switch]$Publish,
    [switch]$KeepWorkDirectory,
    [string]$SourceRepository = 'https://github.com/leonardo-nicolas/Lagosystem.git',
    [string]$DestinationRepository = 'https://github.com/Lagosystem/Lagosystem.Modules.NET.git',
    [string]$SourceFeatureBranch = 'feature/platform-country-administration',
    [string]$WorkingDirectory = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)][string[]]$Parameters,
        [string]$Location = ''
    )
    if ($Location) { & git --no-pager -C $Location @Parameters }
    else { & git --no-pager @Parameters }
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed ($LASTEXITCODE): git $($Parameters -join ' ')"
    }
}

foreach ($executable in @('git', 'tar')) {
    if (-not (Get-Command $executable -ErrorAction SilentlyContinue)) {
        throw "Required executable not found: $executable"
    }
}
# First prefer Git's official filter-repo subcommand. On Windows, pip --user
# may install the Python module successfully but not add Scripts to PATH.
# In that case invoke the module through the same Python launcher instead.
$script:FilterRepoMode = ''
$script:FilterRepoPython = ''
try {
    & git filter-repo --version 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $script:FilterRepoMode = 'git' }
}
catch {
    # Missing executable: test the Python module fallback below.
}

if ([string]::IsNullOrWhiteSpace($script:FilterRepoMode)) {
    foreach ($pythonLauncher in @('py', 'python', 'python3')) {
        if (-not (Get-Command $pythonLauncher -ErrorAction SilentlyContinue)) { continue }
        try {
            & $pythonLauncher -m git_filter_repo --version 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                $script:FilterRepoMode = 'python'
                $script:FilterRepoPython = $pythonLauncher
                break
            }
        }
        catch {
            # Try another available Python launcher.
        }
    }
}

if ([string]::IsNullOrWhiteSpace($script:FilterRepoMode)) {
    throw 'git-filter-repo not found. Run: py -m pip install --user --upgrade git-filter-repo ; then verify: py -m git_filter_repo --version'
}
Write-Host "git-filter-repo available via: $($script:FilterRepoMode)"

function Invoke-FilterRepo {
    param(
        [Parameter(Mandatory = $true)][string]$Location,
        [Parameter(Mandatory = $true)][string[]]$Parameters
    )

    if ($script:FilterRepoMode -eq 'git') {
        Invoke-Git -Location $Location -Parameters (@('filter-repo') + $Parameters)
        return
    }

    Push-Location -LiteralPath $Location
    try {
        & $script:FilterRepoPython -m git_filter_repo @Parameters
        if ($LASTEXITCODE -ne 0) {
            throw "git-filter-repo Python module failed ($LASTEXITCODE): $($Parameters -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}
if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
    $WorkingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('lagosystem-modules-' + [Guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $WorkingDirectory) {
    throw "Working directory must not exist: $WorkingDirectory"
}

New-Item -ItemType Directory -Path $WorkingDirectory | Out-Null
$original = Join-Path $WorkingDirectory 'original.git'
$filtered = Join-Path $WorkingDirectory 'filtered.git'
$destination = Join-Path $WorkingDirectory 'destination'
$snapshot = Join-Path $WorkingDirectory 'snapshot'
$archive = Join-Path $WorkingDirectory 'latest-helper.tar'
$completed = $false

try {
    Write-Host 'Cloning private Foundation and making an isolated history copy...'
    Invoke-Git -Parameters @('clone', '--mirror', $SourceRepository, $original)
    Invoke-Git -Parameters @('clone', '--mirror', $original, $filtered)

    # Preserve the final, independently revised helper before removing its
    # potentially encumbered historical implementations from ALL Git commits.
    New-Item -ItemType Directory -Path $snapshot | Out-Null
    Invoke-Git -Parameters @(
        "--git-dir=$original", 'archive', '--format=tar', "--output=$archive",
        "refs/heads/$SourceFeatureBranch", '--',
        'backend/Lagosystem.Modules/Helpers/ThrowHelper.Collections.cs'
    )
    & tar -xf $archive -C $snapshot
    if ($LASTEXITCODE -ne 0) { throw 'Could not extract the safe current helper.' }

    Write-Host 'Filtering private ERP history to modules and module tests only...'
    Invoke-FilterRepo -Location $filtered -Parameters @(
        '--force',
        '--path', 'backend/Lagosystem.Modules/',
        '--path', 'tests/Lagosystem.Modules.Tests/',
        '--path-rename', 'backend/Lagosystem.Modules/:src/Lagosystem.Modules/'
    )

    # Remove the implementation and references from every historical commit.
    # Reintroduce ONLY the current, reviewed helper later as a new clean commit.
    Invoke-FilterRepo -Location $filtered -Parameters @(
        '--force', '--invert-paths',
        '--path', 'src/Lagosystem.Modules/Collections/StringList.cs',
        '--path', 'src/Lagosystem.Modules/Helpers/ThrowHelper.Collections.cs',
        '--path', 'tests/Lagosystem.Modules.Tests/StringListTests.cs'
    )

    $historyScan = @(Invoke-Git -Parameters @(
        "--git-dir=$filtered", 'log', '--all', '--pretty=format:', '--name-only',
        '-G', 'StringList', '--',
        'src/Lagosystem.Modules/', 'tests/Lagosystem.Modules.Tests/'
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    if ($historyScan.Count -gt 0) {
        Write-Warning ('Legacy resource/symbol names containing StringList still occur in historical snapshots: ' + ($historyScan -join ', '))
        Write-Warning 'The historical implementation and StringListTests are excluded; review obsolete resource keys separately before a NuGet release.'
    }

    Write-Host 'Integrating sanitized source ancestry with public README/LICENSE...'
    Invoke-Git -Parameters @('clone', $DestinationRepository, $destination)
    Invoke-Git -Location $destination -Parameters @('remote', 'add', 'filtered', $filtered)
    Invoke-Git -Location $destination -Parameters @('fetch', 'filtered')
    Invoke-Git -Location $destination -Parameters @(
        'switch', '-c', 'migration/foundation-modules',
        "filtered/$SourceFeatureBranch"
    )
    Invoke-Git -Location $destination -Parameters @(
        'merge', '--no-ff', '--allow-unrelated-histories',
        '-m', 'merge(modules): integrate sanitized native Foundation history with standalone Apache-2.0 project',
        'origin/main'
    )

    $testCsproj = Join-Path $destination 'tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj'
    $testXml = [System.IO.File]::ReadAllText($testCsproj)
    $oldRef = '../../backend/Lagosystem.Modules/Lagosystem.Modules.csproj'
    $newRef = '../../src/Lagosystem.Modules/Lagosystem.Modules.csproj'
    if (-not $testXml.Contains($oldRef)) {
        throw 'Unexpected unit-test project reference; stop and review.'
    }
    [System.IO.File]::WriteAllText(
        $testCsproj, $testXml.Replace($oldRef, $newRef),
        [System.Text.UTF8Encoding]::new($false)
    )

    $latestHelper = Join-Path $snapshot 'backend/Lagosystem.Modules/Helpers/ThrowHelper.Collections.cs'
    $cleanHelper = Join-Path $destination 'src/Lagosystem.Modules/Helpers/ThrowHelper.Collections.cs'
    if (-not (Test-Path -LiteralPath $latestHelper)) { throw 'Snapshot helper is missing.' }
    Copy-Item -LiteralPath $latestHelper -Destination $cleanHelper

    # The latest source still has one orphaned internal method named after
    # StringList. Its sole historical caller was the excluded implementation.
    # Remove only that dead method; retain generic array-validation helpers.
    $helperText = [System.IO.File]::ReadAllText($cleanHelper)
    $obsoleteMethod = '(?m)^[ \t]*\[MethodImpl\(MethodImplOptions\.NoInlining\)\]\r?\n[ \t]*internal static Exception ThrowForDuplicateOnStringList\(\)\r?\n[ \t]*=> new InvalidOperationException\(ExceptionsResource\.DuplicateValueTStringListAddObject\);\r?\n'
    $cleanedHelper = [regex]::Replace($helperText, $obsoleteMethod, '')
    if ($cleanedHelper -eq $helperText) {
        throw 'Orphaned StringList exception helper not located as expected; review before publishing.'
    }
    [System.IO.File]::WriteAllText($cleanHelper, $cleanedHelper, [System.Text.UTF8Encoding]::new($false))

    Invoke-Git -Location $destination -Parameters @(
        'add', '--', 'src/Lagosystem.Modules/Helpers/ThrowHelper.Collections.cs',
        'tests/Lagosystem.Modules.Tests/Lagosystem.Modules.Tests.csproj'
    )
    Invoke-Git -Location $destination -Parameters @(
        'commit', '-m',
        'refactor(extraction): repair standalone test reference and restore safe collection helper',
        '-m',
        'Exclude StringList and its helper history from all public source commits. No CI, builds or tests executed.'
    )

    $branches = @(Invoke-Git -Parameters @(
        "--git-dir=$filtered", 'for-each-ref', '--format=%(refname:short)', 'refs/heads'
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    Write-Host 'Filtered branches with native original commit timestamps:'
    foreach ($branch in $branches) {
        $sha = (Invoke-Git -Parameters @("--git-dir=$filtered", 'rev-parse', "refs/heads/$branch") | Out-String).Trim()
        Write-Host "  $branch => $sha"
    }
    Write-Host 'Proposed main commit:'
    Invoke-Git -Location $destination -Parameters @('log', '-1', '--format=%H%n%an%n%aI%n%s')
    Write-Host 'Files extracted to src/ and tests/:'
    Invoke-Git -Location $destination -Parameters @('ls-tree', '-r', '--name-only', 'HEAD', '--', 'src/', 'tests/')
    Write-Host 'Changes vs existing destination main:'
    Invoke-Git -Location $destination -Parameters @('diff', '--stat', 'origin/main..HEAD')

    if (-not $Publish) {
        Write-Host 'DRY RUN complete; nothing pushed. No compilation, tests, Actions or NuGet release.' -ForegroundColor Yellow
        Write-Host 'Review the report, then rerun with -Publish to publish the merge and branches.'
        $completed = $true
        return
    }

    Write-Host 'Publishing main by normal fast-forward push...' -ForegroundColor Yellow
    Invoke-Git -Location $destination -Parameters @('push', 'origin', 'HEAD:refs/heads/main')
    foreach ($branch in $branches) {
        $target = if ($branch -eq 'main') { 'history/foundation-main' } else { $branch }
        Invoke-Git -Location $destination -Parameters @(
            'push', 'origin', "refs/remotes/filtered/$branch" + ":refs/heads/$target"
        )
    }
    Write-Host 'History and branches published. CI, builds, tests and NuGet were NOT run.' -ForegroundColor Green
    $completed = $true
}
finally {
    if (-not $completed) {
        Write-Warning "Migration stopped. Preserve the temporary files for inspection: $WorkingDirectory"
    }
    elseif ($KeepWorkDirectory -or -not $Publish) {
        Write-Host "Kept reviewable temporary repositories at: $WorkingDirectory"
    }
    else {
        Remove-Item -LiteralPath $WorkingDirectory -Recurse -Force
    }
}
