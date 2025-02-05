using StutteredBars.Helpers;

namespace StutteredBars.Filetypes.AMTA;

public struct AMTATagTable
{
    public struct AMTATag
    {
        public uint TagOffset;
        public string Name;
    }

    private long FileBase;

    public uint TagCount;
    public AMTATag[] Tags;

    public AMTATagTable(ref FileReader amtaReader)
    {
        TagCount = amtaReader.ReadUInt32();  
        Tags = new AMTATag[TagCount];

        FileBase = amtaReader.Position;

        for (int i = 0; i < TagCount; i++)
        {
            Tags[i] = new AMTATag
            {
                TagOffset = amtaReader.ReadUInt32()
            };
        }

        for (int i = 0; i < TagCount; i++)
        {
            amtaReader.Position = FileBase + Tags[i].TagOffset;
            Tags[i].Name = amtaReader.ReadTerminatedString();
        }
    }
}