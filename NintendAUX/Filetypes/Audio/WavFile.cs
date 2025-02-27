using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NintendAUX.Filetypes.Generic;
using Tmds.DBus.Protocol;

namespace NintendAUX.Filetypes.Audio;

public struct WavFile
{
    public WavHeader Header;
    public WavFormat Format;
    public AudioChannel[] AudioData;

    [StructLayout(LayoutKind.Sequential)]
    public struct WavHeader
    {
        public uint ChunkID; // RIFF 
        public uint ChunkSize;
        public uint Format; // WAVE
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WavFormat
    {
        public uint ChunkID; // fmt        
        public uint ChunkSize;  // 16    
        public ushort AudioFormat;  // 1 (PCM)
        public ushort NumChannels;  
        public uint SampleRate;     // 48000
        public uint ByteRate; // (Sample Rate * BitsPerSample * Channels) / 8
        public ushort BlockAlign; // (BitsPerSample * Channels) / 8.1 - 8 bit mono2 - 8 bit stereo/16 bit mono4 - 16 bit stereo
        public ushort BitsPerSample; // 16
    }
    
    public static byte[] Write(WavFile wavData)
    {
        using MemoryStream saveStream = new();
        var wavWriter = new FileWriter(saveStream);

        // Calculate correct chunk sizes and format values
        wavData.Format.ChunkSize = 16; // fmt chunk is always 16 bytes for PCM
        wavData.Format.ByteRate = (uint)(wavData.Format.SampleRate * wavData.Format.NumChannels * (wavData.Format.BitsPerSample / 8));
        wavData.Format.BlockAlign = (ushort)(wavData.Format.NumChannels * (wavData.Format.BitsPerSample / 8));
        
        uint samplesPerChannel = (uint)wavData.AudioData[0].Data.Length;
        uint dataSize = samplesPerChannel * wavData.Format.NumChannels * sizeof(short);
        
        wavData.Header.ChunkSize = 4 + (8 + wavData.Format.ChunkSize) + (8 + dataSize);

        // Write header and format chunks
        wavWriter.Write(MemoryMarshal.AsBytes(new Span<WavHeader>(ref wavData.Header)));
        wavWriter.Write(MemoryMarshal.AsBytes(new Span<WavFormat>(ref wavData.Format)));
        
        // Write data chunk header
        wavWriter.Write(0x61746164); // "data" in hex
        wavWriter.Write(dataSize);
        
        // Write PCM data
        for (int sampleIndex = 0; sampleIndex < samplesPerChannel; sampleIndex++)
        {
            foreach (var channel in wavData.AudioData)
            {
                short mixedSample = 0;
                mixedSample += channel.Data[sampleIndex];
                mixedSample = (short)(mixedSample / wavData.AudioData.Length); // Average the samples
                wavWriter.Write(BitConverter.GetBytes(mixedSample));   
            }
        }

        return saveStream.ToArray();
    }
}