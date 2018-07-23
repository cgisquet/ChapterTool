﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using ChapterTool.Util.ChapterData;
using FluentAssertions;

namespace ChapterTool.Util.Tests
{
    [TestClass()]
    public class IfoDataTests
    {
        [TestMethod()]
        public void IfoDataTest()
        {
            string path = @"..\..\[ifo_Sample]\VTS_05_0.IFO";
            if (!File.Exists(path)) path = @"..\" + path;
            var result = IfoData.GetStreams(path).ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.IsTrue(result[0].Chapters.Count == 7);

            var expectResult = new[]
            {
                new { Name = "Chapter 01", Time = "00:00:00.000" },
                new { Name = "Chapter 02", Time = "00:17:43.562" },
                new { Name = "Chapter 03", Time = "00:37:17.001" },
                new { Name = "Chapter 04", Time = "00:56:27.551" },
                new { Name = "Chapter 05", Time = "01:12:41.057" },
                new { Name = "Chapter 06", Time = "01:32:31.813" },
                new { Name = "Chapter 07", Time = "01:49:12.679" }
            };

            Console.WriteLine(result[0]);
            int index = 0;
            foreach (var chapter in result[0].Chapters)
            {
                Console.WriteLine(chapter);
                expectResult[index].Name.Should().Be(chapter.Name);
                expectResult[index].Time.Should().Be(chapter.Time.Time2String());
                ++index;
            }
        }
    }
}
