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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NodeVsDebugger
{
    public class V8DebugSession
    {
        public event Action Closed;
        public event Action<Dictionary<string, string>> Connected;
        public event Action<string, JToken> EventReceived;
        public event Action<JObject> MismatchedResponse;

        V8Connection connection;
        Dictionary<int, JObject> references = new Dictionary<int, JObject>();
        int reqSeq = 1;
        Dictionary<int, Action<JObject>> callbacks = new Dictionary<int, Action<JObject>>();

        public V8DebugSession(V8Connection connection)
        {
            this.connection = connection;
            connection.DataReceived += connection_DataReceived;
            connection.Closed += connection_Closed;
            connection.Connected += connection_Connected;
        }

        void connection_Connected(Dictionary<string, string> headers)
        {
            if (Connected != null)
                Connected.Invoke(headers);
        }

        void connection_Closed()
        {
            lock (this) {
                foreach (var cb in callbacks.Values)
                    cb(null);
                callbacks.Clear();
            }
            if (Closed != null)
                Closed.Invoke();
        }

        public V8DebugSession(string host, int port) : this(ConnectTo(host, port)) { }

        static V8Connection ConnectTo(string host, int port)
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new DnsEndPoint(host, port));
            return new V8Connection(new NetworkStream(sock, true));
        }

        void connection_DataReceived(Dictionary<string, string> headers, JObject body)
        {
            if (body == null)
                return;
            switch ((string)body["type"]) {
                case "response":
                    OnResponse(body);
                    break;
                case "event":
                    OnEvent(body);
                    break;
                default:
                    Debug.Assert(false, "Unknown message type: " + body);
                    break;
            }
        }

        void OnEvent(JObject body)
        {
            // All objects exposed through the debugger is assigned an ID called a handle.
            // This handle is serialized and can be used to identify objects.
            // A handle has a certain lifetime after which it will no longer refer to the same object.
            // Currently the lifetime of handles match the processing of a debug event.
            // For each debug event handles are recycled.
            references.Clear();
            ParseRefs(body);
            if (EventReceived != null)
                EventReceived.Invoke((string)body["event"], (JToken)body["body"]);
        }

        void OnResponse(JObject body)
        {
            ParseRefs(body);
            if (body["request_seq"] != null) {
                var requestSeq = (int)body["request_seq"];
                Action<JObject> callback = null;
                lock (this) {
                    if (callbacks.ContainsKey(requestSeq)) {
                        callback = callbacks[requestSeq];
                        callbacks.Remove(requestSeq);
                    }
                }
                if (callback != null) {
                    callback(body);
                } else {
                    if (MismatchedResponse != null)
                        MismatchedResponse.Invoke(body);
                }
            }
        }

        void ParseRefs(JObject body)
        {
            var refs = body["refs"] as JArray;
            if (refs == null)
                return;
            foreach (JObject iref in refs) {
                var handle = (int)iref["handle"];
                if (references.ContainsKey(handle))
                    references.Remove(handle);
                references.Add(handle, iref);
            }
        }

        public void RequestRaw(string command, string rawArgs, Action<JObject> callback = null)
        {
            RequestRaw(command, new JRaw(rawArgs), callback);
        }

        public void Request<T>(string command, T arguments, Action<JObject> callback = null)
        {
            RequestRaw(command, JsonConvert.SerializeObject(arguments), callback);
        }

        public JObject RequestSync<T>(string command, T arguments, int timeoutMs = Timeout.Infinite)
        {
            var evt = new ManualResetEvent(false);
            JObject result = null;
            Request(command, arguments, resp => {
                result = resp;
                evt.Set();
            });
            if (connection.IsClosed)
                return result;
            evt.WaitOne(timeoutMs);
            return result;
        }

        public void RequestRaw(string command, JToken arguments, Action<JObject> callback = null)
        {
            var seq = reqSeq++;
            if (callback != null) {
                lock (this)
                    callbacks.Add(seq, callback);
            }
            connection.SendData(JsonConvert.SerializeObject(new {
                seq,
                type = "request",
                command,
                arguments,
            }));
        }

        public JObject LookupRef(int refHandle, int timeoutMs = 0)
        {
            lock (this) {
                JObject result;
                if (references.TryGetValue(refHandle, out result))
                    return result;
            }
            var response = RequestSync("lookup", new { handles = new[] { refHandle } }, timeoutMs);
            if (response == null)
                return null;
            // TODO: add to cache
            return (JObject)response["body"][refHandle.ToString()];
        }

        public JObject LookupFullString(int refHandle, int timeoutMs = 1000)
        {
            // TODO: search in cache
            var response = RequestSync("lookup", new { handles = new[] { refHandle }, maxStringLength = -1 }, timeoutMs);
            if (response == null)
                return null;
            // TODO: add to cache
            return (JObject)response["body"][refHandle.ToString()];
        }

        public void Close()
        {
            connection.Close();
        }

        public void StartReading()
        {
            connection.StartReading();
        }
    }
}
