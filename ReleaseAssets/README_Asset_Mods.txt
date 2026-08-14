When the game boots, this folder is automatically scanned for asset mods. Every
.bundle file found inside it replaces the matching bundle in the game's own
files, so you never have to overwrite anything in your Suzerain install.

Each subfolder is one asset mod. To install a mod, extract it into its own
subfolder here.

For example, you might have the following file structure:

    AssetMods/suzermoe/          <<< This subfolder is one asset mod
    AssetMods/suzermoe/Suzerain_Data/StreamingAssets/aa/StandaloneWindows64/portrait.bundle
    AssetMods/another_mod/       <<< This subfolder is another asset mod
    AssetMods/another_mod/icons.bundle


Unlike some other games, the folder structure inside a mod does not matter at
all. Bundles are matched by filename alone, at any depth, so you can extract a
mod exactly as it was downloaded and leave its folders untouched. All three of
these are equivalent:

    AssetMods/suzermoe/Suzerain_Data/StreamingAssets/aa/StandaloneWindows64/portrait.bundle
    AssetMods/suzermoe/portrait.bundle
    AssetMods/portrait.bundle

Only .bundle files are used. Anything else a mod ships (catalog.bin,
settings.json, readmes, screenshots) is ignored and can be left where it is.

If two asset mods replace the same bundle, only one of them can be loaded. The mod
whose folder comes first alphabetically is used, and a warning naming both mods
is written to the BepInEx console.


This folder does nothing until the loader is switched on. In
BepInEx/config/com.onehalf.suzerainunbound.cfg, set:

    [Asset Mods]
    CustomAssetLoader = true

If a mod seems to do nothing, check the BepInEx console. Bundles that match
nothing in your version of Suzerain are listed there as warnings, which usually
means the mod was built for a different version of the game.
