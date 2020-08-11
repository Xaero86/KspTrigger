# Execute with Windows PowerShell to compile addon
param ($kspRoot='E:\Jeux\Steam\steamapps\common\Kerbal Space Program', $cSharpCompiler='C:\Windows\Microsoft.NET\Framework64\v4.0.30319', [switch]$deploy)

$ADDON_NAME = 'KspTrigger'

# Configure environment
Set-Location -Path $PSScriptRoot
$ErrorActionPreference = "Stop"
$envPaths = $env:Path -split ';'
if ($envPaths -notcontains $cSharpCompiler) {
    $env:Path += "$($cSharpCompiler);"
}

# Clean
$BUILDS_DIR = 'builds'
$PROD_DIR = [IO.Path]::Combine($BUILDS_DIR, $ADDON_NAME)
$ADDON_LIB_PATH = [IO.Path]::Combine($PROD_DIR, 'Plugins')

Remove-Item -path $PROD_DIR -recurse -ErrorAction Ignore
New-Item -ItemType Directory -Force -Path $ADDON_LIB_PATH > $null

# Production
$SRC_DIR = "src"
$SRC_SUBDIRS = "action", "condition", "event", "trigger", "ui", "utils"
$SRC_FILES = @()
$SRC_FILES += $([IO.Path]::Combine($SRC_DIR, '*.cs'))
ForEach ($subdir in $SRC_SUBDIRS)
{
  $SRC_FILES += $([IO.Path]::Combine($SRC_DIR, $subdir, '*.cs'))
}
$ADDON_LIB = "$($ADDON_NAME).dll"
$KSP_LIB = [IO.Path]::Combine($kspRoot, 'KSP_x64_Data\Managed')

csc -lib:$KSP_LIB -r:Assembly-CSharp.dll,Assembly-CSharp-firstpass.dll,UnityEngine.dll,UnityEngine.CoreModule.dll,UnityEngine.UI.dll,UnityEngine.AnimationModule.dll,UnityEngine.ImageConversionModule.dll,UnityEngine.IMGUIModule.dll,UnityEngine.TextRenderingModule.dll -t:library -out:"$($ADDON_LIB_PATH)/$($ADDON_LIB)" $SRC_FILES

if(!$?)
{
    Exit
}

# Archive
$RESOURCES_DIR = "Resources"
$RESOURCES_FILES = [IO.Path]::Combine($RESOURCES_DIR, '*')
cp $RESOURCES_FILES $PROD_DIR

$ARCHIVE_PATH = "$($BUILDS_DIR)\$($ADDON_NAME).zip"
Compress-Archive -Force -Path $PROD_DIR -DestinationPath $ARCHIVE_PATH

if($deploy)
{
    # Deployment
    $DELIVERY_DIR = [IO.Path]::Combine($kspRoot, 'GameData')
    Expand-Archive -Force -Path $ARCHIVE_PATH -DestinationPath $DELIVERY_DIR
}
