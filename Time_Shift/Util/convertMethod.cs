﻿// ****************************************************************************
//
// Copyright (C) 2014-2015 TautCony (TautCony@vcb-s.com)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
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
using System.Xml;
using System.Text;
using System.Linq;
using System.Drawing;
using ChapterTool.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ChapterTool.Util
{
    internal static class ConvertMethod
    {
        //format a pts as hh:mm:ss.sss
        public static string Time2String(int pts)
        {
            decimal total = pts / 45000M;
            return Time2String(total);
        }
        public static string Time2String(decimal second)
        {
            decimal secondPart = Math.Floor(second);
            decimal millisecondPart = Math.Round((second - secondPart) * 1000M);
            return Time2String(new TimeSpan(0, 0, 0, (int)secondPart, (int)millisecondPart));
        }


        public static string Time2String(TimeSpan temp)
        {
            return $"{temp.Hours:D2}:{temp.Minutes:D2}:{temp.Seconds:D2}.{temp.Milliseconds:D3}";
        }

        public static string Time2String(Chapter item, TimeSpan offset, bool mul1K1)
        {
            return
                Time2String(mul1K1
                    ? Pts2Time((int) ((decimal) (item.Time + offset).TotalSeconds*45045M))
                    : item.Time + offset);
        }

        public static readonly Regex RTimeFormat = new Regex(@"(?<Hour>\d+):(?<Minute>\d+):(?<Second>\d+)\.(?<Millisecond>\d{3})");

        public static TimeSpan String2Time(string input)
        {
            if (string.IsNullOrEmpty(input)) { return TimeSpan.Zero; }
            var        temp = RTimeFormat.Match(input);
            int        hour = int.Parse(temp.Groups["Hour"].Value);
            int      minute = int.Parse(temp.Groups["Minute"].Value);
            int      second = int.Parse(temp.Groups["Second"].Value);
            int millisecond = int.Parse(temp.Groups["Millisecond"].Value);
            return new TimeSpan(0, hour, minute, second, millisecond);
        }

        public static TimeSpan Pts2Time(int pts)
        {
            decimal total = pts / 45000M;
            decimal secondPart = Math.Floor(total);
            decimal millisecondPart = Math.Round((total - secondPart) * 1000M);
            return new TimeSpan(0, 0, 0, (int)secondPart, (int)millisecondPart);
        }



        public static Point String2Point(string input)
        {
            var rpos = new Regex(@"{X=(?<x>.+),Y=(?<y>.+)}");
            if (string.IsNullOrEmpty(input)) { return new Point(-32000, -32000); }
            var temp = rpos.Match(input);
            int x = int.Parse(temp.Groups["x"].Value);
            int y = int.Parse(temp.Groups["y"].Value);
            return new Point(x, y);
        }

        public static int ConvertFr2Index(double frame)
        {
            decimal[] frameRate = { 0M, 24000M / 1001, 24000M / 1000,
                                        25000M / 1000, 30000M / 1001,
                                        50000M / 1000, 60000M / 1001 };
            var result = Enumerable.Range(0, 7).Where(index => (Math.Abs(frame - (double)frameRate[index])) < 1e-5);
            return result.First();
        }

        public static string GetUTF8String(byte[] buffer)
        {
            if (buffer == null) return null;
            if (buffer.Length <= 3) return Encoding.UTF8.GetString(buffer);
            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
            {
                return new UTF8Encoding(false).GetString(buffer, 3, buffer.Length - 3);
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public static List<ChapterInfo>  PraseXml(XmlDocument doc)
        {
            List<ChapterInfo> result = new List<ChapterInfo>();
            XmlElement root = doc.DocumentElement;
            if (root == null) return result;
            if (root.Name != "Chapters")
            {
                throw new Exception($"Invalid Xml file.\nroot node Name: {root.Name}");
            }
            foreach (XmlNode editionEntry in root.ChildNodes)//Get Entrance for each chapter
            {
                ChapterInfo buff = new ChapterInfo {SourceType = "XML"};
                int index = 0;
                foreach (XmlNode editionEntryChildNode in ((XmlElement)editionEntry).ChildNodes)//Get all the child nodes in current chapter
                {
                    if (editionEntryChildNode.Name != "ChapterAtom") { continue; }
                    Chapter temp  = new Chapter();
                    Chapter temp2 = new Chapter();
                    foreach (XmlNode chapterAtomChildNode in ((XmlElement)editionEntryChildNode).ChildNodes)//Get detail info for current chapter node
                    {
                        switch (chapterAtomChildNode.Name)
                        {
                            case "ChapterTimeStart":
                                temp.Time = String2Time(RTimeFormat.Match(chapterAtomChildNode.InnerText).Value);
                                break;
                            case "ChapterTimeEnd":
                                temp2.Time = String2Time(RTimeFormat.Match(chapterAtomChildNode.InnerText).Value);
                                break;
                            case "ChapterDisplay":
                                temp.Name  = ((XmlElement) chapterAtomChildNode).ChildNodes.Item(0)?.InnerText;
                                temp2.Name = temp.Name;
                                break;
                        }
                    }
                    temp.Number = ++index;          // Chapter node for ChapterTimeStart
                    buff.Chapters.Add(temp);
                    if (!(temp2.Time.TotalSeconds > 1e-5)) continue;
                    temp2.Number = index;           // Chapter node for ChapterTimeEnd
                    buff.Chapters.Add(temp2);
                }

                for (int i = 0; i < buff.Chapters.Count - 1; i++)
                {
                    if (buff.Chapters[i].Time == buff.Chapters[i+1].Time)
                    {
                        buff.Chapters.Remove(buff.Chapters[i--]);
                    }
                }
                //buff.Chapters = buff.Chapters.Distinct().ToList();
                result.Add(buff);
            }
            return result;
        }

        private const string ColorProfile = "color-config.json";

        public static void SaveColor(List<Color> colorList)
        {
            StringBuilder json = new StringBuilder("[");
            colorList.ForEach(item => json.AppendFormat($"\"#{item.R:X2}{item.G:X2}{item.B:X2}\","));
            json[json.Length - 1] = ']';
            File.WriteAllText(ColorProfile, json.ToString());
        }

        public static void LoadColor(Form1 window)
        {
            if (!File.Exists(ColorProfile)) return;
            string json = File.ReadAllText(ColorProfile);
            Regex rcolor = new Regex("\"(?<hex>.+?)\"");
            var matchesOfJson = rcolor.Matches(json);
            if (matchesOfJson.Count < 6) { return; }
            window.BackChange     = ColorTranslator.FromHtml(matchesOfJson[0].Groups["hex"].Value);
            window.TextBack       = ColorTranslator.FromHtml(matchesOfJson[1].Groups["hex"].Value);
            window.MouseOverColor = ColorTranslator.FromHtml(matchesOfJson[2].Groups["hex"].Value);
            window.MouseDownColor = ColorTranslator.FromHtml(matchesOfJson[3].Groups["hex"].Value);
            window.BordBackColor  = ColorTranslator.FromHtml(matchesOfJson[4].Groups["hex"].Value);
            window.TextFrontColor = ColorTranslator.FromHtml(matchesOfJson[5].Groups["hex"].Value);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
        //1 = normal (green);
        //2 = error (red);
        //3 = warning (yellow).
        //
        public static void SetState(this System.Windows.Forms.ProgressBar pBar, int state)
        {
            SendMessage(pBar.Handle, 1040, (IntPtr)state, IntPtr.Zero);
        }

        //from http://www.sukitech.com/?p=1080
        //尋找視窗
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //將視窗移動到最上層
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

    }
}
