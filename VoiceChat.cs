using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using HawkNetworking;
using ShadowLib.Networking;

namespace WLProxChat
{
    public class VoiceChat : ShadowNetworkBehaviour
    {
        public static float Volume = 1f;
        public static float SpacialBlend = 1f;
        public static VoiceChatMode Mode = VoiceChatMode.Off;
        public static bool EnableVoiceChat = false;
        public static float MaxDistance = 250f;
        public static AudioRolloffMode AudioRolloff = AudioRolloffMode.Logarithmic;

        public AudioSource audioSource;
        public PlayerController player;

        private ConcurrentQueue<float> audioQueue = new ConcurrentQueue<float>();
        private AudioClip streamingClip;

        private MemoryStream compressedStream = new MemoryStream();
        private MemoryStream decompressedStream = new MemoryStream();

        private const int channels = 1;

        private bool running = true;
        private int sampleRate = 16000;

        private Transform playerBodyTransform;

        private bool isMuted = false;

        private int totalSamplesQueued;
        private int totalSamplesDequeued;
        private string lastError = "";
        private int bufferCountLastFrame;

        private byte RPC_SEND_VOICE_DATA;
        
        private HawkConnection myConnection;
        private HawkConnection ownerConnection;

        protected override void Start()
        {
            base.Start();

            sampleRate = (int)SteamUser.OptimalSampleRate;

            streamingClip = AudioClip.Create("SteamVoice", sampleRate * 10, channels, sampleRate, true, OnAudioRead, OnAudioSetPosition);
            audioSource.clip = streamingClip;
            audioSource.loop = true;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 10f;
            audioSource.Play();

            running = true;
        }

        protected override void RegisterRPCs(HawkNetworkObject networkObject)
        {
            base.RegisterRPCs(networkObject);

            RPC_SEND_VOICE_DATA = networkObject.RegisterRPC(RpcReceiveVoiceData);
        }

        protected override void NetworkPost(HawkNetworkObject networkObject)
        {
            base.NetworkPost(networkObject);

            if (networkObject.IsServer())
            {
                networkObject.AssignOwnership(player.networkObject.GetOwner(), true);
            }
            
            StartCoroutine(VoiceCaptureLoop());
        }

        private void Update()
        {
            audioSource.spatialBlend = SpacialBlend;
            audioSource.maxDistance = MaxDistance;
        }

        private void FixedUpdate()
        {
            if (playerBodyTransform == null && player?.GetPlayerCharacter()?.GetPlayerBody() != null)
            {
                playerBodyTransform = player.GetPlayerCharacter().GetPlayerBody().transform;
            }
            
            if (playerBodyTransform != null)
            {
                transform.position = playerBodyTransform.position;
            }
        }
        
        private IEnumerator VoiceCaptureLoop()
        {
            var wait = new WaitForSeconds(0.025f);
            
            debug("voice capture loop method");

            while (running)
            {
                yield return wait;

                debug("voice capture loop");

                if (networkObject == null || !networkObject.IsOwner())
                {
                    continue;
                }

                SetSteamVoiceRecord();
                
                if (!SteamUser.HasVoiceData || !EnableVoiceChat)
                {
                    continue;
                }

                OwnerSendVoiceData();
                
                debug("finish voice capture loop");
            }
        }

        private void OwnerSendVoiceData()
        {
            debug("OwnerSendVoiceData");

            if (networkObject == null || !networkObject.IsOwner())
            {
                return;
            }
            
            debug("check");
            
            try
            {
                var compressed = SteamUser.ReadVoiceDataBytes();
                debug(compressed);

                if (compressed == null || compressed.Length == 0)
                {
                    return;
                }
                
                debug(compressed.Length);
                
                networkObject.SendRPCUnreliable(RPC_SEND_VOICE_DATA, RPCRecievers.Others, compressed);
                
                debug("sent rpc");
            }
            catch (Exception e)
            {
                lastError = e.Message;
                Plugin.Logger.LogError($"Error reading / sending voice data: {e.Message} {e.StackTrace}");
            }
        }
        
        private void SetSteamVoiceRecord()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                isMuted = !isMuted;
            }

            if (EnableVoiceChat && !isMuted)
            {
                if (Mode == VoiceChatMode.Off)
                {
                    SteamUser.VoiceRecord = false;
                }
                else if (Mode == VoiceChatMode.PushToTalk)
                {
                    SteamUser.VoiceRecord = Input.GetKey(KeyCode.T);
                }
                else if (Mode == VoiceChatMode.AlwaysOn)
                {
                    SteamUser.VoiceRecord = true;
                }
            }
            else
            {
                SteamUser.VoiceRecord = false;
            }
        }

        private void RpcReceiveVoiceData(HawkNetReader reader, HawkRPCInfo info)
        {
            debug("RpcReceiveVoiceData");

            try
            {
                hasReceivedData = true;

                var compressed = reader.ReadBytesAndSize().ToArray();
                debug(compressed);

                compressedStream.SetLength(0);
                compressedStream.Write(compressed, 0, compressed.Length);
                compressedStream.Position = 0;
                
                lastCompressedSize = compressed.Length;

                decompressedStream.SetLength(0);
                var written = SteamUser.DecompressVoice(compressed, decompressedStream);
                debug(written);

                lastDecompressedSize = written;

                if (written <= 0)
                {
                    return;
                }

                decompressedStream.Position = 0;
                var buffer = decompressedStream.ToArray();
                debug(buffer.Length);

                for (var i = 0; i < buffer.Length; i += 2)
                {
                    var sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    var f = sample / 32768f;
                    audioQueue.Enqueue(f);
                    totalSamplesQueued++;
                }

                bufferCountLastFrame = audioQueue.Count;
                debug("finished receive voice data");
            }
            catch (Exception e)
            {
                lastError = e.Message;
                Plugin.Logger.LogWarning($"Error receiving / decompressing voice data: {e.Message} {e.StackTrace}");
            }
        }

        private void OnAudioRead(float[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (audioQueue.TryDequeue(out var sample))
                {
                    var amplified = sample * Volume;
                    data[i] = Mathf.Clamp(amplified, -1f, 1f);
                    totalSamplesDequeued++;
                }
                else
                {
                    data[i] = 0f;
                }
            }
        }

        private void OnAudioSetPosition(int newPosition) { }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            running = false;
            SteamUser.VoiceRecord = false;
        }

        private int lastCompressedSize = 0;
        private int lastDecompressedSize = 0;
        private bool hasReceivedData = false;

        private void OnGUI()
        {
            if (networkObject == null || !networkObject.IsOwner())
                return;

            return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 650), "WL Voice Chat Debug", GUI.skin.window);

            GUILayout.Label($"[SteamUser.HasVoiceData]: {SteamUser.HasVoiceData}");
            GUILayout.Label($"[SteamUser.VoiceRecord]: {SteamUser.VoiceRecord}");
            GUILayout.Label($"Muted: {isMuted}");
            GUILayout.Label($"VoiceChat Enabled: {EnableVoiceChat}");
            GUILayout.Label($"Mode: {Mode}");
            GUILayout.Space(10);

            GUILayout.Label($"[AudioSource] IsPlaying: {audioSource.isPlaying}");
            GUILayout.Label($"Volume: {audioSource.volume:0.00}, SpatialBlend: {audioSource.spatialBlend:0.00}");
            GUILayout.Label($"AudioSource Pos: {transform.position}");
            GUILayout.Space(10);

            GUILayout.Label($"Buffer Size: {audioQueue.Count} (Last Frame: {bufferCountLastFrame})");
            GUILayout.Label($"Samples Queued: {totalSamplesQueued}");
            GUILayout.Label($"Samples Dequeued: {totalSamplesDequeued}");
            GUILayout.Label($"Last Compressed Size: {lastCompressedSize} bytes");
            GUILayout.Label($"Last Decompressed Size: {lastDecompressedSize} bytes");
            GUILayout.Label($"Received Audio: {hasReceivedData}");

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(lastError))
            {
                GUILayout.Label($"<color=red>Last Error: {lastError}</color>");
            }

            GUILayout.EndArea();
        }

        private static void debug(string message)
        {
            //Plugin.Logger.LogDebug(message);
        }

        private static void debug(object message)
        {
            debug(message?.ToString());
        }
    }
}
