using System;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using static NintendAUX.Filetypes.Audio.BwavFile;

namespace NintendAUX.Utilities;

public class ConversionUtilities
{
    public static short[] DecodeChannel(ref ResBwavChannelInfo channelInfo)
    {
        short[] pcmData = new short[channelInfo.SampleCount];

        int sampleAmount = Convert.ToInt32(channelInfo.SampleCount).DivideByRoundUp(14); // samples per frame
        
        int sourceIndex = 0;
        int destinationIndex = 0;
        
        int index1 = 0;
        
        short history1 = channelInfo.LoopPointArray[0].AdpcmHistoryArray[0];
        short history2 = channelInfo.LoopPointArray[0].AdpcmHistoryArray[1];
        
        for (int index2 = 0; index2 < sampleAmount; ++index2)
        {
            byte headerByte = channelInfo.OSamples[index1++];
            
            int lowNibble = (1 << AdpcmUtilities.GetLowNibble(headerByte)) * 2048;
            int highNibble = AdpcmUtilities.GetHighNibble(headerByte); // Don't care.
            
            short coefficient1 = channelInfo.DspAdpcmCoefficients[highNibble * 2];
            short coefficient2 = channelInfo.DspAdpcmCoefficients[highNibble * 2 + 1];
            
            int sampleData = Math.Min(14, Convert.ToInt32(channelInfo.SampleCount) - sourceIndex);
            
            for (int index3 = 0; index3 < sampleData; ++index3)
            {
                int nibble = index3 % 2 == 0 ? 
                    (int) AdpcmUtilities.NibbleToSbyte[channelInfo.OSamples[index1] >> 4 & 15] : 
                    AdpcmUtilities.NibbleToSbyte[channelInfo.OSamples[index1++] & 15];
                
                int nibbleMult = lowNibble * nibble;
                
                short sample = (short)Math.Clamp(coefficient1 * history1 + coefficient2 * history2 + nibbleMult + 1024 >> 11, short.MinValue, short.MaxValue);
                
                history2 = history1;
                history1 = sample;
                
                pcmData[destinationIndex++] = sample;
                ++sourceIndex;
            }
        }
        
        return pcmData;
    } 
    
    public static WavFile CreateWavData(ref ResBwavChannelInfo channelInfo, AudioChannel[] pcmData)
    {
        var wavFile = new WavFile
        {
            Header = new WavFile.WavHeader
            {
                ChunkID = 0x46464952,    // "RIFF"
                Format = 0x45564157      // "WAVE"
            },
            Format = new WavFile.WavFormat
            {
                ChunkID = 0x20746D66,    // "fmt "
                ChunkSize = 16,          // mono audio for now
                AudioFormat = 1,         // PCM
                NumChannels = (ushort)pcmData.Length,
                SampleRate = channelInfo.SampleRate,
                BitsPerSample = 16
            },
            AudioData = pcmData
        };
        
        wavFile.Format.BlockAlign = (ushort)(wavFile.Format.NumChannels * (wavFile.Format.BitsPerSample / 8));
        wavFile.Format.ByteRate = wavFile.Format.SampleRate * wavFile.Format.BlockAlign;

        wavFile.Header.ChunkSize = (uint)(4 + (8 + wavFile.Format.ChunkSize) + (8 + wavFile.AudioData.Length));

        return wavFile;
    }
}
