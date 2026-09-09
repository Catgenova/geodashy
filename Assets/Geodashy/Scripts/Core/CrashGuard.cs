using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Geodashy.Core
{
    /// <summary>Keeps the last log lines and writes a bug report file when a run throws.</summary>
    public static class CrashGuard
    {
        const int Keep = 60;
        static readonly Queue<string> log = new Queue<string>();
        static bool hooked;

        public static void Hook()
        {
            if (hooked) return;
            hooked = true;
            Application.logMessageReceived += (msg, stack, type) =>
            {
                log.Enqueue("[" + type + "] " + msg);
                while (log.Count > Keep) log.Dequeue();
            };
        }

        /// <summary>Writes a report next to the captures and returns its path (empty on failure).</summary>
        public static string Report(Exception e, string context)
        {
            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "geodashy", "captures");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "bug_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(GameInfo.Title + " bug report " + DateTime.Now.ToString("u"));
                sb.AppendLine("Version " + Application.version + " · " + Application.platform + " · Unity " + Application.unityVersion);
                sb.AppendLine(context);
                sb.AppendLine();
                sb.AppendLine(e.ToString());
                sb.AppendLine();
                sb.AppendLine("Recent log:");
                foreach (var line in log) sb.AppendLine(line);
                File.WriteAllText(path, sb.ToString());
                return path;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
