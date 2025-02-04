using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace StutteredBars;

public class BARSParser
{
    public BARSFile ReadBARSFile(string filePath)
    {
        BARSFile fileData = new BARSFile();

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // Read the header
            byte[] headerBuffer = new byte[Marshal.SizeOf(typeof(BARSFile.BarsHeader))];
            stream.Read(headerBuffer, 0, headerBuffer.Length);

            // Convert the byte array to the BarsHeader struct
            GCHandle handle = GCHandle.Alloc(headerBuffer, GCHandleType.Pinned);
            fileData.Header = Marshal.PtrToStructure<BARSFile.BarsHeader>(handle.AddrOfPinnedObject());
            handle.Free();

            // Initialize the FileDataHash array based on the FileCount in the header
            fileData.FileDataHash = new uint[fileData.Header.FileCount];

            // Read each file data hash
            for (int i = 0; i < fileData.Header.FileCount; i++)
            {
                byte[] hashBuffer = new byte[4];
                stream.Read(hashBuffer, 0, hashBuffer.Length);
                fileData.FileDataHash[i] = BitConverter.ToUInt32(hashBuffer, 0);
            }

            fileData.EntryArray = new BARSFile.BarsEntry[fileData.Header.FileCount];

            for (int i = 0; i < fileData.Header.FileCount; i++)
            {
                byte[] hashBuffer = new byte[8];
                stream.Read(hashBuffer, 0, hashBuffer.Length);
                fileData.EntryArray[i].BamtaOffset = BitConverter.ToUInt32(hashBuffer, 0);
                fileData.EntryArray[i].BwavOffset = BitConverter.ToUInt32(hashBuffer, 4);
                Console.WriteLine($"Entry {i} @ {fileData.EntryArray[i].BamtaOffset:x8} bamta + {fileData.EntryArray[i].BwavOffset:x8} bwav");
            }

            byte[] reserveFileBuffer = new byte[4];
            stream.Read(reserveFileBuffer, 0, 4);
            fileData.ReserveData.FileCount = BitConverter.ToUInt32(reserveFileBuffer);

            fileData.ReserveData.FileHashes = new uint[fileData.ReserveData.FileCount];

            for (int i = 0; i < fileData.ReserveData.FileCount; i++)
            {
                byte[] reserveHash = new byte[4];
                stream.Read(reserveHash, 0, reserveHash.Length);
                fileData.ReserveData.FileHashes[i] = BitConverter.ToUInt32(reserveHash);
            }

            fileData.AmtaList = new BARSFile.AmtaDetail[fileData.EntryArray.Length];
            fileData.AmtaData = new BARSFile.Amta[fileData.EntryArray.Length];
            fileData.BwavList = new BARSFile.Bwav[fileData.EntryArray.Length];

            // BWAVParser bwavParser = new BWAVParser();

            for (int i = 0; i < fileData.EntryArray.Length; i++)
            {
                stream.Seek(fileData.EntryArray[i].BamtaOffset, SeekOrigin.Begin);
                byte[] hashBuffer = new byte[Marshal.SizeOf<BARSFile.AmtaDetail>()];
                stream.ReadExactly(hashBuffer);
                handle = GCHandle.Alloc(hashBuffer, GCHandleType.Pinned);
                fileData.AmtaList[i] = Marshal.PtrToStructure<BARSFile.AmtaDetail>(handle.AddrOfPinnedObject());
                handle.Free();

                stream.Seek(fileData.EntryArray[i].BamtaOffset + 36 /* Path offset*/ + fileData.AmtaList[i].PathOffset, SeekOrigin.Begin);
                // Read the null-terminated string for the path
                List<byte> pathBytes = new();
                byte b;
                while ((b = (byte)stream.ReadByte()) != 0)
                    pathBytes.Add(b);
                fileData.AmtaList[i].Path = System.Text.Encoding.UTF8.GetString(pathBytes.ToArray());

                int amtaBufferSize = 0;
                if (i == fileData.EntryArray.Length - 1)
                    amtaBufferSize = (int)(fileData.EntryArray[0].BwavOffset) - (int)(fileData.EntryArray[i].BamtaOffset);
                else
                    amtaBufferSize = (int)(fileData.EntryArray[i + 1].BamtaOffset) - (int)(fileData.EntryArray[i].BamtaOffset);

                stream.Seek(fileData.EntryArray[i].BamtaOffset, SeekOrigin.Begin);
                byte[] amtaFullBuffer = new byte[amtaBufferSize];
                stream.ReadExactly(amtaFullBuffer);
                fileData.AmtaData[i].Data = amtaFullBuffer;

                stream.Seek(fileData.EntryArray[i].BwavOffset, SeekOrigin.Begin);

                int bwavBufferSize;
                if (i == fileData.EntryArray.Length - 1)
                    bwavBufferSize = (int)fileData.Header.FileSize - (int)fileData.EntryArray[i].BwavOffset;
                else
                    bwavBufferSize = (int)fileData.EntryArray[i + 1].BwavOffset - (int)fileData.EntryArray[i].BwavOffset;
                
                if (bwavBufferSize < 0) // Dupe key
                {
                    stream.Seek(fileData.EntryArray[i - 1].BwavOffset, SeekOrigin.Begin);
                    bwavBufferSize = (int)fileData.EntryArray[i].BwavOffset - (int)fileData.EntryArray[i - 1].BwavOffset;
                }
                byte[] bwavBuffer = new byte[bwavBufferSize];
                stream.ReadExactly(bwavBuffer);
                fileData.BwavList[i].Data = bwavBuffer;

            }

        }

        return fileData;
    }
}