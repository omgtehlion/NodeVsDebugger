using System;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    // This class represents a document context to the debugger. A document context represents a location within a source file. 
    class AD7DocumentContext : IDebugDocumentContext2
    {
        // And implementation of IDebugCodeContext2 and IDebugMemoryContext2. 
        // IDebugMemoryContext2 represents a position in the address space of the machine running the program being debugged.
        // IDebugCodeContext2 represents the starting position of a code instruction. 
        // For most run-time architectures today, a code context can be thought of as an address in a program's execution stream.
        //class AD7CodeContext : IDebugCodeContext2, IDebugCodeContext100
        //AD7MemoryAddress m_codeContext;

        readonly string m_fileName;
        TEXT_POSITION m_begPos;
        TEXT_POSITION m_endPos;

        public AD7DocumentContext(string fileName, TEXT_POSITION begPos, TEXT_POSITION endPos)
        {
            m_fileName = fileName;
            m_begPos = begPos;
            m_endPos = endPos;
        }

        public AD7DocumentContext(string fileName, uint begLine, uint endLine, uint begCol = 0, uint endCol = 0)
        {
            m_fileName = fileName;
            m_begPos = new TEXT_POSITION { dwColumn = begCol, dwLine = begLine };
            m_endPos = new TEXT_POSITION { dwColumn = endCol, dwLine = endLine };
        }

        #region IDebugDocumentContext2 Members

        // Compares this document context to a given array of document contexts.
        int IDebugDocumentContext2.Compare(enum_DOCCONTEXT_COMPARE Compare, IDebugDocumentContext2[] rgpDocContextSet, uint dwDocContextSetLen, out uint pdwDocContext)
        {
            pdwDocContext = 0;
            return Constants.E_NOTIMPL;
        }

        // Retrieves a list of all code contexts associated with this document context.
        // The engine sample only supports one code context per document context and 
        // the code contexts are always memory addresses.
        int IDebugDocumentContext2.EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
        {
            ppEnumCodeCxts = null;
            return Constants.S_FALSE;
        }

        // Gets the document that contains this document context.
        // This method is for those debug engines that supply documents directly to the IDE. Since the sample engine
        // does not do this, this method returns E_NOTIMPL.
        int IDebugDocumentContext2.GetDocument(out IDebugDocument2 ppDocument)
        {
            ppDocument = null;
            return Constants.E_NOTIMPL;
        }

        // Gets the language associated with this document context.
        int IDebugDocumentContext2.GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = "JavaScript";
            pguidLanguage = AD7Guids.guidLanguageJs;
            return Constants.S_OK;
        }

        // Gets the displayable name of the document that contains this document context.
        int IDebugDocumentContext2.GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            pbstrFileName = m_fileName;
            return Constants.S_OK;
        }

        // Gets the source code range of this document context.
        // A source range is the entire range of source code, from the current statement back to just after the previous s
        // statement that contributed code. The source range is typically used for mixing source statements, including 
        // comments, with code in the disassembly window.
        // Sincethis engine does not support the disassembly window, this is not implemented.
        int IDebugDocumentContext2.GetSourceRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            throw new NotImplementedException("This method is not implemented");
        }

        // Gets the file statement range of the document context.
        // A statement range is the range of the lines that contributed the code to which this document context refers.
        int IDebugDocumentContext2.GetStatementRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            try {
                pBegPosition[0].dwColumn = m_begPos.dwColumn;
                pBegPosition[0].dwLine = m_begPos.dwLine;
                pEndPosition[0].dwColumn = m_endPos.dwColumn;
                pEndPosition[0].dwLine = m_endPos.dwLine;
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }

            return Constants.S_OK;
        }

        // Moves the document context by a given number of statements or lines.
        // This is used primarily to support the Autos window in discovering the proximity statements around 
        // this document context. 
        int IDebugDocumentContext2.Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
        {
            ppDocContext = null;
            return Constants.E_NOTIMPL;
        }

        #endregion
    }
}
