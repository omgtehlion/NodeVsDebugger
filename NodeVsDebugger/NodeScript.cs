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

using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace NodeVsDebugger
{
    [DebuggerDisplay("{Id} {Name}")]
    public class NodeScript : IEquatable<NodeScript>
    {
        private JObject jObject;

        public string Name;
        public int Id;
        public int LineOffset;
        public int ColumnOffset;
        public string LocalFile;

        public NodeScript(int id, string name, JObject jObject)
        {
            this.jObject = jObject;

            Id = id;
            Name = name;
            LineOffset = (int)jObject["lineOffset"];
            ColumnOffset = (int)jObject["columnOffset"];
        }

        bool IEquatable<NodeScript>.Equals(NodeScript other)
        {
            if (other == null)
                return false;
            return this == other || (Id == other.Id && Name == other.Name);
        }
    }
}
