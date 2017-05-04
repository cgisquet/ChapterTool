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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChapterTool.Util.ChapterData
{
    public static class IfoData
    {
        public static IEnumerable<ChapterInfo> GetStreams(string ifoFile)
        {
            var pgcCount = IfoParser.GetPGCnb(ifoFile);
            for (var i = 1; i <= pgcCount; i++)
            {
                yield return GetChapterInfo(ifoFile, i);
            }
        }

        private static ChapterInfo GetChapterInfo(string location, int titleSetNum)
        {
            var titleRegex   = new Regex(@"^VTS_(\d+)_0\.IFO", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var result       = titleRegex.Match(location);
            if (result.Success) titleSetNum = int.Parse(result.Groups[1].Value);

            var pgc = new ChapterInfo
            {
                SourceType  = "DVD",
            };
            var fileName = Path.GetFileNameWithoutExtension(location);
            Debug.Assert(fileName != null);
            if (fileName.Count(ch => ch == '_') == 2)
            {
                var barIndex = fileName.LastIndexOf('_');
                pgc.Title = pgc.SourceName = $"{fileName.Substring(0, barIndex)}_{titleSetNum}";
            }

            pgc.Chapters        = GetChapters(location, titleSetNum, out TimeSpan duration, out double fps);
            pgc.Duration        = duration;
            pgc.FramesPerSecond = fps;

            if (pgc.Duration.TotalSeconds < 10)
                pgc = null;

            return pgc;
        }

        private static List<Chapter> GetChapters(string ifoFile, int programChain, out TimeSpan duration, out double fps)
        {
            var chapters = new List<Chapter>();
            duration     = TimeSpan.Zero;
            fps          = 0;

            var stream = new FileStream(ifoFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            var pcgItPosition = stream.GetPCGIP_Position();
            var programChainPrograms = -1;
            var programTime   = TimeSpan.Zero;
            double fpsLocal;
            if (programChain >= 0)
            {
                var chainOffset      = stream.GetChainOffset(pcgItPosition, programChain);
                programTime          = stream.ReadTimeSpan(pcgItPosition, chainOffset, out fpsLocal) ?? TimeSpan.Zero;
                programChainPrograms = stream.GetNumberOfPrograms(pcgItPosition, chainOffset);
            }
            else
            {
                var programChains = stream.GetProgramChains(pcgItPosition);
                for (var curChain = 1; curChain <= programChains; curChain++)
                {
                    var chainOffset = stream.GetChainOffset(pcgItPosition, curChain);
                    var time  = stream.ReadTimeSpan(pcgItPosition, chainOffset, out fpsLocal);
                    if (time == null) break;

                    if (time.Value <= programTime) continue;
                    programChain         = curChain;
                    programChainPrograms = stream.GetNumberOfPrograms(pcgItPosition, chainOffset);
                    programTime          = time.Value;
                }
            }
            if (programChain < 0) return null;

            chapters.Add(new Chapter { Name = "Chapter 01" ,Time = TimeSpan.Zero});

            var longestChainOffset   = stream.GetChainOffset(pcgItPosition, programChain);
            int programMapOffset     = IfoParser.ToInt16(stream.GetFileBlock((pcgItPosition + longestChainOffset) + 230, 2));
            int cellTableOffset      = IfoParser.ToInt16(stream.GetFileBlock((pcgItPosition + longestChainOffset) + 0xE8, 2));
            for (var currentProgram  = 0; currentProgram < programChainPrograms; ++currentProgram)
            {
                int entryCell        = stream.GetFileBlock(((pcgItPosition + longestChainOffset) + programMapOffset) + currentProgram, 1)[0];
                var exitCell         = entryCell;
                if (currentProgram < (programChainPrograms - 1))
                    exitCell         = stream.GetFileBlock(((pcgItPosition + longestChainOffset) + programMapOffset) + (currentProgram + 1), 1)[0] - 1;

                var totalTime = TimeSpan.Zero;
                for (var currentCell = entryCell; currentCell <= exitCell; currentCell++)
                {
                    var cellStart = cellTableOffset + ((currentCell - 1) * 0x18);
                    var bytes     = stream.GetFileBlock((pcgItPosition + longestChainOffset) + cellStart, 4);
                    var cellType  = bytes[0] >> 6;
                    if (cellType == 0x00 || cellType == 0x01)
                    {
                        bytes = stream.GetFileBlock(((pcgItPosition + longestChainOffset) + cellStart) + 4, 4);
                        var time = IfoParser.ReadTimeSpan(bytes, out fps) ?? TimeSpan.Zero;
                        totalTime    += time;
                    }
                }

                //add a constant amount of time for each chapter?
                //totalTime += TimeSpan.FromMilliseconds(fps != 0 ? (double)1000 / fps / 8D : 0);

                duration += totalTime;
                if (currentProgram + 1 < programChainPrograms)
                    chapters.Add(new Chapter { Name = $"Chapter {currentProgram + 2:D2}", Time = duration });
            }
            stream.Dispose();
            return chapters;
        }
    }
}
