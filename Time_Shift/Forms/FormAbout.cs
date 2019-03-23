﻿// ****************************************************************************
//
// Copyright (C) 2014-2016 TautCony (TautCony@vcb-s.com)
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
namespace ChapterTool.Forms
{
    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using ChapterTool.Util;

    public partial class FormAbout : Form
    {
        private readonly int _poi;

        public FormAbout()
        {
            InitializeComponent();

            // this.SizeChanged += new System.EventHandler(this.Form2_SizeChanged);
            // this.BackColor = Color.DimGray; // "#252525";
            _poi = new Random().Next(1, 5);
            FormBorderStyle = FormBorderStyle.None;
            linkLabel1.Text = AssemblyProduct;
            label2.Text = $"Version {AssemblyVersion}";
            label3.Text = System.IO.File.GetLastWriteTime(Application.ExecutablePath).ToString(CultureInfo.InvariantCulture);
            notifyIcon1.Visible = false;
        }

        private static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        private static string AssemblyProduct
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                return attributes.Length == 0 ? string.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        private void Form2_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized) return;
            Hide();
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(1000, "具体作用开发中~", "现在完全没用啦", ToolTipIcon.Info);
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void CloseForm()
        {
            while (Opacity > 0)
            {
                Opacity -= 0.02;
                Thread.Sleep(20);
            }
            Logger.Log("关于窗口被关闭");
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_poi == 1) { CloseForm(); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_poi == 2) { CloseForm(); }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_poi == 3) { CloseForm(); }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (_poi == 4) { CloseForm(); }
        }

        // from http://www.sukitech.com/?p=948
        private Point _startPoint;

        private void Form2_MouseDown(object sender, MouseEventArgs e)
        {
            _startPoint = new Point(-e.X, -e.Y);

            // startPoint = new Point(-e.X + SystemInformation.FrameBorderSize.Width, -e.Y - SystemInformation.FrameBorderSize.Height);
        }

        private void Form2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var mousePos = MousePosition;
            mousePos.Offset(_startPoint.X, _startPoint.Y);
            Location = mousePos;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Thread.Sleep(20000);
            WindowState = FormWindowState.Minimized;
            Logger.Log("关于窗口被最小化");
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    System.Diagnostics.Process.Start("https://github.com/TautCony/ChapterTool");
                    break;
                case MouseButtons.Right:
                    System.Diagnostics.Process.Start("https://bitbucket.org/TautCony/chaptertool");
                    break;
                case MouseButtons.Middle:
                    System.Diagnostics.Process.Start("https://raw.githubusercontent.com/tautcony/ChapterTool/master/Time_Shift/ChangeLog.txt");
                    break;
            }
        }
    }
}
