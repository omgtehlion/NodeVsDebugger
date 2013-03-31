using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeVsDebugger
{
    public static class MyLogger
    {
        private class Logger
        {
            FileStream fs;
            DateTime created;

            internal Logger()
            {
                var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NodeVsDebugger\logs");
                Directory.CreateDirectory(fileName);
                fileName = Path.Combine(fileName, DateTime.Now.ToString(@"yyyyMMdd-HHmmss\.\l\o\g"));
                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 8, true);

                created = DateTime.UtcNow;
                WriteRaw(string.Format("Created {0:yyyy.MM.dd HH:mm:ss.fff}\r\n\r\n", created));
            }

            internal void Write(string text)
            {
                text = string.Format("{0,8} {1}\r\n", (int)(DateTime.UtcNow - created).TotalMilliseconds, text);
                WriteRaw(text);
            }

            internal void WriteRaw(string text)
            {
                var bytes = Encoding.Unicode.GetBytes(text);
                fs.BeginWrite(bytes, 0, bytes.Length, null, null);
            }
        }

        static Logger log = new Logger();

        public static void Trace(string text)
        {
            log.Write(text);
        }
    }
}
