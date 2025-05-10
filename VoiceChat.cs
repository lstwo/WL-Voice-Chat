using Steamworks;
using System.IO;
using System.Linq;
using HawkNetworking;
using ShadowLib.Networking;
using UnityEngine;
using WLProxChat;

public class VoiceChat : ShadowNetworkBehaviour
{
    public static float Volume;
    
    public AudioSource source;
    public PlayerController player;

    private MemoryStream output;
    private MemoryStream stream;
    private MemoryStream input;

    private int optimalRate;
    private int clipBufferSize;
    private float[] clipBuffer;

    private int playbackBuffer;
    private int dataPosition;
    private int dataReceived;

    private byte RPC_VOICE_DATA;
    
    private bool initialized = false;
    
    private int debugPacketsReceived;
    private float debugLastReceivedTime;
    private float debugLastReadData;
    private int debugLargestPlaybackBuffer;
    private byte[] debugLastReceivedData;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 450), "Voice Chat Debug", GUI.skin.window);
    
        GUILayout.Label($"Is Initialized: {initialized}");

        if (initialized)
        {
            GUILayout.Space(10);

            GUILayout.Label($"Is Owner: {networkObject?.IsOwner()}");
            GUILayout.Label($"Voice Mode: {VoiceChatMod.Mode}");
            GUILayout.Label($"Voice Recording: {SteamUser.VoiceRecord}");
            GUILayout.Label($"Has Voice Data: {SteamUser.HasVoiceData}");

            GUILayout.Space(10);
    
            GUILayout.Label($"Optimal Sample Rate: {optimalRate}");
            GUILayout.Label($"Clip Buffer Size: {clipBufferSize}");
            GUILayout.Label($"Playback Buffer: {playbackBuffer}");
            GUILayout.Label($"Data Position: {dataPosition}");
            GUILayout.Label($"Data Received: {dataReceived}");
            GUILayout.Label($"Last Read Audio Data: {debugLastReadData}");
            GUILayout.Label($"Largest Playback Buffer: {debugLargestPlaybackBuffer}");

            GUILayout.Space(10);

            GUILayout.Label($"Packets Received: {debugPacketsReceived}");
            GUILayout.Label($"Last Packet Time: {debugLastReceivedTime:F2}s");
            
            if (debugLastReceivedData != null)
            {
                GUILayout.Label($"Last Received Data: [{string.Join(", ", debugLastReceivedData)}]");
            }
        }

        GUILayout.EndArea();
    }


    protected override void RegisterRPCs(HawkNetworkObject networkObject)
    {
        base.RegisterRPCs(networkObject);

        RPC_VOICE_DATA = networkObject.RegisterRPC(RpcVoiceData);
    }

    protected override void NetworkPost(HawkNetworkObject networkObject)
    {
        base.NetworkPost(networkObject);
        
        networkObject.AssignOwnership(player.networkObject.GetOwner(), false);
        initialized = true;
    }

    protected override void Start()
    {
        base.Start();
        
        optimalRate = (int)SteamUser.OptimalSampleRate;

        clipBufferSize = optimalRate * 5;
        clipBuffer = new float[clipBufferSize];

        stream = new MemoryStream();
        output = new MemoryStream();
        input = new MemoryStream();

        source.clip = AudioClip.Create("VoiceData", 256, 1, optimalRate, true, OnAudioRead, null);
        source.loop = true;
        source.Play();
    }

    private void Update()
    {
        if (!initialized || networkObject == null || !networkObject.IsOwner())
        {
            return;
        }

        source.volume = Volume;
        var voiceMode = VoiceChatMod.Mode;

        if (voiceMode == VoiceChatMode.Off)
        {
            SteamUser.VoiceRecord = false;
        }
        else if(voiceMode == VoiceChatMode.AlwaysOn)
        {
            SteamUser.VoiceRecord = true;
        }
        else
        {
            SteamUser.VoiceRecord = Input.GetKey(KeyCode.V);
        }

        if (!SteamUser.HasVoiceData)
        {
            return;
        }
        
        var compressedWritten = SteamUser.ReadVoiceData(stream);
        stream.Position = 0;

        networkObject.SendRPCUnreliable(RPC_VOICE_DATA, RPCRecievers.All, new VoiceDataNetworkMessage
            {bytesWritten = compressedWritten, compressed = stream.GetBuffer()}.Serialize());
    }

    public void RpcVoiceData(HawkNetReader reader, HawkRPCInfo info)
    {
        if (!initialized)
        {
            return;
        }
        
        debugPacketsReceived++;
        debugLastReceivedTime = Time.time;
        
        var voiceData = reader.ReadHawkMessage<VoiceDataNetworkMessage>();
        var compressed = voiceData.compressed;
        var compressedWritten = voiceData.bytesWritten;
        debugLastReceivedData = compressed.Skip(14).Take(16).ToArray();
        
        input.Write(compressed, 0, compressed.Length);
        input.Position = 0;
        
        var uncompressedWritten = SteamUser.DecompressVoice(input, compressedWritten, output);
        input.Position = 0;
        
        var outputBuffer = output.GetBuffer();
        WriteToClip(outputBuffer, uncompressedWritten);
        output.Position = 0;
    }

    private void OnAudioRead(float[] data)
    {
        if (playbackBuffer > debugLargestPlaybackBuffer)
        {
            debugLargestPlaybackBuffer = playbackBuffer;
        }
        
        for (var i = 0; i < data.Length; ++i)
        {
            data[i] = 0;

            if (playbackBuffer <= 0)
            {
                continue;
            }
            
            dataPosition = (dataPosition + 1) % clipBufferSize;
            data[i] = clipBuffer[dataPosition];
            debugLastReadData = data[i];
            playbackBuffer --;
        }
    }

    private void WriteToClip(byte[] uncompressed, int iSize)
    {
        for (var i = 0; i < iSize; i += 2)
        {
            var converted = (short)(uncompressed[i] | uncompressed[i + 1] << 8) / 32767.0f;
            clipBuffer[dataReceived] = converted;
            Plugin.Logger.LogInfo(converted);

            dataReceived = (dataReceived +1) % clipBufferSize;

            playbackBuffer++;
        }
    }
}