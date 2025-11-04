#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Helper script to configure SignPath secrets for code signing

.DESCRIPTION
    This script helps repository maintainers configure the necessary GitHub secrets
    for SignPath code signing integration.

.PARAMETER ApiToken
    SignPath API token (from SignPath dashboard)

.PARAMETER OrganizationId
    SignPath organization ID (from SignPath dashboard)

.PARAMETER ProjectSlug
    SignPath project slug (from SignPath dashboard)

.PARAMETER Repository
    GitHub repository in format "owner/repo" (defaults to current repo)

.EXAMPLE
    .\setup-code-signing.ps1 -ApiToken "sp-xxx" -OrganizationId "org-xxx" -ProjectSlug "portable-image-gallery"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ApiToken,
    
    [Parameter(Mandatory=$true)]
    [string]$OrganizationId,
    
    [Parameter(Mandatory=$true)]
    [string]$ProjectSlug,
    
    [Parameter(Mandatory=$false)]
    [string]$Repository = "okamarcelo/portable-image-gallery"
)

Write-Host "?? Setting up SignPath Code Signing for ImageGallery" -ForegroundColor Cyan
Write-Host "Repository: $Repository" -ForegroundColor Gray

# Check if GitHub CLI is installed
$ghCli = Get-Command gh -ErrorAction SilentlyContinue
if (-not $ghCli) {
    Write-Host "? GitHub CLI (gh) is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check if user is authenticated
try {
    $currentUser = gh auth status --hostname github.com 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Not authenticated with GitHub CLI" -ForegroundColor Red
        Write-Host "Please run: gh auth login" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "? GitHub CLI authenticated" -ForegroundColor Green
} catch {
    Write-Host "? GitHub CLI authentication check failed" -ForegroundColor Red
    exit 1
}

# Set the secrets
Write-Host "`n?? Configuring GitHub repository secrets..." -ForegroundColor Cyan

try {
    # Set SIGNPATH_API_TOKEN
    Write-Host "Setting SIGNPATH_API_TOKEN..." -ForegroundColor Gray
    $result1 = gh secret set SIGNPATH_API_TOKEN --repo $Repository --body $ApiToken
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? SIGNPATH_API_TOKEN configured" -ForegroundColor Green
    } else {
        throw "Failed to set SIGNPATH_API_TOKEN"
    }

    # Set SIGNPATH_ORGANIZATION_ID
    Write-Host "Setting SIGNPATH_ORGANIZATION_ID..." -ForegroundColor Gray
    $result2 = gh secret set SIGNPATH_ORGANIZATION_ID --repo $Repository --body $OrganizationId
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? SIGNPATH_ORGANIZATION_ID configured" -ForegroundColor Green
    } else {
        throw "Failed to set SIGNPATH_ORGANIZATION_ID"
    }

    # Set SIGNPATH_PROJECT_SLUG
    Write-Host "Setting SIGNPATH_PROJECT_SLUG..." -ForegroundColor Gray
    $result3 = gh secret set SIGNPATH_PROJECT_SLUG --repo $Repository --body $ProjectSlug
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? SIGNPATH_PROJECT_SLUG configured" -ForegroundColor Green
    } else {
        throw "Failed to set SIGNPATH_PROJECT_SLUG"
    }

    Write-Host "`n?? All SignPath secrets configured successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "? Error configuring secrets: $_" -ForegroundColor Red
    exit 1
}

# Verify the secrets were set
Write-Host "`n?? Verifying secrets..." -ForegroundColor Cyan
try {
    $secrets = gh secret list --repo $Repository --json name | ConvertFrom-Json
    $expectedSecrets = @("SIGNPATH_API_TOKEN", "SIGNPATH_ORGANIZATION_ID", "SIGNPATH_PROJECT_SLUG")
    
    foreach ($expectedSecret in $expectedSecrets) {
        if ($secrets.name -contains $expectedSecret) {
            Write-Host "? $expectedSecret is set" -ForegroundColor Green
        } else {
            Write-Host "? $expectedSecret is missing" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "?? Could not verify secrets (this is normal): $_" -ForegroundColor Yellow
}

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "1. Create a PR with 'feature' label" -ForegroundColor White
Write-Host "2. Merge the PR to trigger a signed release" -ForegroundColor White
Write-Host "3. Verify the release contains signed executables" -ForegroundColor White
Write-Host "4. Test download - should not trigger Windows SmartScreen warnings" -ForegroundColor White

Write-Host "`n?? Documentation:" -ForegroundColor Cyan
Write-Host "- See docs/CODE_SIGNING.md for detailed information" -ForegroundColor White
Write-Host "- SignPath dashboard: https://app.signpath.io" -ForegroundColor White

Write-Host "`n? Code signing setup complete!" -ForegroundColor Green