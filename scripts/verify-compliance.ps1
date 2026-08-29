<#
.SYNOPSIS
    Architecture & Quality Standards Compliance Verification Script for EricksonLopez.DapperExtensions.
.DESCRIPTION
    Validates architectural invariants:
    1. Kebab-case naming for all markdown documentation and issue templates.
    2. Zero [Obsolete] usages in production code (src/).
    3. Presence of canonical MIT copyright header across all source files.
    4. Single top-level type per file in src/.
    5. Valid GitHub repository links referencing ericksonlopezf/dotnet-dapper-extensions.
    6. Official support and security email normalization (ericksonlopezf@gmail.com).
    7. Zero prohibited <NoWarn> suppressions across all projects.
    8. PackageProjectUrl points to ericksonlopez.dev/dapper-extensions.
    9. Synchronization of all ADR files with docs/adr/README.md and README.md.
    10. English-first language compliance in code and showcase.
#>

[CmdletBinding()]
param (
    [string]$RootDirectory = "."
)

$ErrorActionPreference = "Stop"
$violations = 0

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  REPOSITORY COMPLIANCE & ARCHITECTURE AUDITOR    " -ForegroundColor Cyan
Write-Host "  Repository: EricksonLopez.DapperExtensions       " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Kebab-case documentation verification
Write-Host "`n[1/10] Checking documentation file naming (kebab-case)..." -ForegroundColor Yellow
$docsFiles = Get-ChildItem -Path (Join-Path $RootDirectory "docs") -Recurse -Filter "*.md" -ErrorAction SilentlyContinue
$issueTemplates = Get-ChildItem -Path (Join-Path $RootDirectory ".github/ISSUE_TEMPLATE") -Recurse -Filter "*.md" -ErrorAction SilentlyContinue
$allDocsToCheck = @($docsFiles) + @($issueTemplates)
$badDocNames = 0
if ($allDocsToCheck) {
    foreach ($doc in $allDocsToCheck) {
        $filename = $doc.Name
        if ($filename -ne "README.md" -and ($filename -cne $filename.ToLower() -or $filename -match "_")) {
            Write-Host "  ❌ Non-kebab-case document: $($doc.FullName)" -ForegroundColor Red
            $violations++
            $badDocNames++
        }
    }
}
if ($badDocNames -eq 0) { Write-Host "  ✅ All documentation files use valid kebab-case naming." -ForegroundColor Green }

# 2. Zero Obsolete APIs in src/
Write-Host "`n[2/10] Checking for [Obsolete] attribute usages in src/..." -ForegroundColor Yellow
$srcCsFiles = Get-ChildItem -Path (Join-Path $RootDirectory "src") -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$obsoleteCount = 0
foreach ($cs in $srcCsFiles) {
    $lines = Get-Content $cs.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s*\[Obsolete\b" -and $lines[$i] -notmatch "^\s*//") {
            Write-Host "  ❌ [Obsolete] found in $($cs.FullName):$($i + 1)" -ForegroundColor Red
            $violations++
            $obsoleteCount++
        }
    }
}
if ($obsoleteCount -eq 0) { Write-Host "  ✅ Zero [Obsolete] attributes in production code." -ForegroundColor Green }

# 3. Canonical MIT Copyright Header
Write-Host "`n[3/10] Checking canonical MIT copyright headers..." -ForegroundColor Yellow
$missingHeaders = 0
foreach ($cs in $srcCsFiles) {
    $firstLine = (Get-Content $cs.FullName -TotalCount 1)
    if ($firstLine -notmatch "Copyright © Erickson Lopez\. MIT License\.") {
        Write-Host "  ❌ Missing MIT header in $($cs.FullName)" -ForegroundColor Red
        $violations++
        $missingHeaders++
    }
}
if ($missingHeaders -eq 0) { Write-Host "  ✅ All production C# files contain the required MIT copyright header." -ForegroundColor Green }

# 4. One Type Per File in src/
Write-Host "`n[4/10] Checking 'One Type Per File' rule in src/..." -ForegroundColor Yellow
$multiTypeFiles = 0
foreach ($cs in $srcCsFiles) {
    $rawContent = [System.IO.File]::ReadAllText($cs.FullName)
    $codeWithoutStrings = [System.Text.RegularExpressions.Regex]::Replace($rawContent, '@"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"', '')
    $codeWithoutComments = [System.Text.RegularExpressions.Regex]::Replace($codeWithoutStrings, '/\*[\s\S]*?\*/|//.*', '')
    $typeDecls = [System.Text.RegularExpressions.Regex]::Matches($codeWithoutComments, '(?m)^(?:public|internal|protected)\s+(?:sealed\s+|readonly\s+|abstract\s+|static\s+)*(?:class|struct|record|interface|enum|delegate)\s+([A-Za-z0-9_]+)')
    if ($typeDecls.Count -gt 1) {
        Write-Host "  ❌ Multiple top-level types in $($cs.FullName):" -ForegroundColor Red
        foreach ($td in $typeDecls) {
            Write-Host "     Type: $($td.Value.Trim())" -ForegroundColor DarkRed
        }
        $violations++
        $multiTypeFiles++
    }
}
if ($multiTypeFiles -eq 0) { Write-Host "  ✅ Every production file satisfies the 'One Type Per File' invariant." -ForegroundColor Green }

# 5. GitHub Repository Identity & Links
Write-Host "`n[5/10] Checking GitHub identity links (ericksonlopezf/dotnet-dapper-extensions)..." -ForegroundColor Yellow
$badLinks = 0
$allTrackedFiles = Get-ChildItem -Path $RootDirectory -Recurse -Include "*.cs", "*.md", "*.props", "*.targets" | Where-Object { $_.FullName -notmatch "\\(obj|bin|\.git)\\" }
foreach ($f in $allTrackedFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "github\.com/ericksonlopez/dotnet-dapper-extensions\b" -or $lines[$i] -match "github\.com/ericksonlopezf/dapper-extensions\b") {
            Write-Host "  ❌ Incorrect GitHub repo link in $($f.FullName):$($i + 1)" -ForegroundColor Red
            $violations++
            $badLinks++
        }
    }
}
if ($badLinks -eq 0) { Write-Host "  ✅ All GitHub URLs correctly target ericksonlopezf/dotnet-dapper-extensions." -ForegroundColor Green }

# 6. Official Contact & Support Email Normalization
Write-Host "`n[6/10] Checking contact and security email normalization (ericksonlopezf@gmail.com)..." -ForegroundColor Yellow
$badEmails = 0
$metaFiles = @("SECURITY.md", "CODE_OF_CONDUCT.md", "SUPPORT.md")
foreach ($meta in $metaFiles) {
    $fullPath = Join-Path $RootDirectory $meta
    if (Test-Path $fullPath) {
        $lines = Get-Content $fullPath
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "ericksonlopez\.dev@gmail\.com") {
                Write-Host "  ❌ Legacy email detected in $meta : line $($i + 1)" -ForegroundColor Red
                $violations++
                $badEmails++
            }
        }
    }
}
if ($badEmails -eq 0) { Write-Host "  ✅ Official contact emails normalized to ericksonlopezf@gmail.com." -ForegroundColor Green }

# 7. Prohibited NoWarn Suppressions Check
Write-Host "`n[7/10] Checking for prohibited NoWarn suppressions (CS1591, CS0618, CS0619, CA1852, CA1707)..." -ForegroundColor Yellow
$prohibitedNoWarnCount = 0
$propsAndCsproj = Get-ChildItem -Path $RootDirectory -Recurse -Include "*.props", "*.csproj" | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$prohibitedCodes = @("CS1591", "1591", "CS0618", "CS0619", "CA1852", "CA1707")
foreach ($proj in $propsAndCsproj) {
    $lines = Get-Content $proj.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "<NoWarn>(.*)</NoWarn>") {
            $matchedVal = $matches[1]
            foreach ($code in $prohibitedCodes) {
                if ($matchedVal -match "(?:^|;)\s*$code\s*(?:;|$)") {
                    Write-Host "  ❌ Prohibited NoWarn '$code' found in $($proj.FullName):$($i + 1)" -ForegroundColor Red
                    $violations++
                    $prohibitedNoWarnCount++
                }
            }
        }
    }
}
if ($prohibitedNoWarnCount -eq 0) { Write-Host "  ✅ Zero prohibited NoWarn suppressions across all projects." -ForegroundColor Green }

# 8. PackageProjectUrl Verification
Write-Host "`n[8/10] Checking PackageProjectUrl (https://ericksonlopez.dev/dapper-extensions)..." -ForegroundColor Yellow
$dirBuildProps = Join-Path $RootDirectory "Directory.Build.props"
$correctUrl = $false
if (Test-Path $dirBuildProps) {
    $content = Get-Content $dirBuildProps -Raw
    if ($content -match "<PackageProjectUrl>https://ericksonlopez\.dev/dapper-extensions</PackageProjectUrl>") {
        $correctUrl = $true
    }
}
if ($correctUrl) {
    Write-Host "  ✅ PackageProjectUrl is correctly set to https://ericksonlopez.dev/dapper-extensions." -ForegroundColor Green
} else {
    Write-Host "  ❌ PackageProjectUrl is missing or invalid in Directory.Build.props." -ForegroundColor Red
    $violations++
}

# 9. ADR Catalog Index Synchronization
Write-Host "`n[9/10] Checking ADR catalog synchronization (docs/adr/ vs README.md)..." -ForegroundColor Yellow
$adrDir = Join-Path $RootDirectory "docs/adr"
$adrIndex = Join-Path $adrDir "README.md"
$rootReadme = Join-Path $RootDirectory "README.md"
$adrFiles = Get-ChildItem -Path $adrDir -Filter "adr-*.md" -ErrorAction SilentlyContinue
$adrSyncIssues = 0

if ((Test-Path $adrIndex) -and (Test-Path $rootReadme)) {
    $adrIndexContent = Get-Content $adrIndex -Raw
    $rootReadmeContent = Get-Content $rootReadme -Raw
    foreach ($adr in $adrFiles) {
        $adrName = $adr.Name
        if ($adrIndexContent -notmatch [regex]::Escape($adrName)) {
            Write-Host "  ❌ ADR '$adrName' is not indexed in docs/adr/README.md" -ForegroundColor Red
            $violations++
            $adrSyncIssues++
        }
        if ($rootReadmeContent -notmatch [regex]::Escape($adrName)) {
            Write-Host "  ❌ ADR '$adrName' is not referenced in root README.md" -ForegroundColor Red
            $violations++
            $adrSyncIssues++
        }
    }
} else {
    Write-Host "  ❌ Missing ADR index or root README.md file." -ForegroundColor Red
    $violations++
    $adrSyncIssues++
}

if ($adrSyncIssues -eq 0) {
    Write-Host "  ✅ All ADR documents are fully indexed in docs/adr/README.md and README.md." -ForegroundColor Green
}

# 10. English Language Compliance in Code & Showcase
Write-Host "`n[10/10] Checking English language compliance across source code and showcase..." -ForegroundColor Yellow
$codeFiles = Get-ChildItem -Path $RootDirectory -Recurse -Include "*.cs" | Where-Object { $_.FullName -notmatch "\\(obj|bin|\.git)\\" }
$spanishKeywords = @(" método ", " métodos ", " ejecución ", " configuración ", " transaccional ", " descripción ", " nivel ", " casos de uso ")
$languageViolations = 0
foreach ($cf in $codeFiles) {
    $lines = Get-Content $cf.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($kw in $spanishKeywords) {
            if ($lines[$i] -match $kw) {
                Write-Host "  ❌ Spanish keyword '$($kw.Trim())' found in $($cf.FullName):$($i + 1)" -ForegroundColor Red
                $violations++
                $languageViolations++
            }
        }
    }
}
if ($languageViolations -eq 0) {
    Write-Host "  ✅ 100% English-first code and documentation compliance verified." -ForegroundColor Green
}

# Summary & Exit Code
Write-Host "`n==================================================" -ForegroundColor Cyan
if ($violations -gt 0) {
    Write-Host "  FAILED: $violations compliance violation(s) detected. " -ForegroundColor Red -BackgroundColor Black
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 1
} else {
    Write-Host "  SUCCESS: 100% Governance & Compliance Verified. Zero violations. " -ForegroundColor Green -BackgroundColor Black
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 0
}
