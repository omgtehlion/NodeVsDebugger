//
// https://github.com/omgtehlion/NodeVsDebugger
// NodeVsDebugger: Node.js Debugging Support for Visual Studio.
//
// Authors:
//   Anton A. Drachev (anton@drachev.com)
//
// Copyright © 2013
//
// Licensed under the terms of BSD 2-Clause License.
// See a license.txt file for the full text of the license.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace NodeVsDebugger
{
    public class V8Connection
    {
        public event Action Closed;
        public event Action<Dictionary<string, string>, JObject> DataReceived;
        public event Action<Dictionary<string, string>> Connected;
        public string CloseReason { get; private set; }

        Stream port;
        StreamReader reader;
        Encoding latin1 = Encoding.GetEncoding("latin1");
        bool isClosed;
        Thread readerThread;
        bool isFirstMsg = true;
        Dictionary<string, string> currentHeaders = new Dictionary<string, string>();

        public bool IsClosed { get { return isClosed; } }

        NodeVsDebugger.MyLogger.Logger log;

        public V8Connection(Stream port)
        {
            this.port = port;
            reader = new StreamReader(port, latin1);
            log = new NodeVsDebugger.MyLogger.Logger("json", Encoding.UTF8, true);
            log.WriteRaw("{\"started\": \"" + log.Created.ToString("yyyy.MM.dd HH:mm:ss.fff") + "\", \"messages\": [");
        }

        void OnReceived(Dictionary<string, string> headers, JObject body)
        {
            if (isFirstMsg) {
                if (Connected != null)
                    Connected(headers);
                isFirstMsg = false;
            } else if (DataReceived != null) {
                LogMessage("in", body.ToString());
                DataReceived.Invoke(headers, body);
            }
        }

        private void LogMessage(string direction, string body)
        {
            log.WriteRaw("{\"type\": \"" + direction + "\", \"at\": \"" +
                log.Elapsed.ToString(CultureInfo.InvariantCulture) +
                "\", \"body\": " + body + "},");
        }

        public void SendData(string body)
        {
            var buff = Encoding.UTF8.GetBytes(body);
            try {
                lock (this) {
                    if (isClosed)
                        return;
                    LogMessage("out", body);
                    var headers = latin1.GetBytes(string.Format("Content-Length: {0}\r\n\r\n", buff.Length));
                    port.Write(headers, 0, headers.Length);
                    port.Write(buff, 0, buff.Length);
                }
            } catch {
                Close();
            }
        }

        public void StartReading()
        {
            ThreadPool.QueueUserWorkItem(ReadThreadWorker);
        }

        void ReadThreadWorker(object state)
        {
            readerThread = Thread.CurrentThread;
            try {
                while (ReadLoop()) { }
            } finally {
                readerThread = null;
                OnClose();
            }
        }

        bool ReadLoop()
        {
            string l;
            try {
                l = reader.ReadLine();
            } catch (IOException ex) {
                CloseReason = ex.ToString();
                return false;
            }
            if (l == null) {
                CloseReason = "Empty response line";
                return false;
            }
            if (l == "") {
                var len = int.Parse(currentHeaders["Content-Length"]);
                var buff = new char[len];
                var read = 0;
                try {
                    while (read < len)
                        read += reader.Read(buff, read, len - read);
                } catch (IOException ex) {
                    CloseReason = ex.ToString();
                    Close();
                    return false;
                }
                var recoded = Encoding.UTF8.GetString(latin1.GetBytes(buff));
                var body = (JObject)JsonConvert.DeserializeObject(recoded);
                OnReceived(currentHeaders, body);
                // done
                currentHeaders.Clear();
            } else {
                if (isFirstMsg && l == "Remote debugging session already active") {
                    CloseReason = l;
                    Close();
                    return false;
                }
                var parts = l.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    throw new Exception("Invalid header: " + l);
                currentHeaders.Add(parts[0], parts[1].Trim());
            }
            return true;
        }

        void OnClose()
        {
            isClosed = true;
            if (Closed != null)
                Closed.Invoke();
            try {
                port.Dispose();
            } catch { }
        }

        public void Close()
        {
            if (readerThread != null)
                readerThread.Abort();
            try {
                port.Dispose();
            } catch { }
        }
    }
}
