# ChapterTool [![Build status](https://ci.appveyor.com/api/projects/status/rtc76h5ulveafj5f?svg=true)](https://ci.appveyor.com/project/tautcony/chaptertool)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Ftautcony%2FChapterTool.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Ftautcony%2FChapterTool?ref=badge_shield)

- ChapterTool is made for extracting chapter from various types of files and edit it.

## Feature

- Extract chapter file from various types of file
- Freely time adjustment(expression in Infix notation or Reverse Polish notation)
- Move all chapter number backward optionally(for OGM format)
- Load chapter name from a text file as template
- Calculate frames from chapter time
- Supported save formats: `.txt`, `.xml`, `.qpf`, `.json`

### Supported file type

- OGM(`.txt`)
- XML(`.xml`)
- MPLS from BluRay(`.mpls`)
- IFO from DVD(`.ifo`)
- XPL from HDDVD(`.xpl`)
- CUE plain text or embedded(`.cue`, `.flac`, `.tak`)
- Matroska file(`.mkv`, `.mka`)
- Mp4 file(`.mp4`, `.m4a`, `.m4v`)
- WebVTT(`.vtt`)

## Thanks to

 - [Chapters file time Editor](https://www.nmm-hd.org/newbbs/viewtopic.php?f=16&t=24)
 - [BD Chapters MOD](https://www.nmm-hd.org/newbbs/viewtopic.php?f=16&t=517) (no longer in use)
 - [gMKVExtractGUI](http://sourceforge.net/projects/gmkvextractgui/)
 - [Chapter Grabber](http://jvance.com/pages/ChapterGrabber.xhtml)
 - [MKVToolNix](https://www.bunkus.org/videotools/mkvtoolnix/links.html)
 - [libbluray](http://www.videolan.org/developers/libbluray.html)
 - [BDedit](http://pel.hu/bdedit/)
 - [Knuckleball](https://github.com/jimevans/knuckleball)
 - [mp4v2](https://code.google.com/archive/p/mp4v2/)
 - [BluRay](https://github.com/lerks/BluRay)
 - [IfoEdit](http://www.ifoedit.com/index.html)

## Requirements

- You must have `.NET Framework 4.7` available from Windows Update.
- The matroska file's support is powerd by [`MKVToolNix`](https://mkvtoolnix.download/downloads.html#windows).
- The mp4 file's support is powerd by `libmp4v2`, you need get the dll before using this feature.

## Source Code

 - [BitBucket](https://bitbucket.org/TautCony/chaptertool)
 - [GitHub](https://github.com/tautcony/ChapterTool)

 ![](https://img.shields.io/github/downloads/tautcony/chaptertool/total.svg)
 ![](https://img.shields.io/github/license/tautcony/chaptertool.svg)


[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Ftautcony%2FChapterTool.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Ftautcony%2FChapterTool?ref=badge_large)