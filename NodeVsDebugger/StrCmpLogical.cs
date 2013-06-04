using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace NodeVsDebugger
{
    [SuppressUnmanagedCodeSecurity]
    public sealed class StrCmpLogical : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public int Compare(string a, string b)
        {
            return StrCmpLogicalW(a, b);
        }

        public static readonly StrCmpLogical Instance = new StrCmpLogical();
    }
}
