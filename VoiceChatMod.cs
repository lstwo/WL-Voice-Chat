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

        var ldb = ui.CreateLDBTrio("Voice Chat Mode", "lstwo.VoiceChatMode");
        ldb.Dropdown.options = Enum.GetNames(typeof(VoiceChatMode)).Select(name => new Dropdown.OptionData(name)).ToList();
        ldb.Button.OnClick += () => Mode = (VoiceChatMode)ldb.Dropdown.value;

        ui.AddSpacer(6);

        var lib = ui.CreateLIBTrio("Voice Chat Volume", "lstwo.VoiceChatVolume");
        lib.Input.Component.characterValidation = InputField.CharacterValidation.Decimal;
        lib.Button.OnClick += () => VoiceChat.Volume = Mathf.Clamp(float.Parse(lib.Input.Text), 0, 25);

        ui.AddSpacer(6);
    }

    public override void Update()
    {
        
    }

    public override void RefreshUI()
    {
        
    }

    public override string Name => "Voice Chat";
    public override string Description => "";
    public override HacksTab HacksTab => lstwoMODS_WobblyLife.Plugin.ExtraHacksTab;

    internal static VoiceChatMode Mode = VoiceChatMode.Off;
}