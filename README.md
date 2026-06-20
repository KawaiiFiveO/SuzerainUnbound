# Suzerain Unbound

![preview](/docs/preview.png)

Quality of Life patches for vanilla Suzerain. Remove limitations placed by the developers and add important features.

### [View Documentation Webpage](https://onehalf.dev/SuzerainUnbound/)

### Included patches

Each patch can be enabled or disabled in the configuration file.

**Skip Splash**: Skips the Torpor Games intro logos. Enabled by default.

**Fast Dialogue**: Instantly shows text on screen (similar to `Typewriter` OFF) and enables **Dialogue Skip**, which allows you to hold a key down (default: `LeftCtrl`) to rapidly advance the dialogue. You can even skip unread text (which is not allowed by the `Skip Read Dialogue Text` menu option in vanilla). Enabled by default.

**Run In Background**: Prevents the game from pausing or throttling framerate when Alt-Tabbed, which improves stability. Game audio can also be muted when tabbing out. Enabled by default (muted).

**Yes I'm Sure**: Automatically skip all in-game/setup "Are you sure about your decisions?" prompts. Optionally, skip all system prompts as well. Enabled by default (in-game only).

**Dota Camera Rebind**: Allow map dragging with the Middle-Mouse Button. (Does not affect Right-Mouse.) Enabled by default.

**Fix Camera Edge Pan**: Prevents endless panning when the mouse is on a second monitor. Optionally, disable edge panning entirely. Enabled by default (edge fix).

**Discord RPC**: Enables Discord Rich Presence for Suzerain, which displays info similar to Steam Rich Presence.

**Read All Reports:** Press a key to instantly mark all pending map reports or news articles as read (default: `F1`/`F2`).

**Torpor Mode Unlock**: Suzerain forces Torpor Mode ON when playing on a fresh profile. This allows it to be set to OFF so that you can save your game.

**Force Achievements**: Forces Steam achievements to unlock even if Torpor Mode is off.

Torpor Mode Unlock and Force Achievements are disabled by default because they are cheats that can alter your user profile or save files.

## Installation

Begin with a vanilla installation of Suzerain. (This patch is NOT compatible with mods that use MelonLoader, such as "Suzerain Modding Kit".)

Go to the [Releases page](https://github.com/KawaiiFiveO/SuzerainUnbound/releases) and download the latest "With Modloader" release. It will be named something like `SuzerainUnbound_v1.x.x_WithModloader.zip`, the version number will vary.

Extract the contents of this zip file directly into your main Suzerain directory (where `Suzerain.exe` is located).

(Optional) To enable or disable specific patches, go into the `Suzerain/BepInEx/config` folder and open `com.onehalf.suzerainunbound.cfg` in a text editor (such as Notepad). Edit the settings that you want, then save the file.

Launch the game and wait 1-2 minutes on the first boot for BepInEx to initialize. If you did it correctly, a second window will appear outside of the game that looks like this:

<img width="976" height="506" alt="bepinex" src="https://github.com/user-attachments/assets/f2677342-b1d7-4801-b529-6fee7c04a810" />

Enjoy the improvements!

### Game Updates

If there is a game update, Suzerain Unbound will stop working, and the `BepInEx/interop` folder will need to be deleted to launch the game. It is recommended to uninstall BepInEx entirely by following the instructions below, then install a new "With Modloader" release (if present).

### Advanced Users

If you already have BepInEx 6.0 installed and want to use Suzerain Unbound alongside other mods, you can download the "Plugin Only" release and install it by dropping `SuzerainUnbound.dll` directly into your existing `Suzerain/BepInEx/plugins` folder.

**NOTE**: This plugin requires **BepInEx 6.0 (IL2CPP) Bleeding Edge** (tested on build #764). It will **NOT** work on BepInEx 5.x. If you do not know what this means, please download the **"With Modloader"** bundle, which includes everything pre-configured!

### Uninstall

If you wish to temporarily disable BepInEx, you can rename the `BepInEx` folder in the `Suzerain` directory to something else (such as `zBepinEx`) to prevent it from launching when you start the game.

To disable only Suzerain Unbound specifically, remove `SuzerainUnbound.dll` from the `Suzerain/BepInEx/plugins` folder.

To permanently revert your game back to vanilla, delete the following items from the `Suzerain` directory:

```
BepInEx (folder)
dotnet (folder)
.doorstop_version
doorstop_config.ini
winhttp.dll
```

Re-verify your game files on Steam if needed (usually unnecessary).

## Development Setup

If you want to contribute to Suzerain Unbound or build it from source, you will need to link the project to your local installation of Suzerain.

### Prerequisites
1. You must have **Suzerain** installed.
2. You must have **BepInEx 6.0 (IL2CPP)** installed in your game directory. (tested on build #764)
3. You must have launched the game at least once with BepInEx active to generate the `BepInEx/interop` assemblies.

* Note: Standard BepInEx 5.x **will not work** as it does not support IL2CPP games.
* You can download the correct bleeding edge builds from the official BepInEx build server: [BepInEx Bleeding Edge Builds](https://builds.bepinex.dev/projects/bepinex_be) (Make sure to select the **Unity IL2CPP** build for your operating system, e.g., `BepInEx-Unity.IL2CPP-win-x64`).
* If there is a game update, you will need to delete the `interop` folder to regenerate the assemblies, then re-link them in the project.

### Linking your Game Files

1. Clone this repository to your local machine.
2. In the root directory of the project, locate the file named `Directory.Build.props.example`.
3. Copy this file and rename the copy to `Directory.Build.props`.
4. Open `Directory.Build.props` in a text editor and change `PATH_TO_YOUR_SUZERAIN_GAME_FOLDER` to your actual Suzerain installation directory (e.g., `C:\Program Files (x86)\Steam\steamapps\common\Suzerain`).
5. Open the solution in Visual Studio 2022 and compile! The compiler will now automatically find your local game assemblies.

## Other Stuff

### Feature Requests

You can submit a [feature request](https://github.com/KawaiiFiveO/SuzerainUnbound/issues) to suggest a specific patch, or submit a pull request if you have created a new patch or bug fix and would like it to be added. Contributions are welcome.

### Known Issues

**General Crashes**: Vanilla Suzerain/BepInEx (Bleeding Edge) is already known to be unstable, so adding mods to it probably doesn't help.

### Compatibility

Latest release of Suzerain Unbound: v1.4.1

Tested with Suzerain version 3.1.0 (Windows) Build: 175, and WILL break if there is a major game update. Submit an [issue report](https://github.com/KawaiiFiveO/SuzerainUnbound/issues) if a patch is broken AND you are using the correct version of Suzerain Unbound. Include the log files in your report.

If the latest release is outdated, you will have to wait for Suzerain Unbound to be updated. In the meantime, you must disable or uninstall BepInEx entirely to play vanilla Suzerain.

Suzerain Unbound is **NOT compatible with mods that use MelonLoader**, such as "Suzerain Modding Kit".

### Disclaimer

These patches are not guaranteed to be 100% safe, although it is very unlikely that they will cause any damage to your save files. If you are worried, it's always a good idea to backup your save files before modding the game. **If you cannot accept these risks, do not use Suzerain Unbound.**

Mods such as Force Achievements will permanently alter your user profile, and you may need to use tools such as Steam Achievement Manager if you wish to revert the changes. Do not enable these patches unless you are sure that you want their effects.

### License

Suzerain Unbound is licensed under the MIT License. The Modloader bundle includes BepInEx, used under the LGPL-2.1 license.
