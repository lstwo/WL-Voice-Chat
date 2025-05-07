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
            var listener = __instance.gameObject.AddComponent<AudioListener>();
            
            var go = new GameObject("VoiceChatManager");
            var source = go.AddComponent<AudioSource>();
            
            var voiceChat = go.AddComponent<VoiceChat>();
            voiceChat.source = source;
            voiceChat.Initialize();
        }
    }
}