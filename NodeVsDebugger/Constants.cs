using System;

namespace NodeVsDebugger
{
    public class Constants
    {
        public const int E_FAIL = unchecked((int)0x80004005U);
        public const int E_NOTIMPL = unchecked((int)0x80004001U);
        public const int E_WIN32_ALREADY_INITIALIZED = 1247;
        public const int E_WIN32_INVALID_NAME = 123;
        public const int RPC_E_SERVERFAULT = unchecked((int)0x80010105U);
        public const int S_FALSE = 1;
        public const int S_OK = 0;

        public const string ProgramProviderGuid = "2550642A-954E-4375-97EC-0432634D0559";
        public const string DebugEngineGuid = "22BA18B8-7559-4D3F-9ECB-BC583C5E3774";
        public const string DebuggerGuid = "24883480-6830-46EF-AC78-65E17F06DC6D";
        public const string DebuggerIdString = "guidNodeVsDebugger";

        public const string EngineName = "Node.js Debugger";
    }
}
