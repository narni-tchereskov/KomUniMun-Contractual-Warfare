# Instructions

## Dependencies

To build the DLL, install the [.NET SDK][1].

Place these KSP/Harmony assemblies inside `Plugins/Managed/`:

- `0Harmony.dll` - from `GameData/000_Harmony/`
- `Assembly-CSharp.dll` - from `KSP_x64_Data/Managed/`
- `UnityEngine.CoreModule.dll` - from `KSP_x64_Data/Managed/`
- `UnityEngine.dll` - from `KSP_x64_Data/Managed/`

## Layout

```text
Plugins
│   INSTRUCTIONS.md
│   VesselRectifier.csproj
│   settings.cfg.example
│
├───VesselRectifier
│       BdaIntegration.cs
│       Hardening.cs
│       HarmonyPatches.cs
│       VesselPositioned.cs
│       Positioning.cs
│       Settings.cs
│       VesselClassification.cs
│       VesselRectifier.cs
│       VesselTrack.cs
│       VesselTracking.cs
│
└───Managed
        0Harmony.dll
        Assembly-CSharp.dll
        UnityEngine.CoreModule.dll
        UnityEngine.dll
```

## Build

```sh
dotnet build VesselRectifier\KomUniMunVesselRectifier.csproj -c Release
```

The DLL is written to `VesselRectifier\KomUniMunVesselRectifier.dll`.

## Install

Copy `KomUniMunVesselRectifier.dll` into `GameData/ContractPacks/KUM/Plugins/`.

[1]: https://dotnet.microsoft.com/en-us/download
