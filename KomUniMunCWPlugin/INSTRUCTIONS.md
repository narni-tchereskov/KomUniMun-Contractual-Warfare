# Instructions

## Dependencies

To build the DLL, install the [.NET SDK][1].

Place these KSP/Harmony assemblies inside `KomUniMunCWPlugin/Managed/`:

- `0Harmony.dll` - from `GameData/000_Harmony/`
- `Assembly-CSharp.dll` - from `KSP_x64_Data/Managed/`
- `UnityEngine.CoreModule.dll` - from `KSP_x64_Data/Managed/`
- `UnityEngine.dll` - from `KSP_x64_Data/Managed/`

## Layout

```text
KomUniMunCWPlugin
│   INSTRUCTIONS.md
│   KomUniMunCW.csproj
│
├───Project
│       BdaIntegration.cs
│       HarmonyPatches.cs
│       ReflectionUtils.cs
│       Settings.cs
│       VerboseLogging.cs
│       VesselClassification.cs
│       VesselExtensions.cs
│       VesselFlags.cs
│       VesselPositioned.cs
│       VesselPositioning.cs
│       CW.cs
│       VesselTrack.cs
│       VesselTracking.cs
│
└───Managed
        0Harmony.dll
        Assembly-CSharp.dll
        ContractConfigurator.dll
        UnityEngine.CoreModule.dll
        UnityEngine.dll
```

## Build

```sh
dotnet build KomUniMunCWPlugin\KomUniMunCW.csproj -c Release
```

The DLL is written to `KomUniMunCWPlugin\KomUniMunCW.dll`.

## Install

Copy `KomUniMunCW.dll` into `GameData/ContractPacks/KUM/Plugins/`.

[1]: https://dotnet.microsoft.com/en-us/download
