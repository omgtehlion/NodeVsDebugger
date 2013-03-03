using System;

namespace NodeVsDebugger
{
    // These are well-known guids in AD7. Most of these are used to specify filters in what the debugger UI is requesting.
    // For instance, guidFilterLocals can be passed to IDebugStackFrame2::EnumProperties to specify only locals are requested.
    static class AD7Guids
    {
        static public readonly Guid guidFilterRegisters = new Guid("223ae797-bd09-4f28-8241-2763bdc5f713");
        static public readonly Guid guidFilterLocals = new Guid("b200f725-e725-4c53-b36a-1ec27aef12ef");
        static public readonly Guid guidFilterAllLocals = new Guid("196db21f-5f22-45a9-b5a3-32cddb30db06");
        static public readonly Guid guidFilterArgs = new Guid("804bccea-0475-4ae7-8a46-1862688ab863");
        static public readonly Guid guidFilterLocalsPlusArgs = new Guid("e74721bb-10c0-40f5-807f-920d37f95419");
        static public readonly Guid guidFilterAllLocalsPlusArgs = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");
        static public readonly Guid guidFilterAutoRegisters = new Guid("38fc3258-d4d8-401e-a638-779a0145e906");  

        // Language guid for C++. Used when the language for a document context or a stack frame is requested.
        static public readonly Guid guidLanguageCpp = new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");
        // Language guid for JavaScript.
        static public readonly Guid guidLanguageJs = new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");
        // Language guid for Script.
        static public readonly Guid guidLanguageScript = new Guid("F7FA31DA-C32A-11D0-B442-00A0244A1DD2");
    }
}
