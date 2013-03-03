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

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace NodeVsDebugger
{
    public class DebuggedThread
    {
        public int Id;
        public List<NodeThreadContext> StackFrames = new List<NodeThreadContext>();
        internal AD7Thread Client;
    }
}
