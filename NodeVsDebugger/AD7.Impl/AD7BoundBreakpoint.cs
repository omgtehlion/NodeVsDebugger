using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    // This class represents a breakpoint that has been bound to a location in the debuggee. It is a child of the pending breakpoint
    // that creates it. Unless the pending breakpoint only has one bound breakpoint, each bound breakpoint is displayed as a child of the
    // pending breakpoint in the breakpoints window. Otherwise, only one is displayed.
    class AD7BoundBreakpoint : IDebugBoundBreakpoint2
    {
        private AD7PendingBreakpoint m_pendingBreakpoint;
        private readonly AD7Engine m_engine;
        private readonly int m_breakpointId;
        private bool m_enabled;
        private bool m_deleted;

        public AD7BoundBreakpoint(AD7Engine engine, int breakpointId, AD7PendingBreakpoint pendingBreakpoint)
        {
            m_engine = engine;
            m_breakpointId = breakpointId;
            m_pendingBreakpoint = pendingBreakpoint;
            m_enabled = true;
            m_deleted = false;
        }

        #region IDebugBoundBreakpoint2 Members

        // Called when the breakpoint is being deleted by the user.
        public int Delete()
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            if (!m_deleted) {
                m_deleted = true;
                m_engine.DebuggedProcess.RemoveBreakpoint(m_breakpointId);
                Debug.Assert(m_pendingBreakpoint != null);
                m_pendingBreakpoint.OnBoundBreakpointDeleted(this);
                m_pendingBreakpoint = null;
            }
            return Constants.S_OK;
        }

        // Called by the debugger UI when the user is enabling or disabling a breakpoint.
        public int Enable(int fEnable)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            Debug.Assert(!m_deleted);
            var enabled = fEnable != 0;
            if (m_enabled != enabled) {
                // A production debug engine would remove or add the underlying int3 here. The sample engine does not support true disabling
                // of breakpionts.
            }
            m_enabled = enabled;
            return Constants.S_OK;
        }

        // Return the breakpoint resolution which describes how the breakpoint bound in the debuggee.
        int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution)
        {
            ppBPResolution = null;
            return Constants.S_OK;
        }

        // Return the pending breakpoint for this bound breakpoint.
        int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = m_pendingBreakpoint;
            return Constants.S_OK;
        }

        // 
        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
        {
            pState[0] = 0;
            if (m_deleted) {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            } else if (m_enabled) {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            } else if (!m_enabled) {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }
            return Constants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit and return it here.
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = 0;
            return Constants.S_OK;
        }

        // The sample engine does not support conditions on breakpoints.
        // A real-world debugger will use this to specify when a breakpoint will be hit
        // and when it should be ignored.
        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            throw new NotImplementedException();
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit. The debugger calls SetHitCount when the user 
        // resets a breakpoint's hit count.
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            throw new NotImplementedException();
        }

        // The sample engine does not support pass counts on breakpoints.
        // This is used to specify the breakpoint hit count condition.
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
