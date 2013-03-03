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
using System.IO;
using System.Linq;

namespace NodeVsDebugger
{
    public static class Tools
    {
        static HashSet<string> reservedNames = new HashSet<string> {
            "arguments", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "enum",
            "eval", "export", "extends", "false", "finally", "for", "function", "if", "implements", "import", "in", "instanceof", "interface",
            "let", "new", "null", "package", "private", "protected", "public", "return", "static", "super", "switch", "this", "throw", "true",
            "try", "typeof", "var", "void", "while", "with", "yield"
        };

        public static bool IsValidIdentifier(string name)
        {
            if (reservedNames.Contains(name))
                return false;
            if (name[0] != '$' && name[0] != '_' && !char.IsLetter(name[0]))
                return false;
            return name.Skip(1).All(c => char.IsLetterOrDigit(c) || char.IsNumber(c));
        }

        static string defaultNode;
        public static string GetDefaultNode()
        {
            if (defaultNode != null)
                return defaultNode;
            var path = Environment.GetEnvironmentVariable("PATH").Split(';');
            defaultNode = path.Select(p => Path.Combine(p, "node.exe")).FirstOrDefault(File.Exists);
            if (defaultNode == null) {
                // TODO: debug log
                path = new[] {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                };
                defaultNode = path.Select(p => Path.Combine(p, @"nodejs\node.exe")).FirstOrDefault(File.Exists);
            }
            if (defaultNode == null) {
                // TODO: debug log
            }
            return defaultNode;
        }
    }
}
