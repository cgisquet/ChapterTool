﻿// ****************************************************************************
//
// Copyright (C) 2009-2015 Kurtnoise (kurtnoise@free.fr)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// ****************************************************************************
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace ChapterTool.Util
{
    public class MplsData
    {
        /// <summary>include all chapters in mpls divisionally</summary>
        public List<Clip> ChapterClips   { get; } = new List<Clip>();
        /// <summary>include all time code in mpls</summary>
        public List<int> EntireTimeStamp { get; } = new List<int>();

        public override string ToString() => $"MPLS: {ChapterClips.Count} Viedo Clips, {ChapterClips.Sum(item=>item.TimeStamp.Count)} Time Stamps";

        private readonly byte[] _data;

        public delegate void LogEventHandler(string message);

        public static event LogEventHandler OnLog;

        public MplsData(string path)
        {
            _data = File.ReadAllBytes(path);
            ParseMpls();
        }

        private void ParseMpls()
        {
            int playlistMarkSectionStartAddress, playItemNumber, playItemEntries;
            ParseHeader(out playlistMarkSectionStartAddress, out playItemNumber, out playItemEntries);
            for (var playItemOrder = 0; playItemOrder < playItemNumber; playItemOrder++)
            {
                int lengthOfPlayItem, itemStartAdress, streamCount;
                ParsePlayItem(playItemEntries, out lengthOfPlayItem, out itemStartAdress, out streamCount);
                for (int streamOrder = 0; streamOrder < streamCount; streamOrder++)
                {
                    ParseStream(itemStartAdress, streamOrder, playItemOrder);
                }
                playItemEntries += lengthOfPlayItem + 2;//for that not counting the two length bytes themselves.
            }
            ParsePlaylistMark(playlistMarkSectionStartAddress);
        }


        private void ParseHeader(out int playlistMarkSectionStartAddress, out int playItemNumber, out int playItemEntries)
        {
            string fileType = Encoding.ASCII.GetString(_data, 0, 8);
            if ((fileType != "MPLS0100" && fileType != "MPLS0200") /*|| _data[45] != 1*/)
            {
                throw new Exception($"This Playlist has an unknown file type {fileType}.");
            }
            int playlistSectionStartAddress = Byte2Int32(_data, 0x08);
            playlistMarkSectionStartAddress = Byte2Int32(_data, 0x0c);
            playItemNumber                  = Byte2Int16(_data, playlistSectionStartAddress + 0x06);
            playItemEntries                 = playlistSectionStartAddress + 0x0a;
        }

        private void ParsePlayItem(int playItemEntries, out int lengthOfPlayItem, out int itemStartAdress, out int streamCount)
        {
            lengthOfPlayItem     = Byte2Int16(_data, playItemEntries);
            var bytes            = new byte[lengthOfPlayItem + 2];
            Array.Copy(_data, playItemEntries, bytes, 0, lengthOfPlayItem);
            Clip streamClip      = new Clip
            {
                TimeIn  = Byte2Int32(bytes, 0x0e),
                TimeOut = Byte2Int32(bytes, 0x12)
            };
            streamClip.Length          = streamClip.TimeOut - streamClip.TimeIn;
            streamClip.RelativeTimeIn  = ChapterClips.Sum(clip => clip.Length);
            //streamClip.RelativeTimeOut = streamClip.RelativeTimeIn + streamClip.Length;

            itemStartAdress            = playItemEntries + 0x32;
            streamCount                = bytes[0x23] >> 4;
            int isMultiAngle           = (bytes[0x0c] >> 4) & 0x01;
            StringBuilder nameBuilder  = new StringBuilder(Encoding.ASCII.GetString(bytes, 0x02, 0x05));

            if (isMultiAngle == 1)  //skip multi-angle
            {
                int numberOfAngles = bytes[0x22];
                for (int i = 1; i < numberOfAngles; i++)
                {
                    nameBuilder.Append("&" + Encoding.ASCII.GetString(bytes, 0x24 + (i - 1) * 0x0a, 0x05));
                }
                itemStartAdress = playItemEntries + 0x02 + (numberOfAngles - 1) * 0x0a;
                OnLog?.Invoke($"Chapter with {numberOfAngles} Angle, file name: {nameBuilder}");
            }
            streamClip.Name = nameBuilder.ToString();
            ChapterClips.Add(streamClip);
        }

        private void ParseStream(int itemStartAdress, int streamOrder, int playItemOrder)
        {
            var stream = new byte[16];
            Array.Copy(_data, itemStartAdress + streamOrder * 16, stream, 0, 16);
            if (0x01 != stream[01]) return; //make sure this stream is Play Item
            int streamCodingType = stream[0x0b];
            if (0x1b != streamCodingType && // AVC
                0x02 != streamCodingType && // MPEG-I/II
                0xea != streamCodingType)   // VC-1
                return;
            ChapterClips[playItemOrder].Fps = stream[0x0c] & 0xf;//last 4 bits is the fps
        }
        private void ParsePlaylistMark(int playlistMarkSectionStartAddress)
        {
            int playlistMarkNumber  = Byte2Int16(_data, playlistMarkSectionStartAddress + 0x04);
            int playlistMarkEntries = playlistMarkSectionStartAddress + 0x06;
            var bytelist = new byte[14];    // eg. 0001 yyyy xxxxxxxx FFFF 000000
                                            // 00       mark_id
                                            // 01       mark_type
                                            // 02 - 03  play_item_ref
                                            // 04 - 07  time
                                            // 08 - 09  entry_es_pid
                                            // 10 - 13  duration
            for (var mark = 0; mark < playlistMarkNumber; ++mark)
            {
                Array.Copy(_data, playlistMarkEntries + mark * 14, bytelist, 0, 14);
                if (0x01 != bytelist[1]) continue;//make sure the playlist mark type is an entry mark
                int streamFileIndex = Byte2Int16(bytelist, 0x02);
                Clip streamClip     = ChapterClips[streamFileIndex];
                int timeStamp       = Byte2Int32(bytelist, 0x04);
                int relativeSeconds = timeStamp - streamClip.TimeIn + streamClip.RelativeTimeIn;
                streamClip.TimeStamp.Add(timeStamp);
                EntireTimeStamp.Add(relativeSeconds);
            }
        }

        private static short Byte2Int16(IReadOnlyList<byte> bytes, int index, bool bigEndian = true)
        {
            return (short)(bigEndian ? (bytes[index] << 8) | bytes[index + 1] :
                                       (bytes[index + 1] << 8) | bytes[index]);
        }

        private static int Byte2Int32(IReadOnlyList<byte> bytes, int index, bool bigEndian = true)
        {
            return bigEndian ? (bytes[index] << 24) | (bytes[index + 1] << 16) | (bytes[index + 2] << 8) | bytes[index + 3]:
                               (bytes[index + 3] << 24) | (bytes[index + 2] << 16) | (bytes[index + 1] << 8) | bytes[index];
        }

        /// <summary>
        /// 将 pts 值转换为TimeSpan对象
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentException"><paramref name="pts"/> 值小于 0。</exception>
        public static TimeSpan Pts2Time(int pts)
        {
            if (pts < 0)
            {
                throw new ArgumentOutOfRangeException($"InvalidArgument=\"{pts}\"的值对于{nameof(pts)}无效");
            }
            decimal total = pts / 45000M;
            decimal secondPart = Math.Floor(total);
            decimal millisecondPart = Math.Round((total - secondPart) * 1000M, MidpointRounding.AwayFromZero);
            return new TimeSpan(0, 0, 0, (int)secondPart, (int)millisecondPart);
        }

        private readonly List<decimal> _frameRate = new List<decimal> { 0M, 24000M / 1001, 24M, 25M, 30000M / 1001, 50M, 60000M / 1001 };

        public ChapterInfo ToChapterInfo(int index, bool combineChapter)
        {
            if (index > ChapterClips.Count && !combineChapter)
            {
                throw new IndexOutOfRangeException("Index of Video Clip out of range");
            }
            ChapterInfo info = new ChapterInfo
            {
                SourceType = "MPLS",
                SourceName = combineChapter ? "FULL Chapter" : ChapterClips[index].Name,
                Duration   = Pts2Time(combineChapter
                    ? EntireTimeStamp.Last() - EntireTimeStamp.First()
                    : ChapterClips[index].TimeOut - ChapterClips[index].TimeIn),
                FramesPerSecond = (double) _frameRate[ChapterClips.First().Fps]
            };

            var current = combineChapter ? EntireTimeStamp : ChapterClips[index].TimeStamp;
            if (current.Count < 2) return info;
            int offset  = current.First();
            /**
             *the begin time stamp of the chapter isn't the begin of the video
             *eg: Hidan no Aria AA, There are 24 black frames at the begining of each even episode
             *    Which results that the first time stamp should be the 00:00:01.001
             */
            if (!combineChapter && ChapterClips[index].TimeIn != offset)
            {
                offset = ChapterClips[index].TimeIn;
                OnLog?.Invoke($"first time stamp: {current.First()}, Time in: {offset}");
            }
            var name = new ChapterName();
            info.Chapters = current.Select(item => new Chapter
            {
                Time   = Pts2Time(item - offset),
                Number = name.Index,
                Name   = name.Get()
            }).ToList();
            return info;
        }
    }
}
