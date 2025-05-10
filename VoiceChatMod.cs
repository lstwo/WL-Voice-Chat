using System;
using System.Linq;
using lstwoMODS_Core;
using lstwoMODS_Core.Hacks;
using lstwoMODS_Core.UI.TabMenus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WLProxChat;

public class VoiceChatMod : BaseHack
{
    public override void ConstructUI(GameObject root)
    {
        var ui = new HacksUIHelper(root);
        
        ui.AddSpacer(6);

        ui.CreateLabel("Push To Talk key: T; Toggle Mute Key: M");
        
        ui.AddSpacer(6);

        voiceChatToggle = ui.CreateToggle("lstwo.VoiceChatToggle", "Enable Voice Chat", b => VoiceChat.enableVoiceChat = b);
        
        ui.AddSpacer(6);

        micModeLdb = ui.CreateLDBTrio("Microphone Mode", "lstwo.VoiceChatMode");
        micModeLdb.Dropdown.options = Enum.GetNames(typeof(VoiceChatMode)).Select(name => new Dropdown.OptionData(name)).ToList();
        micModeLdb.Button.OnClick += () => VoiceChat.Mode = (VoiceChatMode)micModeLdb.Dropdown.value;

        ui.AddSpacer(6);

        volumeLib = ui.CreateLIBTrio("Voice Chat Volume", "lstwo.VoiceChatVolume");
        volumeLib.Input.Component.characterValidation = InputField.CharacterValidation.Decimal;
        volumeLib.Button.OnClick += () => VoiceChat.Volume = Mathf.Clamp(float.Parse(volumeLib.Input.Text), 0, 25);

        ui.AddSpacer(6);
        
        spacialBlendLib = ui.CreateLIBTrio("Voice Chat Spacial Blend", "lstwo.VoiceChatSpacialBlend");
        spacialBlendLib.Input.Component.characterValidation = InputField.CharacterValidation.Decimal;
        spacialBlendLib.Button.OnClick += () => VoiceChat.SpacialBlend = Mathf.Clamp(float.Parse(spacialBlendLib.Input.Text), 0, 1);
        
        ui.AddSpacer(6);
    }

    public override void Update()
    {
        
    }
    
    public override void RefreshUI()
    {
        voiceChatToggle.SetIsOnWithoutNotify(VoiceChat.enableVoiceChat);
        micModeLdb.Dropdown.SetValueWithoutNotify((int)VoiceChat.Mode);
        volumeLib.Input.Component.SetTextWithoutNotify(VoiceChat.Volume.ToString());
        spacialBlendLib.Input.Component.SetTextWithoutNotify(VoiceChat.SpacialBlend.ToString());
    }

    public override string Name => "Voice Chat";
    public override string Description => "";
    public override HacksTab HacksTab => lstwoMODS_WobblyLife.Plugin.ExtraHacksTab;

    private HacksUIHelper.LDBTrio micModeLdb;
    private HacksUIHelper.LIBTrio volumeLib;
    private HacksUIHelper.LIBTrio spacialBlendLib;
    private Toggle voiceChatToggle;
}