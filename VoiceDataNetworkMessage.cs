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
        compressed = new byte[bytesToRead];

        for (var i = 0; i < bytesToRead; ++i)
        {
            compressed[i] = reader.ReadByte();
        }
    }
}