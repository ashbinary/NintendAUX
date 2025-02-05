using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using StutteredBars.Helpers;

namespace StutteredBars.Filetypes.AMTA;

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

        for (int i = 0; i < MarkerCount; i++)
        {

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
            fileReader.Position = FileBase + 8 + Markers[i].NameOffset;
            Markers[i].Name = fileReader.ReadTerminatedString();
        }
    }

    public static byte[] Save(AMTAMarkerTable markerTable)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);

        amtaWriter.Write(markerTable.MarkerCount);
        foreach (AMTAMarker marker in markerTable.Markers)
        {
            amtaWriter.Write(marker.ID);
            amtaWriter.Write(marker.NameOffset);
            amtaWriter.Write(marker.Start);
            amtaWriter.Write(marker.Length);
        }

        return saveStream.ToArray();
    }
}