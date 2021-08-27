param ($path, $version)

$absolutePath = Resolve-Path -Path $path
[System.Collections.Generic.List[string]]$lines = Get-Content -Path $absolutePath
$unreleasedRefLineIndex = $lines.FindLastIndex({ param ($x) $x.StartsWith("[Unreleased]:") })
$unreleasedRefLine = $lines[$unreleasedRefLineIndex]
$versionStartIndex = $unreleasedRefLine.LastIndexOf('/') + 1
$unreleasedRefPrefix = $unreleasedRefLine.Substring(0, $versionStartIndex)
$newUnreleasedRefLine = "${unreleasedRefPrefix}v${version}...HEAD"
$lines[$unreleasedRefLineIndex] = $unreleasedRefLine.Replace("Unreleased", $version).Replace("...HEAD", "...v${version}")
$lines.Insert($unreleasedRefLineIndex, $newUnreleasedRefLine)

$unreleasedStartLineIndex = $lines.IndexOf("## [Unreleased]")
$formattedDate = Get-Date -UFormat "%Y-%m-%d"
$lines[$unreleasedStartLineIndex] = "## [${version}] - ${formattedDate}"
$lines.Insert($unreleasedStartLineIndex, "")
$lines.Insert($unreleasedStartLineIndex, "## [Unreleased]")

Set-Content -Path $path -Value $lines