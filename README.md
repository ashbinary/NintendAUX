# NintendAUX
<img src="https://raw.githubusercontent.com/ashbinary/NintendAUX/refs/heads/main/assets/logo.png" width="150" title="NintendAUX logo"/>

**NintendAUX** (pronounced /nɪnˈtɛndoʊ/) is a GUI Avalonia-based editor for various audio formats used by Nintendo EPD, specifically **BARS (Binary Audio Resource)** files and **BWAV (Binary Wave)** files. AMTA (Audio Metadata) files are partially supported aswell, but support past what exists at the moment is not planned for the future due to the complexity of the filetype.

## Current Support
|Game|Importing|Exporting|Editing|
|:-:|:-:|:-:|:-:|
|Splatoon 3|✓|✓ (mostly)|✓|
|Tears of the Kingdom|X|X|X|
|Super Mario Wonder|X|X|X|
|Earlier BARS Versions|X|X|X|

## Preview
<img src="https://raw.githubusercontent.com/ashbinary/NintendAUX/refs/heads/main/assets/preview.png" title="NintendAUX in action"/>

## Credits
**VGAudio** - Original implementation of the [ADPCM decoder](https://github.com/Thealexbarney/VGAudio/blob/master/src/VGAudio/Codecs/GcAdpcm/GcAdpcmDecoder.cs) used in the tool.

**Watertoon** - Documentation of the file formats used in NintendAUX.

**AeonSake** - Creator of NintendAUX's [FileWriter, FileReader, and SARC library](https://gitlab.com/AeonSake/nintendo-tools)

**NachoL** - Helped with beta-testing early versions of NintendAUX and finding resources.
