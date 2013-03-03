using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Runtime.InteropServices;

// This file contains the various event objects that are sent to the debugger from the sample engine via IDebugEventCallback2::Event.
// These are used in EngineCallback.cs.
// The events are how the engine tells the debugger about what is happening in the debuggee process. 
// There are three base classe the other events derive from: AD7AsynchronousEvent, AD7StoppingEvent, and AD7SynchronousEvent. These 
// each implement the IDebugEvent2.GetAttributes method for the type of event they represent. 
// Most events sent the debugger are asynchronous events.

namespace NodeVsDebugger
{
    #region Event base classes

    class AD7AsynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    class AD7StoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNC_STOP;
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    class AD7SynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    class AD7SynchronousStoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_STOPPING | (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;
        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    #endregion

    // The debug engine (DE) sends this interface to the session debug manager (SDM) when an instance of the DE is created.
    [Guid("FE5B734C-759D-4E59-AB04-F103343BDD06")]
    sealed class AD7EngineCreateEvent : AD7AsynchronousEvent, IDebugEngineCreateEvent2
    {
        private readonly IDebugEngine2 m_engine;
        public AD7EngineCreateEvent(AD7Engine engine)
        {
            m_engine = engine;
        }

        int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 engine)
        {
            engine = m_engine;
            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is attached to.
    [Guid("96CD11EE-ECD4-4E89-957E-B5D496FC4139")]
    sealed class AD7ProgramCreateEvent : AD7AsynchronousEvent, IDebugProgramCreateEvent2 { }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a module is loaded or unloaded.
    [Guid("989DB083-0D7C-40D1-A9D9-921BF611A4B2")]
    sealed class AD7ModuleLoadEvent : AD7AsynchronousEvent, IDebugModuleLoadEvent2
    {
        readonly AD7Module m_module;
        readonly bool m_fLoad;

        public AD7ModuleLoadEvent(AD7Module module, bool fLoad)
        {
            m_module = module;
            m_fLoad = fLoad;
        }

        int IDebugModuleLoadEvent2.GetModule(out IDebugModule2 module, ref string debugMessage, ref int fIsLoad)
        {
            module = m_module;
            if (m_fLoad) {
                debugMessage = String.Concat("Loaded '", m_module.m_script.Name, "'");
                fIsLoad = 1;
            } else {
                debugMessage = String.Concat("Unloaded '", m_module.m_script.Name, "'");
                fIsLoad = 0;
            }
            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program has run to completion
    // or is otherwise destroyed.
    [Guid("E147E9E3-6440-4073-A7B7-A65592C714B5")]
    sealed class AD7ProgramDestroyEvent : AD7SynchronousEvent, IDebugProgramDestroyEvent2
    {
        readonly uint m_exitCode;
        public AD7ProgramDestroyEvent(uint exitCode)
        {
            m_exitCode = exitCode;
        }

        int IDebugProgramDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = m_exitCode;
            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
    [Guid("2090CCFC-70C5-491D-A5E8-BAD2DD9EE3EA")]
    sealed class AD7ThreadCreateEvent : AD7AsynchronousEvent, IDebugThreadCreateEvent2 { }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread has exited.
    [Guid("2C3B7532-A36F-4A6E-9072-49BE649B8541")]
    sealed class AD7ThreadDestroyEvent : AD7AsynchronousEvent, IDebugThreadDestroyEvent2
    {
        readonly uint m_exitCode;
        public AD7ThreadDestroyEvent(uint exitCode)
        {
            m_exitCode = exitCode;
        }

        int IDebugThreadDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = m_exitCode;
            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is loaded, but before any code is executed.
    [Guid("B1844850-1349-45D4-9F12-495212F5EB0B")]
    sealed class AD7LoadCompleteEvent : AD7StoppingEvent, IDebugLoadCompleteEvent2 { }

    // This interface tells the session debug manager (SDM) that an asynchronous break has been successfully completed.
    [Guid("c7405d1d-e24b-44e0-b707-d8a5a4e1641b")]
    sealed class AD7AsyncBreakCompleteEvent : AD7StoppingEvent, IDebugBreakEvent2 { }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) to output a string for debug tracing.
    [Guid("569c4bb1-7b82-46fc-ae28-4536ddad753e")]
    sealed class AD7OutputDebugStringEvent : AD7AsynchronousEvent, IDebugOutputStringEvent2
    {
        private readonly string m_str;
        public AD7OutputDebugStringEvent(string str)
        {
            m_str = str;
        }

        int IDebugOutputStringEvent2.GetString(out string pbstrString)
        {
            pbstrString = m_str;
            return Constants.S_OK;
        }
    }

    // This interface is sent when a pending breakpoint has been bound in the debuggee.
    [Guid("1dddb704-cf99-4b8a-b746-dabb01dd13a0")]
    sealed class AD7BreakpointBoundEvent : AD7AsynchronousEvent, IDebugBreakpointBoundEvent2
    {
        private readonly AD7PendingBreakpoint m_pendingBreakpoint;
        private readonly AD7BoundBreakpoint m_boundBreakpoint;

        public AD7BreakpointBoundEvent(AD7PendingBreakpoint pendingBreakpoint, AD7BoundBreakpoint boundBreakpoint)
        {
            m_pendingBreakpoint = pendingBreakpoint;
            m_boundBreakpoint = boundBreakpoint;
        }

        int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = new AD7BoundBreakpointsEnum(new IDebugBoundBreakpoint2[] { m_boundBreakpoint });
            return Constants.S_OK;
        }

        int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = m_pendingBreakpoint;
            return Constants.S_OK;
        }
    }

    // This Event is sent when a breakpoint is hit in the debuggee
    [Guid("501C1E21-C557-48B8-BA30-A1EAB0BC4A74")]
    sealed class AD7BreakpointEvent : AD7StoppingEvent, IDebugBreakpointEvent2
    {
        readonly IEnumDebugBoundBreakpoints2 m_boundBreakpoints;
        public AD7BreakpointEvent(IEnumDebugBoundBreakpoints2 boundBreakpoints)
        {
            m_boundBreakpoints = boundBreakpoints;
        }

        int IDebugBreakpointEvent2.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = m_boundBreakpoints;
            return Constants.S_OK;
        }
    }
}
