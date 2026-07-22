[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SpecId,

    [switch]$MainPR
)

$ErrorActionPreference = 'Stop'

function Get-GitHubToken {
    $inputLines = @('protocol=https', 'host=github.com')
    $output = $inputLines | git credential-manager get 2>&1
    $match = $output | Select-String -Pattern '^password=(.*)$'
    if (-not $match) {
        throw 'Could not retrieve GitHub token from git credential manager.'
    }
    return $match.Matches[0].Groups[1].Value
}

function Invoke-GitHubApi {
    param(
        [string]$Uri,
        [string]$Method = 'GET',
        [string]$Body = $null
    )

    $token = Get-GitHubToken
    $headers = @{
        Authorization = "token $token"
        Accept = 'application/vnd.github+json'
        'X-GitHub-Api-Version' = '2022-11-28'
    }

    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $headers
        ContentType = 'application/json'
        UseBasicParsing = $true
    }

    if ($Body) {
        $params.Body = $Body
    }

    return Invoke-RestMethod @params
}

function Get-OrCreatePullRequest {
    param(
        [string]$Head,
        [string]$Base,
        [string]$Title
    )

    $owner = 'eduhza'
    $repo = 'responsabilimano'
    $existing = Invoke-GitHubApi -Uri "https://api.github.com/repos/$owner/$repo/pulls?head=$owner`:$Head&base=$Base"
    if ($existing) {
        return $existing[0]
    }

    $body = @{
        title = $Title
        head = $Head
        base = $Base
        body = "Closes $SpecId"
    } | ConvertTo-Json -Compress

    return Invoke-GitHubApi -Uri "https://api.github.com/repos/$owner/$repo/pulls" -Method POST -Body $body
}

$branch = git branch --show-current
if (-not $branch) {
    throw 'Could not determine current git branch.'
}

$status = git status --short
if ($status) {
    Write-Host "Staging changes for $SpecId..."
    git add -A
    git commit -m "Finalize $SpecId" --quiet
}

Write-Host "Pushing branch $branch to origin..."
git push origin $branch

Write-Host "Creating pull request to develop..."
$pr = Get-OrCreatePullRequest -Head $branch -Base 'develop' -Title "Implement $SpecId"
Write-Host "Pull request to develop: $($pr.html_url)"

if ($MainPR) {
    Write-Host "Creating pull request from develop to main..."
    $prMain = Get-OrCreatePullRequest -Head 'develop' -Base 'main' -Title "Release $SpecId"
    Write-Host "Pull request to main: $($prMain.html_url)"
}
