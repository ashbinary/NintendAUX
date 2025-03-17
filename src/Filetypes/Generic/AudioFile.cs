using System;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Services;
using NintendAUX.ViewModels;

namespace NintendAUX.Filetypes.Generic;

/// <summary>
///     Represents either a BWAVFile or a BARSFile
/// </summary>
public class AudioFile
{
    private object _file;

    public AudioFile(BarsFile barsFile)
    {
        _file = barsFile;
        Type = InputFileType.Bars;
    }

    public AudioFile(BwavFile bwavFile)
    {
        _file = bwavFile;
        Type = InputFileType.Bwav;
    }

    public InputFileType Type { get; }

    public bool IsBarsFile => Type == InputFileType.Bars;
    public bool IsBwavFile => Type == InputFileType.Bwav;

    public BarsFile AsBarsFile()
    {
        if (!IsBarsFile)
            throw new InvalidOperationException("This AudioFile is not a BARSFile");

        return (BarsFile)_file;
    }

    public BwavFile AsBwavFile()
    {
        if (!IsBwavFile)
            throw new InvalidOperationException("This AudioFile is not a BWAVFile");

        return (BwavFile)_file;
    }

    public void RemoveEntryAt(int index)
    {
        if (!IsBarsFile)
            throw new InvalidOperationException("This AudioFile is not a BARSFile");

        var barsFile = (BarsFile)_file;
        barsFile.EntryArray.RemoveAt(index);
        _file = barsFile;
    }

    public void UpdateBwavAt(int index, BwavFile newBwav)
    {
        if (!IsBarsFile)
            throw new InvalidOperationException("This AudioFile is not a BARSFile");

        var barsFile = (BarsFile)_file;
        var entry = barsFile.EntryArray[index];
        entry.Bwav = newBwav;
        barsFile.EntryArray[index] = entry;
        _file = barsFile;
        
        NodeService.UpdateNodeArray();
    }

    public void UpdateBametaAt(int index, AmtaFile newBameta)
    {
        if (!IsBarsFile)
            throw new InvalidOperationException("This AudioFile is not a BARSFile");

        var barsFile = (BarsFile)_file;
        var entry = barsFile.EntryArray[index];
        entry.Bamta = newBameta;
        barsFile.EntryArray[index] = entry;
        _file = barsFile;
        
        NodeService.UpdateNodeArray();
    }

    public byte[] Save()
    {
        return Type switch
        {
            InputFileType.Bars => BarsFile.SoftSave(AsBarsFile()),
            InputFileType.Bwav => BwavFile.Save(AsBwavFile())
        };
    }

    public uint GetMagic()
    {
        return Type switch
        {
            InputFileType.Bars => AsBarsFile().Header.Magic,
            InputFileType.Bwav => AsBwavFile().Header.Magic
        };
    }

    // Implicit conversion operators for convenience
    public static implicit operator AudioFile(BarsFile barsFile)
    {
        return new AudioFile(barsFile);
    }

    public static implicit operator AudioFile(BwavFile bwavFile)
    {
        return new AudioFile(bwavFile);
    }
}