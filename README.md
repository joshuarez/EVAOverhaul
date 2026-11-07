# EVA Overhaul (KSP 1.12.5)

Sprinting, +2 inventory slots on every EVA suit, and snappier, less sticky EVA controls.
No Module Manager or Harmony needed.

## What it does

| Feature | Details |
|---|---|
| Sprint | Hold **LeftShift** while walking/running for 1.6x speed. Off automatically on ladders, jetpack, seats, swimming and ragdoll, so LeftShift is still "jetpack up" in space. |
| +2 inventory slots | Every kerbal EVA suit (standard, female, Vintage, Future, Slim) gets 2 extra slots. |
| Snappier controls | Faster turning, stronger jetpack, no sliding when you let go of the keys, stronger ladder push-off, and a clean release from hatches/ladders so kerbals don't re-grab the handle. |

Everything can be tuned in `GameData/EVAOverhaul/EVAOverhaul.cfg`.

## Installing (players)

Copy the `EVAOverhaul` folder from the zip into your KSP `GameData` folder:

```
Kerbal Space Program/GameData/EVAOverhaul/
    EVAOverhaul.cfg
    EVAOverhaul.version
    Plugins/EVAOverhaul.dll
```

If you uninstall, empty the two extra inventory slots first or their contents may be lost.

## Building (developers)

1. Copy `LocalDev.props.example` to `LocalDev.props` and put your KSP folder in it (the folder with `KSP_x64.exe`).
   On Linux/macOS also add `<KSPManaged>` pointing at `KSP_Data/Managed` or `KSP.app/Contents/Resources/Data/Managed`.
2. Open `EVAOverhaul.csproj` in Visual Studio, pick **Release**, and Build.
3. Each build copies the mod into your KSP `GameData`. Release builds also create
   `Releases/EVAOverhaul-<version>.zip`, ready to upload.

Command line alternative: `dotnet build -c Release -p:KSPDir="C:\path\to\Kerbal Space Program"`

## Releasing a new version

Bump the version in three places so they match:
`EVAOverhaul.csproj` (`<Version>`), `Properties/AssemblyInfo.cs`, and `GameData/EVAOverhaul/EVAOverhaul.version`.
Then build Release and upload the zip from `Releases/`.
