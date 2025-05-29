# Wobbly Life Voice Chat Mod

This mod is for [lstwoMODS](https://github.com/lstwoMODS/lstwoMODS-Core) and adds Steam voice chat support to Wobbly Life.

# Requirements

This mod requires [lstwoMODS Core](https://github.com/lstwoMODS/lstwoMODS-Core), [lstwoMODS Wobbly Life](https://github.com/lstwoMODS/lstwoMODS-WobblyLife) and [ShadowLib](https://github.com/lstwo/shadowlib).

You can also install it through the [lstwoMODS Installer](https://github.com/lstwoMODS/lstwoMODSInstaller).

# How To Install (manual)

Download both files and move the `.dll` file to `BepInEx/plugins` and the `globalgamemanagers` file to `Wobbly Life_Data`.

> [!IMPORTANT]
> This may break with updates so if your Wobbly Life doesn't boot after installing verify your game files through Steam and either wait for an update by me or do this:
> 1. Download [UABEA](https://github.com/nesrak1/UABEA/releases/latest)
> 2. Open the original `globalgamemanagers` file in UABEA
> 3. Find the Unnamed asset with Type `AudioManager` and click `Edit Data`
> 4. Change `m_DisableAudio` from `true` to `false`
> 5. Click `Ok` then go to `File > Save` and close

# How To Use

You can configure the mod by opening the **lstwoMODS menu** then going to **`Extra Mods > Voice Chat`**.
You can configure your microphone in the **Steam settings** either in the **Steam app** or in the **`Shift + Tab`** menu under the sections `Voice` and `In-Game Voice`.

# License

https://github.com/lstwo/license/blob/main/LICENSE.md
