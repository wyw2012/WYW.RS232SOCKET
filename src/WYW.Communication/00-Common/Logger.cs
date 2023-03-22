﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WYW.Communication
{
    class Logger
    {
        public static void Debug(string content, bool withTimeStamp = false)
        {
            Write("Log\\Debug", content, withTimeStamp);
        }
        public static void Error(string content, bool withTimeStamp = false)
        {
            Write("Log\\Error", content, withTimeStamp);
        }
        public static void Info(string content, bool withTimeStamp = false)
        {
            Write("Log\\Info", content, withTimeStamp);
        }
        public static void Exception(string content, bool withTimeStamp = false)
        {
            Write("Log\\Exception", content, withTimeStamp);
        }
        public static void Write(string folder, string content, bool withTimeStamp = false)
        {
            var writer = LogWritter.CreateInstance(folder);
            writer.WriteLine(content, withTimeStamp);
        }
    }

    class LogWritter
    {

        private static object locker = new object();

        private static List<LogWritter> loggerList = new List<LogWritter>();

        static LogWritter logger = null;
        public static LogWritter CreateInstance(string folder)
        {
            var logger = loggerList.SingleOrDefault(x => x.Folder == folder);
            if (logger == null)
            {
                logger = new LogWritter(folder);
                loggerList.Add(logger);
            }
            return logger;
        }

        private StringBuilder sb = new StringBuilder(1024000);
        private DateTime lastWriteTime = DateTime.MinValue;
        private System.Timers.Timer timer;

        public LogWritter(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            Folder = folder;
            timer = new System.Timers.Timer(3000) { Enabled = true, AutoReset = false };
            timer.Elapsed += Timer_Elapsed;

        }

        private string Folder { get; }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Write();
            timer.Stop();
        }

        /// <summary>
        /// 单个文件最大长度，单位MB
        /// </summary>
        public int MaxFileLength { get; set; } = 10;

        public void WriteLine(string content, bool withTimeStamp)
        {
            lock (locker)
            {
                // 先加入队列
                if (withTimeStamp)
                {
                    sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {content}");
                }
                else
                {
                    sb.AppendLine($"{content}");
                }
            }
            if ((DateTime.Now - lastWriteTime).TotalMilliseconds > timer.Interval)
            {
                Write();
            }
            else
            {
                timer.Start();
            }

        }


        /// <summary>
        /// 立即写入磁盘
        /// </summary>
        /// <param name="folder"></param>
        private void Write()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var filePath = Path.Combine(Folder, GetFileName(Folder));
                    lock (locker)
                    {
                        using (var sw = new StreamWriter(filePath, true, Encoding.Default))
                        {
                            sw.Write(sb.ToString());
                        }
                        lastWriteTime = DateTime.Now;
                        sb.Clear();
                    }
                }
                catch
                {

                }
            });
        }

        /// <summary>
        /// 根据文件长度进行拆分文件
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private string GetFileName(string folder)
        {
            string fileName = $"{DateTime.Now:yyyy-MM-dd}_001.txt";
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            var files = dirInfo.GetFiles().Where(x => x.CreationTime > new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            if (files.Any())
            {
                FileInfo file = files.OrderByDescending(x => x.CreationTime).First();

                if (file.Length < 1024 * 1024 * MaxFileLength)
                {
                    fileName = file.Name;
                }
                else
                {
                    var temp = file.Name.Replace(".txt", "").Split('_');
                    if (temp.Length >= 2)
                    {
                        int index = 0;
                        int.TryParse(temp[1], out index);
                        fileName = $"{DateTime.Now:yyyy-MM-dd}_{++index:D3}.txt";
                    }
                }
            }

            return fileName;
        }
    }


}