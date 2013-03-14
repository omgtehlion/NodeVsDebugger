using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    using FIF = enum_FRAMEINFO_FLAGS;
    // Represents a logical stack frame on the thread stack. 
    // Also implements the IDebugExpressionContext interface, which allows expression evaluation and watch windows.
    class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        readonly AD7Engine m_engine;
        readonly AD7Thread m_thread;
        readonly NodeThreadContext m_threadContext;

        private string m_functionName;
        private uint m_lineNum;
        private bool m_hasSource = true;

        // An array of this frame's parameters
        private Property[] m_parameters { get { return m_threadContext.Args; } }
        // An array of this frame's locals
        private Property[] m_locals { get { return m_threadContext.Locals; } }

        public AD7StackFrame(AD7Engine engine, AD7Thread thread, NodeThreadContext threadContext)
        {
            m_engine = engine;
            m_thread = thread;
            m_threadContext = threadContext;

            // Try to get source information for this location. If symbols for this file have not been found, this will fail.
            m_functionName = m_threadContext.func.AnyName;
            m_lineNum = (uint)m_threadContext.line;
        }

        #region Non-interface methods

        // Construct a FRAMEINFO for this stack frame with the requested information.
        public FRAMEINFO GetFrameInfo(FIF dwFieldSpec)
        {
            var frameInfo = new FRAMEINFO();
            // TODO: were called:
            // Modules.Add(mod);
            // Callback.OnModuleLoad(mod);

            // The debugger is asking for the formatted name of the function which is displayed in the callstack window.
            // There are several optional parts to this name including the module, argument types and values, and line numbers.
            // The optional information is requested by setting flags in the dwFieldSpec parameter.
            if (dwFieldSpec.HasFlag(FIF.FIF_FUNCNAME)) {
                // If there is source information, construct a string that contains the module name, function name, and optionally argument names and values.
                if (m_hasSource) {
                    var funcName = new StringBuilder();

                    if (dwFieldSpec.HasFlag(FIF.FIF_FUNCNAME_MODULE))
                        funcName.Append(System.IO.Path.GetFileName(m_threadContext.script.Name) + "!");

                    funcName.Append(m_functionName);

                    if (dwFieldSpec.HasFlag(FIF.FIF_FUNCNAME_ARGS)) {
                        funcName.Append("(");
                        var format = GetArgumentFormat(dwFieldSpec);
                        funcName.Append(string.Join(", ", m_parameters
                            .Select(p => string.Format(format, p.m_typeName, string.IsNullOrEmpty(p.m_name) ? "?" : p.m_name, p.m_value))
                        ));
                        funcName.Append(")");
                    }

                    if (dwFieldSpec.HasFlag(FIF.FIF_FUNCNAME_LINES))
                        funcName.AppendFormat(" Line {0}", m_lineNum);
                    frameInfo.m_bstrFuncName = funcName.ToString();
                } else {
                    throw new NotImplementedException();
                }
                frameInfo.m_dwValidFields |= FIF.FIF_FUNCNAME;
            }

            // The debugger is requesting the name of the module for this stack frame.
            if (dwFieldSpec.HasFlag(FIF.FIF_MODULE)) {
                frameInfo.m_bstrModule = m_threadContext.script.Name;
                frameInfo.m_dwValidFields |= FIF.FIF_MODULE;
            }

            // The debugger is requesting the IDebugStackFrame2 value for this frame info.
            if (dwFieldSpec.HasFlag(FIF.FIF_FRAME)) {
                frameInfo.m_pFrame = this;
                frameInfo.m_dwValidFields |= FIF.FIF_FRAME;
            }

            // Does this stack frame of symbols loaded?
            if (dwFieldSpec.HasFlag(FIF.FIF_DEBUGINFO)) {
                frameInfo.m_fHasDebugInfo = m_hasSource ? 1 : 0;
                frameInfo.m_dwValidFields |= FIF.FIF_DEBUGINFO;
            }

            // Is this frame stale?
            if (dwFieldSpec.HasFlag(FIF.FIF_STALECODE)) {
                frameInfo.m_fStaleCode = 0;
                frameInfo.m_dwValidFields |= FIF.FIF_STALECODE;
            }
            return frameInfo;
        }

        private static string GetArgumentFormat(FIF dwFieldSpec)
        {
            switch (dwFieldSpec & FIF.FIF_FUNCNAME_ARGS_ALL) {
                case FIF.FIF_FUNCNAME_ARGS_TYPES:
                    return "{0}";
                case FIF.FIF_FUNCNAME_ARGS_NAMES:
                    return "{1}";
                case FIF.FIF_FUNCNAME_ARGS_VALUES:
                    return "{2}";
                case FIF.FIF_FUNCNAME_ARGS_TYPES | FIF.FIF_FUNCNAME_ARGS_NAMES:
                    return "{0} {1}";
                case FIF.FIF_FUNCNAME_ARGS_TYPES | FIF.FIF_FUNCNAME_ARGS_VALUES:
                    return "{0} ={2}";
                case FIF.FIF_FUNCNAME_ARGS_NAMES | FIF.FIF_FUNCNAME_ARGS_VALUES:
                    return "{1}={2}";
                case FIF.FIF_FUNCNAME_ARGS_ALL:
                    return "{0} {1}={2}";
                default:
                    return "";
            }
        }

        #endregion

        #region IDebugStackFrame2 Members

        // Creates an enumerator for properties associated with the stack frame, such as local variables.
        // The sample engine only supports returning locals and parameters. Other possible values include
        // class fields (this pointer), registers, exceptions...
        int IDebugStackFrame2.EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout, out uint elementsReturned, out IEnumDebugPropertyInfo2 enumObject)
        {
            int hr;
            elementsReturned = 0;
            enumObject = null;
            try {
                IEnumerable<Property> props = null;
                hr = Constants.S_OK;
                if (guidFilter == AD7Guids.guidFilterLocalsPlusArgs || guidFilter == AD7Guids.guidFilterAllLocalsPlusArgs || guidFilter == AD7Guids.guidFilterAllLocals) {
                    props = m_locals.Concat(m_parameters);
                } else if (guidFilter == AD7Guids.guidFilterLocals) {
                    props = m_locals;
                } else if (guidFilter == AD7Guids.guidFilterArgs) {
                    props = m_parameters;
                } else {
                    hr = Constants.E_NOTIMPL;
                }
                if (props != null) {
                    var propInfos = props
                        .Select(p => new AD7Property(p).ConstructDebugPropertyInfo(dwFields))
                        .ToArray();
                    elementsReturned = (uint)propInfos.Length;
                    enumObject = new AD7PropertyInfoEnum(propInfos);
                }
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }
            return hr;
        }

        // Gets the code context for this stack frame. The code context represents the current instruction pointer in this stack frame.
        int IDebugStackFrame2.GetCodeContext(out IDebugCodeContext2 memoryAddress)
        {
            memoryAddress = null;
            return Constants.S_FALSE;
        }

        // Gets a description of the properties of a stack frame.
        // Calling the IDebugProperty2::EnumChildren method with appropriate filters can retrieve the local variables, method parameters, registers, and "this" 
        // pointer associated with the stack frame. The debugger calls EnumProperties to obtain these values in the sample.
        int IDebugStackFrame2.GetDebugProperty(out IDebugProperty2 property)
        {
            property = null;
            return Constants.S_FALSE;
        }

        // Gets the document context for this stack frame. The debugger will call this when the current stack frame is changed
        // and will use it to open the correct source document for this stack frame.
        int IDebugStackFrame2.GetDocumentContext(out IDebugDocumentContext2 docContext)
        {
            docContext = null;
            try {
                if (m_hasSource) {
                    // Assume all lines begin and end at the beginning of the line.
                    var documentName = m_engine.DebuggedProcess.GetLocalFile(m_threadContext.script, fetchIfNotExists: true);
                    docContext = new AD7DocumentContext(documentName, m_lineNum, m_lineNum);
                    return Constants.S_OK;
                }
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }

            return Constants.S_FALSE;
        }

        // Gets an evaluation context for expression evaluation within the current context of a stack frame and thread.
        // Generally, an expression evaluation context can be thought of as a scope for performing expression evaluation. 
        // Call the IDebugExpressionContext2::ParseText method to parse an expression and then call the resulting IDebugExpression2::EvaluateSync 
        // or IDebugExpression2::EvaluateAsync methods to evaluate the parsed expression.
        int IDebugStackFrame2.GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
        {
            ppExprCxt = this;
            return Constants.S_OK;
        }

        // Gets a description of the stack frame.
        int IDebugStackFrame2.GetInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, FRAMEINFO[] pFrameInfo)
        {
            try {
                pFrameInfo[0] = GetFrameInfo(dwFieldSpec);
                return Constants.S_OK;
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets the language associated with this stack frame. 
        // In this sample, all the supported stack frames are C++
        int IDebugStackFrame2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = "JavaScript";
            pguidLanguage = AD7Guids.guidLanguageJs;
            return Constants.S_OK;
        }

        // Gets the name of the stack frame.
        // The name of a stack frame is typically the name of the method being executed.
        int IDebugStackFrame2.GetName(out string name)
        {
            name = "IDebugStackFrame2.GetName";
            return Constants.S_OK;
        }

        // Gets a machine-dependent representation of the range of physical addresses associated with a stack frame.
        int IDebugStackFrame2.GetPhysicalStackRange(out ulong addrMin, out ulong addrMax)
        {
            addrMin = 0;
            addrMax = 0;
            return Constants.E_NOTIMPL;
        }

        // Gets the thread associated with a stack frame.
        int IDebugStackFrame2.GetThread(out IDebugThread2 thread)
        {
            thread = m_thread;
            return Constants.S_OK;
        }

        #endregion

        #region IDebugExpressionContext2 Members

        // Retrieves the name of the evaluation context. 
        // The name is the description of this evaluation context. It is typically something that can be parsed by an expression evaluator 
        // that refers to this exact evaluation context. For example, in C++ the name is as follows: 
        // "{ function-name, source-file-name, module-file-name }"
        int IDebugExpressionContext2.GetName(out string pbstrName)
        {
            throw new NotImplementedException();
        }

        // Parses a text-based expression for evaluation.
        // The engine sample only supports locals and parameters so the only task here is to check the names in those collections.
        int IDebugExpressionContext2.ParseText(string pszCode, enum_PARSEFLAGS dwFlags, uint nRadix, out IDebugExpression2 ppExpr, out string pbstrError, out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = null;

            try {
                var localOrArg = m_parameters.Concat(m_locals).FirstOrDefault(v => String.CompareOrdinal(v.m_name, pszCode) == 0);
                if (localOrArg != null) {
                    ppExpr = new AD7Property(localOrArg);
                    return Constants.S_OK;
                }
                var result = m_engine.DebuggedProcess.Evaluate(m_threadContext.index, pszCode, out pbstrError);
                if (result != null) {
                    ppExpr = new AD7Property(result);
                    return Constants.S_OK;
                }
                pichError = (uint)pbstrError.Length;
                return Constants.S_FALSE;
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #endregion
    }
}

