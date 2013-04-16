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
        internal class Logger
        {
            FileStream fs;
            Encoding encoding;
            internal readonly DateTime Created = DateTime.UtcNow;
            internal double Elapsed { get { return (DateTime.UtcNow - Created).TotalMilliseconds; } }

            internal Logger(string ext, Encoding encoding, bool noProlouge = false)
            {
                this.encoding = encoding;
                var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NodeVsDebugger\logs");
                Directory.CreateDirectory(fileName);
                fileName = Path.Combine(fileName, DateTime.Now.ToString(@"yyyyMMdd-HHmmss") + "." + ext);
                fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 8, true);

                if (!noProlouge)
                    WriteRaw(string.Format("Created {0:yyyy.MM.dd HH:mm:ss.fff}\r\n\r\n", Created));
            }

            internal void Write(string text)
            {
                text = string.Format("{0,8} {1}\r\n", (int)Elapsed, text);
                WriteRaw(text);
            }

            internal void WriteRaw(string text)
            {
                var bytes = encoding.GetBytes(text);
                fs.BeginWrite(bytes, 0, bytes.Length, null, null);
            }
        }

        static Logger log = new Logger("log", Encoding.Unicode);

        public static void Trace(string text)
        {
            log.Write(text);
        }
    }
}
