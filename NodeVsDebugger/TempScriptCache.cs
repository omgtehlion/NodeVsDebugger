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

using System.Collections.Generic;
using System.IO;

namespace NodeVsDebugger
{
    class TempScriptCache
    {
        private readonly string tmpDir;
        private readonly List<string> tmpFiles = new List<string>();

        public TempScriptCache()
        {
            var xTmp = Path.GetTempFileName();
            tmpDir = Path.ChangeExtension(xTmp, null);
            try {
                File.Delete(xTmp);
            } catch { }
            Directory.CreateDirectory(tmpDir);
        }

        public void SaveScript(NodeScript script, string source)
        {
            var path = Path.Combine(tmpDir, script.Id.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, Path.GetFileName(script.Name));
            File.WriteAllText(path, source);
            File.SetAttributes(path, FileAttributes.Temporary | FileAttributes.ReadOnly);
            script.LocalFile = path;
            tmpFiles.Add(path);
        }

        public void Cleanup()
        {
            foreach (var f in tmpFiles) {
                try {
                    File.SetAttributes(f, FileAttributes.Temporary);
                    File.Delete(f);
                } catch { }
            }
            try {
                Directory.Delete(tmpDir, true);
            } catch { }
        }
    }
}
