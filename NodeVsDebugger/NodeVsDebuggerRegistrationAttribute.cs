using Microsoft.VisualStudio.Shell;
using System;
using System.IO;

namespace NodeVsDebugger
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class NodeVsDebuggerRegistrationAttribute : RegistrationAttribute
    {
        readonly Type[] typesToRegister = new[] { typeof(AD7Engine), typeof(AD7ProgramProvider) };

        public NodeVsDebuggerRegistrationAttribute()
        {
        }

        public override void Register(RegistrationContext context)
        {
            using (var guid = context.CreateKey(@"AD7Metrics\Engine\" + new Guid(Constants.DebuggerGuid).ToString("B"))) {
                guid.SetValue("", Constants.DebuggerIdString);
                guid.SetValue("CLSID", typeof(AD7Engine).GUID.ToString("B"));
                guid.SetValue("ProgramProvider", typeof(AD7ProgramProvider).GUID.ToString("B"));
                guid.SetValue("Attach", 1);
                guid.SetValue("AddressBP", 0);
                guid.SetValue("AutoSelectPriority", 4);
                //engineKey.SetValue("Exceptions", 1);
                //engineKey.SetValue("RemoteDebugging", 1);
                guid.SetValue("CallstackBP", 1);
                guid.SetValue("Name", Constants.EngineName);
                guid.SetValue("PortSupplier", "{708C1ECA-FF48-11D2-904F-00C04FA302A1}");
                guid.SetValue("AlwaysLoadProgramProviderLocal", 1);
                using (var incompat = guid.CreateSubkey("IncompatibleList")) {
                    incompat.SetValue("guidCOMPlusNativeEng", "{92EF0900-2251-11D2-B72E-0000F87572EF}");
                    incompat.SetValue("guidCOMPlusOnlyEng", "{449EC4CC-30D2-4032-9256-EE18EB41B62B}");
                    incompat.SetValue("guidNativeOnlyEng", "{449EC4CC-30D2-4032-9256-EE18EB41B62B}");
                    incompat.SetValue("guidScriptEng", "{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}");
                }
            }
            foreach (var t in typesToRegister) {
                using (var tkey = context.CreateKey(@"CLSID\" + t.GUID.ToString("B"))) {
                    tkey.SetValue("Assembly", t.Assembly.FullName);
                    tkey.SetValue("Class", t.FullName);
                    tkey.SetValue("InprocServer32", context.InprocServerPath);
                    //System.IO.Path.Combine(System.Environment.SystemDirectory, "mscoree.dll"));
                    //tkey.SetValue("CodeBase", t.Assembly.Location);
                    tkey.SetValue("CodeBase", Path.Combine(context.ComponentPath, t.Module.Name));
                }
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(@"AD7Metrics\Engine\" + new Guid(Constants.DebuggerGuid).ToString("B"));
            foreach (var t in typesToRegister) {
                context.RemoveKey(@"CLSID\" + t.GUID.ToString("B"));
            }
        }
    }
}
