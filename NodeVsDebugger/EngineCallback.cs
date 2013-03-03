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
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Interop;

namespace NodeVsDebugger
{
    public class EngineCallback
    {
        readonly IDebugEventCallback2 m_ad7Callback;
        readonly AD7Engine m_engine;

        public EngineCallback(AD7Engine engine, IDebugEventCallback2 ad7Callback)
        {
            m_ad7Callback = ad7Callback;
            m_engine = engine;
        }

        public void Send(IDebugEvent2 eventObject, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes;
            var riidEvent = eventObject.GetType().GUID;
            EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));
            EngineUtils.RequireOk(m_ad7Callback.Event(m_engine, null, program, thread, eventObject, ref riidEvent, attributes));
        }

        public void Send(IDebugEvent2 eventObject, IDebugThread2 thread)
        {
            Send(eventObject, m_engine, thread);
        }

        #region ISampleEngineCallback Members

        public void OnModuleLoad(NodeScript debuggedModule)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a mod-load event
            // for the exe.
            if (m_engine.DebuggedProcess != null) {
                //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            }
            var ad7Module = new AD7Module(debuggedModule);
            //debuggedModule.Client = ad7Module;
            // The sample engine does not support binding breakpoints as modules load since the primary exe is the only module
            // symbols are loaded for. A production debugger will need to bind breakpoints when a new module is loaded.
            Send(new AD7ModuleLoadEvent(ad7Module, true /* this is a module load */), null);
        }

        public void OnModuleUnload(NodeScript debuggedModule)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            //AD7Module ad7Module = (AD7Module)debuggedModule.Client;
            //Debug.Assert(ad7Module != null);
            //AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(ad7Module, false /* this is a module unload */);
            //Send(eventObject, null);
        }

        public void OnOutputString(string outputString)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            Send(new AD7OutputDebugStringEvent(outputString), null);
        }

        public void OnProcessExit(uint exitCode)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            Send(new AD7ProgramDestroyEvent(exitCode), null);
        }

        public void OnThreadExit(DebuggedThread debuggedThread, uint exitCode)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            var ad7Thread = debuggedThread.Client;
            Debug.Assert(ad7Thread != null);
            Send(new AD7ThreadDestroyEvent(exitCode), ad7Thread);
        }

        public void OnThreadStart(DebuggedThread debuggedThread)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a thread start event
            // for the main thread of the application.
            if (m_engine.DebuggedProcess != null) {
                //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            }
            var ad7Thread = new AD7Thread(m_engine, debuggedThread);
            debuggedThread.Client = ad7Thread;
            Send(new AD7ThreadCreateEvent(), ad7Thread);
        }

        public void OnBreakpoint(DebuggedThread thread, ReadOnlyCollection<object> clients, uint address)
        {
            // An engine that supports more advanced breakpoint features such as hit counts, conditions and filters
            // should notify each bound breakpoint that it has been hit and evaluate conditions here.
            // The sample engine does not support these features.
            var boundBreakpointsEnum = new AD7BoundBreakpointsEnum(clients.Select(c => (IDebugBoundBreakpoint2)c));
            Send(new AD7BreakpointEvent(boundBreakpointsEnum), thread.Client);
        }

        public void OnException(DebuggedThread thread, uint code)
        {
            // Exception events are sent when an exception occurs in the debuggee that the debugger was not expecting.
            // The sample engine does not support these.
            throw new Exception("The method or operation is not implemented.");
        }

        public void OnStepComplete(DebuggedThread thread)
        {
            // Step complete is sent when a step has finished. The sample engine does not support stepping.
            throw new Exception("The method or operation is not implemented.");
        }

        public void OnAsyncBreakComplete(DebuggedThread thread)
        {
            // This will get called when the engine receives the breakpoint event that is created when the user
            // hits the pause button in vs.
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            Send(new AD7AsyncBreakCompleteEvent(), thread.Client);
        }

        public void OnLoadComplete(DebuggedThread thread)
        {
            Send(new AD7LoadCompleteEvent(), thread.Client);
        }

        public void OnProgramDestroy(uint exitCode)
        {
            Send(new AD7ProgramDestroyEvent(exitCode), null);
        }

        // Engines notify the debugger that a breakpoint has bound through the breakpoint bound event.
        public void OnBreakpointBound(object objBoundBreakpoint, uint address)
        {
            var boundBreakpoint = (AD7BoundBreakpoint)objBoundBreakpoint;
            IDebugPendingBreakpoint2 pendingBreakpoint;
            ((IDebugBoundBreakpoint2)boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);
            Send(new AD7BreakpointBoundEvent((AD7PendingBreakpoint)pendingBreakpoint, boundBreakpoint), null);
        }

        #endregion
    }
}
