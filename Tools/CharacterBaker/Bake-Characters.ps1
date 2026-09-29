<#
.SYNOPSIS
    Bakes the destroyer sprite frames from Kenney's Toon Characters poses (CC0).

.DESCRIPTION
    Each destroyer has an Idle and a Strike frame, written to Assets/_Project/Art/Characters.
    Poses are placed on a common 448 x 320 canvas (feet at the bottom, body center at x = 192)
    so every frame shares the same pivot, then customized:
      - Worker: Male person, with a sledgehammer drawn in its hand (raised, then slammed).
      - Master: Male adventurer, green tunic recolored to kung-fu red, kicks the tower.
      - Robot:  Robot, claw punch.
      - Kaze:   Female adventurer, red headband and white gi; her strike releases an energy blast (in game).
    The frames are committed, so this tool only needs to run when the art changes (Windows, PowerShell 5+).

.EXAMPLE
    .\Tools\CharacterBaker\Bake-Characters.ps1
#>
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\..\Assets\_Project\Art\Characters"),
    [string]$PreviewPath = (Join-Path $PSScriptRoot "..\..\Logs\CharacterPreviews\Roster.png")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -Path (Join-Path $PSScriptRoot "CharacterBaker.cs")

$sources = Join-Path $PSScriptRoot "Sources"
function Pose([string]$name) { New-Object System.Drawing.Bitmap (Join-Path $sources "character_$name.png") }
function Rgb([int]$r, [int]$g, [int]$b) { [System.Drawing.Color]::FromArgb($r, $g, $b) }

$kungFuRed = Rgb 200 48 58
$headbandRed = Rgb 212 44 53
$giWhite = Rgb 244 244 240

function Master([string]$pose) {
    $b = Pose "maleAdventurer_$pose"
    [CharacterBaker]::Recolor($b, 110, 180, 0.25, $kungFuRed, 0.34, 0, 256)      # green tunic -> red
    [CharacterBaker]::Compose($b)
}

function Kaze([string]$pose) {
    $b = Pose "femaleAdventurer_$pose"
    [CharacterBaker]::Recolor($b, 185, 235, 0.12, $headbandRed, 0.54, 0, 112)    # blue headband -> red
    [CharacterBaker]::Recolor($b, 185, 235, 0.12, $giWhite, 0.54, 112, 256)      # blue top -> white gi
    [CharacterBaker]::Recolor($b, 15, 40, 0.5, 0.75, $giWhite, 0.6, 196, 256)   # orange pants -> white (skin is lighter, untouched)
    [CharacterBaker]::Compose($b)
}

# Hammer grip (pose pixels), angle (degrees, clockwise) and handle length, tuned on the Male person attack poses.
$frames = [ordered]@{
    "Worker_Idle"   = [CharacterBaker]::ComposeWithHammer((Pose "malePerson_attack1"), 140, 108, -120, 105)
    "Worker_Strike" = [CharacterBaker]::ComposeWithHammer((Pose "malePerson_attack2"), 172, 200, 14, 105)
    "Master_Idle"   = Master "idle"
    "Master_Strike" = Master "kick"
    "Robot_Idle"    = [CharacterBaker]::Compose((Pose "robot_idle"))
    "Robot_Strike"  = [CharacterBaker]::Compose((Pose "robot_attack0"))
    "Kaze_Idle"     = Kaze "idle"
    "Kaze_Strike"   = Kaze "attack0"
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
foreach ($name in $frames.Keys) {
    $frames[$name].Save((Join-Path $OutDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}

New-Item -ItemType Directory -Force -Path (Split-Path $PreviewPath) | Out-Null
[CharacterBaker]::Sheet(@($frames.Values)).Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Baked $($frames.Count) frames to $(Resolve-Path $OutDir)"
