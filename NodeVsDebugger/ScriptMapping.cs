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
using System.Linq;

namespace NodeVsDebugger
{
    class ScriptMapping
    {
        readonly Tuple<string, string>[] localRemote;

        public ScriptMapping()
        {
            localRemote = new Tuple<string, string>[0];
        }

        public ScriptMapping(IEnumerable<Tuple<string, string>> localRemote)
        {
            this.localRemote = localRemote.Select(mapping => {
                if (!mapping.Item1.EndsWith("\\") && !mapping.Item1.EndsWith("/"))
                    mapping = Tuple.Create(mapping.Item1 + "\\", mapping.Item2);
                if (!mapping.Item2.EndsWith("\\") && !mapping.Item2.EndsWith("/"))
                    mapping = Tuple.Create(mapping.Item1, mapping.Item2 + "/");
                return mapping;
            }).ToArray();
        }

        public ScriptMapping(Newtonsoft.Json.Linq.JToken jToken)
            : this((jToken as IEnumerable<KeyValuePair<string, Newtonsoft.Json.Linq.JToken>>)
            .Select(kvp => Tuple.Create(kvp.Key, (string)kvp.Value))) { }

        public string ToLocal(string remotePath)
        {
            return localRemote.Where(mapping => remotePath.StartsWith(mapping.Item2))
                .Select(mapping => mapping.Item1 + remotePath.Substring(mapping.Item2.Length).Replace('/', '\\'))
                .FirstOrDefault();
        }

        public string ToRemote(string localPath)
        {
            foreach (var mapping in localRemote) {
                var localBase = new Uri(mapping.Item1, UriKind.RelativeOrAbsolute);
                var localUrl = new Uri(localPath, UriKind.RelativeOrAbsolute);
                if (localBase.IsBaseOf(localUrl))
                    return mapping.Item2 + localBase.MakeRelativeUri(localUrl).OriginalString;
            }
            return null;
        }
    }
}
