$ErrorActionPreference = 'Stop'

$cred = @('protocol=https', 'host=github.com') | git credential-manager get 2>&1
$tokenMatch = $cred | Select-String -Pattern '^password=(.*)$'
$token = $tokenMatch.Matches[0].Groups[1].Value

$headers = @{
    Authorization = "token $token"
    Accept = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
}

$owner = 'eduhza'
$repo = 'responsabilimano'

# Get latest develop SHA
$branchInfo = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/branches/develop" -Headers $headers
$sha = $branchInfo.commit.sha
Write-Host "Latest develop SHA: $sha"

$maxAttempts = 40
$attempt = 0
while ($attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 15

    $checks = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/commits/$sha/check-runs" -Headers $headers
    $allCompleted = $true
    $allSuccess = $true
    foreach ($c in $checks.check_runs) {
        if ($c.status -ne 'completed') { $allCompleted = $false }
        elseif ($c.conclusion -eq 'failure' -and $c.name -ne 'deploy') { $allSuccess = $false }
    }

    if ($allCompleted) {
        foreach ($c in $checks.check_runs) {
            Write-Host "  $($c.name): $($c.status) / $($c.conclusion)"
        }
        if ($allSuccess) {
            Write-Host "CI all green!"
            exit 0
        } else {
            Write-Host "CI completed with failures"
            exit 1
        }
    }
    Write-Host "Attempt $attempt/$maxAttempts - CI still running..."
}

Write-Host "Timed out waiting for CI"
exit 1
