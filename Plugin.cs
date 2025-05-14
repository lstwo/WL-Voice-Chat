using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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

        [HarmonyPatch(typeof(PlayerControllerInteractor), "Awake")]
        [HarmonyPostfix]
        public static void PlayerController_Awake_Postfix(ref PlayerControllerInteractor __instance)
        {
            var go = new GameObject("Voice Chat Manager")
            {
                transform = { parent = __instance.transform }
            };

            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0.0f;
            
            var voiceChat = go.AddComponent<VoiceChat>();
            voiceChat.player = __instance.GetComponent<PlayerController>();
            voiceChat.audioSource = source;
        }

        [HarmonyPatch(typeof(Object), nameof(Destroy), typeof(Object))]
        [HarmonyPrefix]
        public static void Object_Destroy_Prefix(Object obj)
        {
            if (obj.name == "Voice Chat Manager")
            {
                throw new Exception("Voice Chat Manager destroyed!");
            }
        }
    }
}