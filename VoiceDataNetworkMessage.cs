using System;
using HawkNetworking;

public class VoiceDataNetworkMessage : IHawkMessage
{
    public int bytesWritten;
    public byte[] compressed;

    public void Serialize(HawkNetWriter writer)
    {
        writer.Write(bytesWritten);
        writer.Write(compressed.Length);

        foreach (var _byte in compressed)
        {
            writer.Write(_byte);
        }
    }

    public void Deserialize(HawkNetReader reader)
    {
        bytesWritten = reader.ReadInt32();
        var bytesToRead = reader.ReadInt32();
        var bytesBuffer = new byte[bytesToRead];
        
        int bytesRead;
        
        for (bytesRead = 0; bytesRead < bytesToRead; bytesRead++)
        {
            try
            {
                bytesBuffer[bytesRead] = reader.ReadByte();
            }
            catch (IndexOutOfRangeException)
            {
                break;
            }
        }

        compressed = new byte[bytesRead];
        
        for (var i = 0; i < bytesRead; ++i)
        {
            compressed[i] = bytesBuffer[i];
        }
    }
}