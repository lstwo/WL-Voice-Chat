using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WLProxChat;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        
        new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll(typeof(Patches));
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private class Patches
    {
        [HarmonyPatch(typeof(GameplayCamera), "Awake")]
        [HarmonyPostfix]
        private static void GamePlayCamera_Awake_Postfix(ref GameplayCamera __instance)
        {
            __instance.gameObject.AddComponent<AudioListener>();
        }

        [HarmonyPatch(typeof(PlayerController), "Awake")]
        [HarmonyPostfix]
        public static void PlayerController_Awake_Postfix(ref PlayerController __instance)
        {
            var go = __instance.gameObject;
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1.0f;
            
            var voiceChat = go.AddComponent<VoiceChat>();
            voiceChat.player = __instance;
            voiceChat.source = source;
        }
    }
}