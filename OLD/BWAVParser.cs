using System.Runtime.InteropServices;

namespace StutteredBars;

public class BWAVParser
{
    public void ParseBWAVData(FileStream baseStream, out BWAVFile newBWAV)
    {
        newBWAV = new BWAVFile();

        // Read the header
        byte[] headerBuffer = new byte[Marshal.SizeOf<BWAVFile.BwavHeader>()];
        baseStream.Read(headerBuffer, 0, headerBuffer.Length);

        // Convert the byte array to the BarsHeader struct
        GCHandle handle = GCHandle.Alloc(headerBuffer, GCHandleType.Pinned);
        newBWAV.Header = Marshal.PtrToStructure<BWAVFile.BwavHeader>(handle.AddrOfPinnedObject());
        handle.Free();

        newBWAV.FileBase = baseStream.Position;

        newBWAV.ChannelInfoArray = new();

        for (int i = 0; i < newBWAV.Header.ChannelCount; i++)
        {
            byte[] channelBuffer = new byte[Marshal.SizeOf<BWAVFile.ResBwavChannelInfo>()];
            baseStream.Read(channelBuffer, 0, channelBuffer.Length);
            handle = GCHandle.Alloc(channelBuffer, GCHandleType.Pinned);
            newBWAV.ChannelInfoArray.Add(Marshal.PtrToStructure<BWAVFile.ResBwavChannelInfo>(handle.AddrOfPinnedObject()));

            for (int j = 0; j < newBWAV.ChannelInfoArray[i].LoopPointCount; j++)
            {
                baseStream.Read(new byte[12], 0, 12); // just move forward 12 idgaf
            }
        }

    }
}