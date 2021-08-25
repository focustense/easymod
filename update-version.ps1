param ($projectpath, $version)

$absolutePath = Resolve-Path -Path $projectpath
$csproj = New-Object xml
$csproj.PreserveWhitespace = $true
$csproj.Load($absolutePath)

$versionElement = $csproj.SelectSingleNode("/Project/PropertyGroup/Version")
$newVersion = $version
if ($newVersion -eq $null) {
	$parts = $versionElement.InnerText.split(".")
	$release = [int]$parts[2]
	$parts[2] = [string]($release + 1)
	$newVersion = $parts -join "."
}
$versionElement.InnerText = $newVersion

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.Encoding]::UTF8
$settings.OmitXmlDeclaration = $true
$writer = [System.Xml.XmlWriter]::Create($absolutePath, $settings)
try {
	$csproj.Save($writer)
} finally {
	$writer.Close()
}

Write-Output "::set-output name=version::$newVersion"