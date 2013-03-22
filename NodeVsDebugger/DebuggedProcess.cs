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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NodeVsDebugger
{
    public sealed class DebuggedProcess
    {
        public readonly int Id;
        public readonly string Name;
        public V8DebugSession dbg;
        Process proc;
        EngineCallback Callback;
        WorkerThread m_pollThread;
        string dbgHost = "localhost";
        int dbgPort = 5858;
        bool dbgConnectOnly = false;
        string dbgWorkDir;
        string nodeExe;
        string main;
        ManualResetEvent attachEvent;
        volatile bool attached = false;
        TempScriptCache tempScriptCache;
        ScriptMapping mappings;

        public DebuggedProcess(string exe, string args, WorkerThread pollThread, EngineCallback callback)
        {
            Callback = callback;
            m_pollThread = pollThread;
            tempScriptCache = new TempScriptCache();
            main = exe;
            dbgWorkDir = Path.GetDirectoryName(exe);
            ParseConfig(args);

            proc = new Process();
            if (!dbgConnectOnly) {
                if (nodeExe == null || !File.Exists(nodeExe)) {
                    System.Windows.Forms.MessageBox.Show("ERROR: node.exe not found.\r\n\r\n" +
                        "Please make sure it is on your %PATH% or installed in default location.\r\n\r\n" +
                        "You can download a copy of Node at http://nodejs.org/", "NodeVsDebugger");
                    throw new ArgumentException("node.exe not found");
                }

                proc.StartInfo = new ProcessStartInfo {
                    Arguments = string.Format("--debug-brk={0} {1}", dbgPort, main),
                    FileName = nodeExe,
                    UseShellExecute = false,
                    WorkingDirectory = dbgWorkDir,
                };
            } else {
                // using fake process and connecting to another machine
                proc.StartInfo = new ProcessStartInfo {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"cmd.exe"),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
            }
            try {
                proc.Start();
                Id = proc.Id;
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("ERROR: starting process\r\n" + ex, "NodeVsDebugger");
                throw;
            }
        }

        public void Attach()
        {
            try {
                dbg = new V8DebugSession(dbgHost, dbgPort);
                dbg.Connected += dbg_Connected;
                dbg.Closed += dbg_Closed;
                dbg.StartReading();
            } catch (Exception ex) {
                System.Windows.Forms.MessageBox.Show("ERROR connecting to debuggee:\r\n" + ex, "NodeVsDebugger");
                throw;
            }
        }

        private void ParseConfig(string confText)
        {
            if (confText == null) {
                mappings = new ScriptMapping();
                nodeExe = Tools.GetDefaultNode();
                dbgPort = RandomizePort();
                return;
            }
            var conf = JsonConvert.DeserializeObject(confText) as JObject;
            if (conf == null)
                throw new ArgumentException("cannot deserialize config");

            if (conf["port"] != null)
                dbgPort = (int)conf["port"];

            var mappingConf = conf["mappings"] as JObject;
            mappings = mappingConf != null ? new ScriptMapping(mappingConf) : new ScriptMapping();

            switch ((string)conf["mode"]) {
                case null:
                case "run":
                    main = (string)conf["main"] ?? main;
                    nodeExe = (string)conf["node"] ?? Tools.GetDefaultNode();
                    dbgHost = "localhost";
                    break;
                case "connect":
                    dbgConnectOnly = true;
                    dbgHost = (string)conf["host"] ?? dbgHost;
                    break;
                default:
                    throw new ArgumentException("mode = " + (string)conf["mode"]);
            }
        }

        private int RandomizePort()
        {
            var port = 0;
            // node.js requires debug_port > 1024 && debug_port < 65536
            while (port <= 1024 || port >= 65536) {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
            }
            return port;
        }

        void dbg_Closed()
        {
            m_pollThread.RunOperation(() => Callback.OnProcessExit(0));
            Cleanup();
        }

        private void Cleanup()
        {
            if (proc != null && !proc.HasExited)
                proc.Kill();
            if (tempScriptCache != null)
                tempScriptCache.Cleanup();
        }

        void dbg_Connected(Dictionary<string, string> obj)
        {
            attached = true;
            if (attachEvent != null)
                attachEvent.Set();
            dbg.EventReceived += dbg_EventReceived;
            dbg.Request("setexceptionbreak", new { type = "uncaught", enabled = true });
            //dbg.Request("setexceptionbreak", new { type = "all", enabled = true });
        }

        void dbg_EventReceived(string evt, JToken body)
        {
            switch (evt) {
                case "break":
                case "exception":
                    m_pollThread.RunOperation(() => Callback.OnAsyncBreakComplete(Threads[0]));
                    break;
                case "afterCompile":
                    var mod = JsonToScript((JObject)body["script"]);
                    Callback.OnModuleLoad(mod);
                    break;
            }
        }

        public void Break()
        {
            Callback.OnAsyncBreakComplete(Threads[0]);
        }
        public void Continue(DebuggedThread thread)
        {
            dbg.Request("continue", "");
        }

        public void Detach()
        {
            Callback.OnProgramDestroy(0);
            dbg.Request("disconnect", "");
            dbg.Close();
        }
        public void Execute(DebuggedThread thread)
        {
            dbg.Request("continue", "");
        }
        public List<NodeScript> Modules = new List<NodeScript>();
        public NodeScript[] GetModules()
        {
            return Modules.ToArray();
        }
        public List<DebuggedThread> Threads = new List<DebuggedThread>();
        public DebuggedThread[] GetThreads()
        {
            return Threads.ToArray();
        }
        public void ResumeFromLaunch()
        {
            dbg.Request("continue", "");
        }
        public void Terminate()
        {
            dbg.RequestSync("evaluate", new { expression = "process.exit(1);", global = true }, 500);
            Detach();
        }

        internal void DoStackWalk(DebuggedThread debuggedThread)
        {
            debuggedThread.StackFrames = new List<NodeThreadContext>();
            var frameId = 0;
            var totalFrames = int.MaxValue;
            var maxTicks = DateTime.Now.AddSeconds(5).Ticks;
            while (frameId < totalFrames && DateTime.Now.Ticks < maxTicks) {
                if (!FetchFrames(debuggedThread, frameId, 5, ref totalFrames))
                    break;
                frameId += 5;
            }
        }

        private bool FetchFrames(DebuggedThread debuggedThread, int fromFrame, int count, ref int totalFrames)
        {
            // we have a problem here: http://code.google.com/p/v8/issues/detail?id=1705
            // better use inlineRefs = true (https://github.com/joyent/node/pull/2379/files)
            var resp = dbg.RequestSync("backtrace", new { fromFrame, toFrame = fromFrame + count, inlineRefs = true }, 1000);
            if (resp == null)
                return false;
            totalFrames = (int)resp["body"]["totalFrames"];
            var frames = (JArray)resp["body"]["frames"];
            if (frames != null)
                debuggedThread.StackFrames.AddRange(frames.Select(x => new NodeThreadContext((JObject)x, this)));
            return true;
        }

        internal void Step(DebuggedThread debuggedThread, Microsoft.VisualStudio.Debugger.Interop.enum_STEPKIND sk)
        {
            var stepaction = "next";
            switch (sk) {
                case Microsoft.VisualStudio.Debugger.Interop.enum_STEPKIND.STEP_INTO:
                    stepaction = "in";
                    break;
                case Microsoft.VisualStudio.Debugger.Interop.enum_STEPKIND.STEP_OUT:
                    stepaction = "out";
                    break;
            }
            dbg.Request("continue", new { stepaction, stepcount = 1 });
        }

        internal Property Evaluate(int frameId, string code, out string error)
        {
            var result = dbg.RequestSync("evaluate", new { expression = code, frame = frameId });
            if ((bool)result["success"]) {
                error = null;
                var fakeVar = new JObject(new JProperty("name", code), new JProperty("value", result["body"]));
                return new Property(fakeVar, this, frameId);
            }
            error = (string)result["message"];
            return null;
        }

        internal int SetBreakpoint(string documentName, uint line, uint column)
        {
            documentName = LocalFileToNodeScript(documentName);
            var resp = dbg.RequestSync("setbreakpoint", new {
                type = "script",
                target = documentName,
                line,
                column,
                enabled = true,
            });
            if ((bool)resp["success"]) {
                return (int)resp["body"]["breakpoint"];
            }
            return -1;
        }

        private string LocalFileToNodeScript(string documentName)
        {
            var m = Modules.FirstOrDefault(x => x.LocalFile == documentName);
            if (m != null)
                return m.Name;
            return mappings.ToRemote(documentName) ?? documentName;
        }

        internal string GetLocalFile(NodeScript script, bool fetchIfNotExists = false)
        {
            if (script.LocalFile == null)
                script.LocalFile = mappings.ToLocal(script.Name);
            if (script.LocalFile == null && File.Exists(script.Name))
                script.LocalFile = script.Name;
            if (fetchIfNotExists && (script.LocalFile == null || !File.Exists(script.LocalFile))) {
                // fetch actual file
                var evt = new ManualResetEvent(false);
                dbg.Request("scripts", new { types = 7, ids = new[] { script.Id }, includeSource = true }, resp => {
                    var body = resp["body"][0];
                    tempScriptCache.SaveScript(script, (string)body["source"]);
                    evt.Set();
                });
                evt.WaitOne(200);
            }
            return script.LocalFile;
        }

        public void RemoveBreakpoint(int breakpointId)
        {
            dbg.RequestSync("clearbreakpoint", new { breakpoint = breakpointId });
        }

        internal void WaitForAttach()
        {
            attachEvent = new ManualResetEvent(false);
            if (attached) {
                attachEvent = null;
                return;
            }
            if (!attachEvent.WaitOne(10 * 1000))
                throw new Exception("cannot attach");
        }

        internal JObject LookupRef(JToken jToken, int timeoutMs = 600)
        {
            var refId = jToken["ref"];
            if (refId == null)
                return (JObject)jToken;
            return dbg.LookupRef((int)jToken["ref"], timeoutMs) ?? (JObject)jToken;
        }

        internal NodeScript JsonToScript(JObject jObject)
        {
            var id = (int)jObject["id"];
            var name = (string)jObject["name"];
            var script = Modules.FirstOrDefault(m => m.Id == id && m.Name == name);
            if (script == null) {
                script = new NodeScript(id, name, jObject);
                Modules.Add(script);
            }
            return script;
        }
    }
}
