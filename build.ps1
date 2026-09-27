param(
    [string]$TimberbornPath = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn',
    [string]$HarmonyPath = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3284904751\0Harmony.dll',
    [string]$ModSettingsPath = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3283831040\version-1.1\Scripts',
    [switch]$SkipRestore
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$dotnet = Join-Path $PSScriptRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.tools/cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
# Regression check: Timberborn's LocalizationRecord expects ID, not Key.
foreach ($csv in (Get-ChildItem -LiteralPath content/Localizations -Filter '*.csv')) {
    # Import-Csv silently skips blank lines, but Timberborn rejects them.
    $lines = [IO.File]::ReadAllLines($csv.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ([string]::IsNullOrWhiteSpace($lines[$i])) {
            throw "Empty localization line in $($csv.Name) (line $($i + 1))"
        }
    }
    $header = Get-Content -LiteralPath $csv.FullName -TotalCount 1
    if ($header -cne 'ID,Text,Comment') { throw "Invalid Timberborn localization header in $($csv.Name): $header" }
    $records = @(Import-Csv -LiteralPath $csv.FullName)
    if (!$records.Count -or @($records | Where-Object { !$_.ID -or !$_.Text }).Count) {
        throw "Missing localization ID or text in $($csv.Name)"
    }
}
if (!$SkipRestore) {
    foreach ($project in @('src/ForestMushrooms/ForestMushrooms.csproj','tests/ForestMushrooms.Tests.csproj','tests/Compatibility/Compatibility.csproj')) {
        & $dotnet restore $project --configfile NuGet.Config
        if ($LASTEXITCODE) { throw "Restore failed: $project" }
    }
}
& $dotnet build src/ForestMushrooms/ForestMushrooms.csproj -c Release --no-restore "-p:TimberbornPath=$TimberbornPath" "-p:HarmonyPath=$HarmonyPath" "-p:ModSettingsPath=$ModSettingsPath"
if ($LASTEXITCODE) { throw 'Build failed' }
& $dotnet run --project tests/ForestMushrooms.Tests.csproj -c Release --no-restore
if ($LASTEXITCODE) { throw 'Tests failed' }
& $dotnet run --project tests/Compatibility/Compatibility.csproj -c Release --no-restore "-p:TimberbornPath=$TimberbornPath" "-p:HarmonyPath=$HarmonyPath" "-p:ModSettingsPath=$ModSettingsPath" -- (Join-Path $TimberbornPath 'Timberborn_Data/Managed') $HarmonyPath $ModSettingsPath
if ($LASTEXITCODE) { throw 'Blueprint compatibility tests failed' }

$package = Join-Path $PSScriptRoot 'dist/ForestMushrooms/version-1.1'
New-Item -ItemType Directory -Force $package | Out-Null
# Remove retired prototype files when rebuilding over an older staged package.
foreach ($obsolete in @('Collections/TemplateCollection.Buildings.LeafCoats.optional.blueprint.json',
    'Buildings/Food/ForestFermenter/TrunkFermenter.LeafCoats.blueprint.json',
    'GoodCollections/GoodCollection.Folktails.blueprint.json',
    'NeedCollections/NeedCollection.Folktails.blueprint.json',
    'Collections/GoodCollection.LeafCoatsAddons.optional.blueprint.json',
    'Collections/NeedCollection.LeafCoatsAddons.optional.blueprint.json',
    'Buildings/Food/Fermenter.LeafCoats.optional.blueprint.json')) {
    $obsoletePath = Join-Path $package $obsolete
    if (Test-Path -LiteralPath $obsoletePath) { Remove-Item -LiteralPath $obsoletePath }
}
Copy-Item -Path content/* -Destination $package -Recurse -Force
Copy-Item -LiteralPath src/ForestMushrooms/bin/Release/netstandard2.1/ForestMushrooms.dll -Destination $package -Force
# Generate a mushroom-only Folktails processor using installed vanilla models and balance.
# No game DLLs, models, textures, or third-party assets are packaged.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $TimberbornPath 'Timberborn_Data/StreamingAssets/Modding/Blueprints.zip'))
try {
    # The cloned model uses Iron Teeth timbermesh materials, even in Folktails.
    $factionPatch = Get-Content content/Factions/Faction.Folktails.blueprint.json -Raw | ConvertFrom-Json
    if ('IronTeeth' -notin $factionPatch.FactionSpec.'MaterialCollectionIds#append') {
        throw 'Folktails fermenter requires the IronTeeth material collection'
    }
    $materialEntry = $archive.GetEntry('MaterialCollections/MaterialCollection.IronTeeth.blueprint.json')
    if (!$materialEntry) { throw 'IronTeeth material collection not found in installed game' }
    $materialReader = [IO.StreamReader]::new($materialEntry.Open())
    try { $materials = $materialReader.ReadToEnd() | ConvertFrom-Json } finally { $materialReader.Dispose() }
    if ('Materials/UberAtlas/Materials/IronTeeth/BaseWood_DarkBrown.IronTeeth' -notin $materials.MaterialCollectionSpec.Materials) {
        throw 'Fermenter BaseWood_DarkBrown.IronTeeth dependency missing from installed collection'
    }
    $entry = $archive.Entries | Where-Object { $_.FullName -eq 'Buildings/Food/Fermenter/Fermenter.IronTeeth.blueprint.json' }
    if (!$entry) { throw 'Timberborn 1.1 Fermenter blueprint not found' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $fermenter = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
} finally { $archive.Dispose() }
$fermenter.TemplateSpec.TemplateName = 'ForestFermenter.Folktails'
$fermenter.TemplateSpec.BackwardCompatibleTemplateNames = @()
$fermenter.ManufactorySpec.ProductionRecipeIds = @('FermentedMushroom')
# Keep the original Iron Teeth name, description and flavor localization keys.
$buildingDir = Join-Path $package 'Buildings/Food/ForestFermenter'
New-Item -ItemType Directory -Force $buildingDir | Out-Null
$fermenter | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $buildingDir 'ForestFermenter.Folktails.blueprint.json') -Encoding utf8
Copy-Item -LiteralPath README.md -Destination (Join-Path $PSScriptRoot 'dist/ForestMushrooms/README.md') -Force
$version = (Get-Content content/manifest.json -Raw | ConvertFrom-Json).Version
Compress-Archive -Path (Join-Path $PSScriptRoot 'dist/ForestMushrooms') -DestinationPath (Join-Path $PSScriptRoot "dist/ForestMushrooms-$version.zip") -Force
Write-Output "Built dist/ForestMushrooms-$version.zip"
