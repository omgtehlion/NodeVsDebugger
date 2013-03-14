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

using System.Linq;
using Newtonsoft.Json.Linq;

namespace NodeVsDebugger
{
    public class NodeThreadContext
    {
        private JObject jObject;

        public int index;
        public NodeScript script;
        public NodeFunc func;
        public int line;
        public int column;
        public Property[] Args;
        public Property[] Locals;

        public NodeThreadContext(JObject frame, DebuggedProcess proc)
        {
            jObject = frame;

            index = (int)frame["index"];
            func = new NodeFunc(proc.LookupRef(frame["func"]));
            script = proc.JsonToScript(proc.LookupRef(frame["script"]));
            line = (int)frame["line"];
            column = (int)frame["column"];

            Args = frame["arguments"].Select(x => new Property((JObject)x, proc, index)).ToArray();
            Locals = frame["locals"].Select(x => new Property((JObject)x, proc, index)).ToArray();
        }
    }

    public class NodeFunc
    {
        private JObject jObject;

        public string name;
        public string inferredName;
        public NodeFunc(JObject jObject)
        {
            this.jObject = jObject;

            name = (string)jObject["name"];
            inferredName = (string)jObject["inferredName"];
        }

        public string AnyName
        {
            get
            {
                return
                    (!string.IsNullOrEmpty(name) ? name :
                    (!string.IsNullOrEmpty(inferredName) ? inferredName :
                    "<no name>"));
            }
        }
    }
}
