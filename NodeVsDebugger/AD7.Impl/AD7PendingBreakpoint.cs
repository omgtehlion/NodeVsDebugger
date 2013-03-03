using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    // This class represents a pending breakpoint which is an abstract representation of a breakpoint before it is bound.
    // When a user creates a new breakpoint, the pending breakpoint is created and is later bound. The bound breakpoints
    // become children of the pending breakpoint.
    class AD7PendingBreakpoint : IDebugPendingBreakpoint2
    {
        // The breakpoint request that resulted in this pending breakpoint being created.
        private IDebugBreakpointRequest2 m_pBPRequest;
        private BP_REQUEST_INFO m_bpRequestInfo;
        private AD7Engine m_engine;
        private List<AD7BoundBreakpoint> m_boundBreakpoints = new List<AD7BoundBreakpoint>();
        private bool m_enabled;
        private bool m_deleted;

        public AD7PendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, AD7Engine engine)
        {
            m_pBPRequest = pBPRequest;
            var requestInfo = new BP_REQUEST_INFO[1];
            EngineUtils.CheckOk(m_pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION, requestInfo));
            m_bpRequestInfo = requestInfo[0];
            m_engine = engine;
            m_enabled = true;
            m_deleted = false;
        }

        private bool CanBind()
        {
            // The sample engine only supports breakpoints on a file and line number. No other types of breakpoints are supported.
            if (m_deleted || m_bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
                return false;
            return true;
        }

        // Remove all of the bound breakpoints for this pending breakpoint
        public void ClearBoundBreakpoints()
        {
            lock (this)
                m_boundBreakpoints.ToList().ForEach(b => b.Delete());
        }

        // Called by bound breakpoints when they are being deleted.
        public void OnBoundBreakpointDeleted(AD7BoundBreakpoint boundBreakpoint)
        {
            lock (this)
                m_boundBreakpoints.Remove(boundBreakpoint);
        }

        #region IDebugPendingBreakpoint2 Members

        // Binds this pending breakpoint to one or more code locations.
        int IDebugPendingBreakpoint2.Bind()
        {
            try {
                if (CanBind()) {
                    var docPosition = (IDebugDocumentPosition2)Marshal.GetObjectForIUnknown(m_bpRequestInfo.bpLocation.unionmember2);

                    // Get the name of the document that the breakpoint was put in
                    string documentName;
                    EngineUtils.CheckOk(docPosition.GetFileName(out documentName));

                    // Get the location in the document that the breakpoint is in.
                    var startPosition = new TEXT_POSITION[1];
                    var endPosition = new TEXT_POSITION[1];
                    EngineUtils.CheckOk(docPosition.GetRange(startPosition, endPosition));

                    var id = m_engine.DebuggedProcess.SetBreakpoint(documentName, startPosition[0].dwLine, startPosition[0].dwColumn);
                    if (id == -1)
                        return Constants.S_FALSE;
                    lock (this)
                        m_boundBreakpoints.Add(new AD7BoundBreakpoint(m_engine, id, this));

                    return Constants.S_OK;
                } else {
                    // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid expression, etc...
                    // The sample engine does not support this, but a real world engine will want to send an instance of IDebugBreakpointErrorEvent2 to the
                    // UI and return a valid instance of IDebugErrorBreakpoint2 from IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then
                    // display information about why the breakpoint did not bind to the user.
                    return Constants.S_FALSE;
                }
            } catch (Exception e) {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Determines whether this pending breakpoint can bind to a code location.
        int IDebugPendingBreakpoint2.CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
        {
            ppErrorEnum = null;
            if (!CanBind()) {
                // Called to determine if a pending breakpoint can be bound. 
                // The breakpoint may not be bound for many reasons such as an invalid location, an invalid expression, etc...
                // The sample engine does not support this, but a real world engine will want to return a valid enumeration of IDebugErrorBreakpoint2.
                // The debugger will then display information about why the breakpoint did not bind to the user.
                ppErrorEnum = null;
                return Constants.S_FALSE;
            }
            return Constants.S_OK;
        }

        // Deletes this pending breakpoint and all breakpoints bound from it.
        int IDebugPendingBreakpoint2.Delete()
        {
            ClearBoundBreakpoints();
            return Constants.S_OK;
        }

        // Toggles the enabled state of this pending breakpoint.
        int IDebugPendingBreakpoint2.Enable(int fEnable)
        {
            lock (this) {
                m_enabled = fEnable != 0;
                m_boundBreakpoints.ForEach(b => b.Enable(fEnable));
            }
            return Constants.S_OK;
        }

        // Enumerates all breakpoints bound from this pending breakpoint
        int IDebugPendingBreakpoint2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            lock (this)
                ppEnum = new AD7BoundBreakpointsEnum(m_boundBreakpoints.ToArray());
            return Constants.S_OK;
        }

        // Enumerates all error breakpoints that resulted from this pending breakpoint.
        int IDebugPendingBreakpoint2.EnumErrorBreakpoints(enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
        {
            // Called when a pending breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid expression, etc...
            // The sample engine does not support this, but a real world engine will want to send an instance of IDebugBreakpointErrorEvent2 to the
            // UI and return a valid enumeration of IDebugErrorBreakpoint2 from IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then
            // display information about why the breakpoint did not bind to the user.
            ppEnum = null;
            return Constants.E_NOTIMPL;
        }

        // Gets the breakpoint request that was used to create this pending breakpoint
        int IDebugPendingBreakpoint2.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
        {
            ppBPRequest = this.m_pBPRequest;
            return Constants.S_OK;
        }

        // Gets the state of this pending breakpoint.
        int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (m_deleted) {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DELETED;
            } else if (m_enabled) {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_ENABLED;
            } else if (!m_enabled) {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DISABLED;
            }

            return Constants.S_OK;
        }

        // The sample engine does not support conditions on breakpoints.
        int IDebugPendingBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            throw new NotImplementedException();
        }

        // The sample engine does not support pass counts on breakpoints.
        int IDebugPendingBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            throw new NotImplementedException();
        }

        // The sample engine will does not support this.
        int IDebugPendingBreakpoint2.Virtualize(int fVirtualize)
        {
            return Constants.S_OK;
        }

        #endregion
    }
}
