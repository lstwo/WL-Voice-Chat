using Steamworks;
using System.IO;
using HawkNetworking;
using ShadowLib.Networking;
using UnityEngine;

public partial class VoiceChat : ShadowNetworkBehaviour
{
    public AudioSource source;

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

    protected override void RegisterRPCs(HawkNetworkObject networkObject)
    {
        base.RegisterRPCs(networkObject);

        RPC_VOICE_DATA = networkObject.RegisterRPC(RpcVoiceData);
    }

    public void Initialize()
    {
        optimalRate = (int)SteamUser.OptimalSampleRate;

        clipBufferSize = optimalRate * 5;
        clipBuffer = new float[clipBufferSize];

        stream = new MemoryStream();
        output = new MemoryStream();
        input = new MemoryStream();

        source.clip = AudioClip.Create("VoiceData", (int)256, 1, (int)optimalRate, true, OnAudioRead, null);
        source.loop = true;
        source.Play();

        initialized = true;
    }

    private void Update()
    {
        if (networkObject == null || !initialized)
        {
            return;
        }
        
        SteamUser.VoiceRecord = Input.GetKey(KeyCode.V);

        if (!SteamUser.HasVoiceData)
        {
            return;
        }
        
        var compressedWritten = SteamUser.ReadVoiceData(stream);
        stream.Position = 0;

        networkObject.SendRPCUnreliable(RPC_VOICE_DATA, RPCRecievers.Others, 
            new VoiceDataNetworkMessage {bytesWritten = compressedWritten, compressed = stream.GetBuffer()}.Serialize());
    }

    public void RpcVoiceData(HawkNetReader reader, HawkRPCInfo info)
    {
        if (!initialized)
        {
            return;
        }
        
        var voiceData = reader.ReadHawkMessage<VoiceDataNetworkMessage>();
        var compressed = voiceData.compressed;
        var bytesWritten = voiceData.bytesWritten;
        
        input.Write(compressed, 0, bytesWritten);
        input.Position = 0;

        var uncompressedWritten = SteamUser.DecompressVoice(input, bytesWritten, output);
        input.Position = 0;

        var outputBuffer = output.GetBuffer();
        WriteToClip(outputBuffer, uncompressedWritten);
        output.Position = 0;
    }

    private void OnAudioRead(float[] data)
    {
        for (var i = 0; i < data.Length; ++i)
        {
            data[i] = 0;

            if (playbackBuffer <= 0)
            {
                continue;
            }
            
            dataPosition = (dataPosition + 1) % clipBufferSize;
            data[i] = clipBuffer[dataPosition];
            playbackBuffer --;
        }
    }

    private void WriteToClip(byte[] uncompressed, int iSize)
    {
        for (var i = 0; i < iSize; i += 2)
        {
            var converted = (short)(uncompressed[i] | uncompressed[i + 1] << 8) / 32767.0f;
            clipBuffer[dataReceived] = converted;

            dataReceived = (dataReceived +1) % clipBufferSize;

            playbackBuffer++;
        }
    }
}