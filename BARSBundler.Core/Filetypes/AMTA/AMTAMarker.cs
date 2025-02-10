using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using BARSBundler.Core.Helpers;

namespace BARSBundler.Core.Filetypes.AMTA;

public struct AMTAMarkerTable
{
    public struct AMTAMarker {
        public uint ID;
        public uint NameOffset;
        public uint Start;
        public uint Length;

        public string Name;
    };

    private long FileBase;

    public uint MarkerCount;
    public AMTAMarker[] Markers;

    public AMTAMarkerTable(ref FileReader fileReader)
    {
        
        MarkerCount = fileReader.ReadUInt32();  
        Markers = new AMTAMarker[MarkerCount];

        FileBase = fileReader.Position;
        long[] markerPositions = new long[MarkerCount];

        for (int i = 0; i < MarkerCount; i++)
        {
            markerPositions[i] = fileReader.Position + 4;
            Markers[i] = new AMTAMarker
            {
                ID = fileReader.ReadUInt32(),
                NameOffset = fileReader.ReadUInt32(),
                Start = fileReader.ReadUInt32(),
                Length = fileReader.ReadUInt32()
            };
        }

        for (int i = 0; i < MarkerCount; i++)
        {
            fileReader.Position = markerPositions[i] + Markers[i].NameOffset;
            Markers[i].Name = fileReader.ReadTerminatedString();
        }
    }

    public static byte[] Save(AMTAMarkerTable markerTable)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);

        List<long> nameOffsetPos = new();

        amtaWriter.Write(markerTable.MarkerCount);
        foreach (AMTAMarker marker in markerTable.Markers)
        {
            amtaWriter.Write(marker.ID);
            nameOffsetPos.Add(amtaWriter.Position);
            amtaWriter.Write(marker.NameOffset);
            amtaWriter.Write(marker.Start);
            amtaWriter.Write(marker.Length);
        }

        for (int i = 0; i < markerTable.MarkerCount; i++)
        {
            amtaWriter.Position = nameOffsetPos[i] + markerTable.Markers[i].NameOffset;
            amtaWriter.Write(markerTable.Markers[i].Name);
        }

        return saveStream.ToArray();
    }

    public static byte[] SoftSave(AMTAMarkerTable markerTable, long pathPosition)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);
        
        List<long> NamePositions = new();
        long MarkerTablePos = amtaWriter.Position;

        amtaWriter.Position = pathPosition;
        for (int i = 0; i < markerTable.MarkerCount; i++)
        {
            NamePositions.Add(amtaWriter.Position - (MarkerTablePos + i * 16) + 4);
            amtaWriter.Write(markerTable.Markers[i].Name);
        }
        
        return saveStream.ToArray();
    }
}