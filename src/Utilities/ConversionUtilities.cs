using System;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using static NintendAUX.Filetypes.Audio.BwavFile;

namespace NintendAUX.Utilities;

public class ConversionUtilities
{
    public static short[] DecodeChannel(ref ResBwavChannelInfo channelInfo)
    {
        var pcmData = new short[channelInfo.SampleCount];

        var sampleAmount = Convert.ToInt32(channelInfo.SampleCount).DivideByRoundUp(14); // samples per frame

        var sourceIndex = 0;
        var destinationIndex = 0;

        var index1 = 0;

        var history1 = channelInfo.LoopPointArray[0].AdpcmHistoryArray[0];
        var history2 = channelInfo.LoopPointArray[0].AdpcmHistoryArray[1];

        for (var index2 = 0; index2 < sampleAmount; ++index2)
        {
            var headerByte = channelInfo.OSamples[index1++];

            var lowNibble = (1 << AdpcmUtilities.GetLowNibble(headerByte)) * 2048;
            int highNibble = AdpcmUtilities.GetHighNibble(headerByte); // Don't care.

            var coefficient1 = channelInfo.DspAdpcmCoefficients[highNibble * 2];
            var coefficient2 = channelInfo.DspAdpcmCoefficients[highNibble * 2 + 1];

            var sampleData = Math.Min(14, Convert.ToInt32(channelInfo.SampleCount) - sourceIndex);

            for (var index3 = 0; index3 < sampleData; ++index3)
            {
                var nibble = index3 % 2 == 0
                    ? (int)AdpcmUtilities.NibbleToSbyte[(channelInfo.OSamples[index1] >> 4) & 15]
                    : AdpcmUtilities.NibbleToSbyte[channelInfo.OSamples[index1++] & 15];

                var nibbleMult = lowNibble * nibble;

                var sample =
                    (short)Math.Clamp((coefficient1 * history1 + coefficient2 * history2 + nibbleMult + 1024) >> 11,
                        short.MinValue, short.MaxValue);

                history2 = history1;
                history1 = sample;

                pcmData[destinationIndex++] = sample;
                ++sourceIndex;
            }
        }

        // Implement looping point and audio normalization (TODO: add removing the option to do so?)
        pcmData = Normalize(ref channelInfo, pcmData);

        return pcmData;
    }

    public static short[] Normalize(ref ResBwavChannelInfo channelInfo, short[] pcmData)
    {
        // Normally 4,294,967,295. However, ToTK has erroneous loop points in some files larger than the size of the file????
        // How does that even happen????
        if (channelInfo.LoopEnd > pcmData.Length)
            return pcmData;

        var oldLength = pcmData.Length;
        var loopLength = Convert.ToInt32(channelInfo.LoopEnd - channelInfo.LoopPointArray[0].LoopStart);
        Array.Resize(ref pcmData, pcmData.Length + loopLength);

        Array.Copy(pcmData, Convert.ToInt32(channelInfo.LoopPointArray[0].LoopStart), pcmData, oldLength, loopLength);

        Span<short> span = pcmData; // span instead of array to honor shadow (and increase performance)
        short multiplier = 2;

        for (var i = 0; i < span.Length; i++)
            span[i] *= Math.Clamp(multiplier, (short)-32768, (short)32767);

        return pcmData;
    }

    public static WavFile CreateWavData(ref ResBwavChannelInfo channelInfo, AudioChannel[] pcmData)
    {
        var wavFile = new WavFile
        {
            Header = new WavFile.WavHeader
            {
                ChunkID = 0x46464952, // "RIFF"
                Format = 0x45564157 // "WAVE"
            },
            Format = new WavFile.WavFormat
            {
                ChunkID = 0x20746D66, // "fmt "
                ChunkSize = 16, // mono audio for now
                AudioFormat = 1, // PCM
                NumChannels = (ushort)pcmData.Length,
                SampleRate = channelInfo.SampleRate,
                BitsPerSample = 16
            },
            AudioData = pcmData
        };

        wavFile.Format.BlockAlign = (ushort)(wavFile.Format.NumChannels * (wavFile.Format.BitsPerSample / 8));
        wavFile.Format.ByteRate = wavFile.Format.SampleRate * wavFile.Format.BlockAlign;

        wavFile.Header.ChunkSize = (uint)(4 + 8 + wavFile.Format.ChunkSize + (8 + wavFile.AudioData.Length));

        return wavFile;
    }
}