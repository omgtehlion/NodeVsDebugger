using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    // this class represents a module loaded in the debuggee process to the debugger. 
    class AD7Module : IDebugModule2, IDebugModule3
    {
        public readonly NodeScript m_script;

        public AD7Module(NodeScript script)
        {
            m_script = script;
        }

        #region IDebugModule2 Members

        // Gets the MODULE_INFO that describes this module.
        // This is how the debugger obtains most of the information about the module.
        int IDebugModule2.GetInfo(enum_MODULE_INFO_FIELDS dwFields, MODULE_INFO[] infoArray)
        {
            try {
                var info = new MODULE_INFO();

                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_NAME) != 0) {
                    info.m_bstrName = System.IO.Path.GetFileName(m_script.Name);
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_NAME;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_URL) != 0) {
                    info.m_bstrUrl = m_script.Name;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URL;
                }
                infoArray[0] = info;

                return Constants.S_OK;
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugModule2.ReloadSymbols_Deprecated(string urlToSymbols, out string debugMessage)
        {
            debugMessage = null;
            Debug.Fail("This function is not called by the debugger.");
            return Constants.E_NOTIMPL;
        }

        int IDebugModule3.ReloadSymbols_Deprecated(string pszUrlToSymbols, out string pbstrDebugMessage)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugModule3 Members

        // IDebugModule3 represents a module that supports alternate locations of symbols and JustMyCode states.
        // The sample does not support alternate symbol locations or JustMyCode, but it does not display symbol load information 

        // Gets the MODULE_INFO that describes this module.
        // This is how the debugger obtains most of the information about the module.
        int IDebugModule3.GetInfo(enum_MODULE_INFO_FIELDS dwFields, MODULE_INFO[] pinfo)
        {
            return ((IDebugModule2)this).GetInfo(dwFields, pinfo);
        }

        // Returns a list of paths searched for symbols and the results of searching each path.
        int IDebugModule3.GetSymbolInfo(enum_SYMBOL_SEARCH_INFO_FIELDS dwFields, MODULE_SYMBOL_SEARCH_INFO[] pinfo)
        {
            // This engine only supports loading symbols at the location specified in the binary's symbol path location in the PE file and
            // does so only for the primary exe of the debuggee.
            // Therefore, it only displays if the symbols were loaded or not. If symbols were loaded, that path is added.
            pinfo[0] = new MODULE_SYMBOL_SEARCH_INFO { dwValidFields = 1, bstrVerboseSearchInfo = "Symbols not loaded" };
            return Constants.S_OK;
        }

        // Used to support the JustMyCode features of the debugger.
        // the sample debug engine does not support JustMyCode and therefore all modules
        // are considered "My Code"
        int IDebugModule3.IsUserCode(out int pfUser)
        {
            pfUser = 1;
            return Constants.S_OK;
        }

        // Loads and initializes symbols for the current module when the user explicitly asks for them to load.
        // The sample engine only supports loading symbols from the location pointed to by the PE file which will load
        // when the module is loaded.
        int IDebugModule3.LoadSymbols()
        {
            throw new NotImplementedException();
        }

        // Used to support the JustMyCode features of the debugger.
        // The sample engine does not support JustMyCode so this is not implemented
        int IDebugModule3.SetJustMyCodeState(int fIsUserCode)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}