// Guids.cs
// MUST match guids.h
using System;

namespace NodeVsDebugger_Package
{
    static class GuidList
    {
        public const string guidNodeVsDebugger_PackagePkgString = "967d54f7-9e49-43e3-a0eb-eb2d277c9e92";
        public const string guidNodeVsDebugger_PackageCmdSetString = "0bcf4159-3b16-415e-ae39-91a35b326606";

        public static readonly Guid guidNodeVsDebugger_PackageCmdSet = new Guid(guidNodeVsDebugger_PackageCmdSetString);
    };
}