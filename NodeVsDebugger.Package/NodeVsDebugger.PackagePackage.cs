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
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NodeVsDebugger;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace NodeVsDebugger_Package
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    // NOTE: not really, actual texts are inside source.extension.vsixmanifest, not resources
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidNodeVsDebugger_PackagePkgString)]
    [NodeVsDebuggerRegistration]
    public sealed class NodeVsDebugger_PackagePackage : Package
    {
        EnvDTE80.DTE2 applicationObject;
        EnvDTE.OutputWindow defaultOutputWindow;
        EnvDTE.Command defaultDebugStart;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public NodeVsDebugger_PackagePackage()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            applicationObject = (EnvDTE80.DTE2)GetService(typeof(EnvDTE.DTE));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs) {
                // Create the command for the menu item.
                mcs.AddCommand(new MenuCommand(MenuItemCallbackAuto,
                    new CommandID(GuidList.guidNodeVsDebugger_PackageCmdSet, (int)PkgCmdIDList.cmdidNodeStartAuto)));
                mcs.AddCommand(new MenuCommand(MenuItemCallbackProject,
                    new CommandID(GuidList.guidNodeVsDebugger_PackageCmdSet, (int)PkgCmdIDList.cmdidNodeStartProject)));
                mcs.AddCommand(new MenuCommand(MenuItemCallbackDocument,
                    new CommandID(GuidList.guidNodeVsDebugger_PackageCmdSet, (int)PkgCmdIDList.cmdidNodeStartDocument)));
                mcs.AddCommand(new MenuCommand(MenuItemCallbackConfigure,
                    new CommandID(GuidList.guidNodeVsDebugger_PackageCmdSet, (int)PkgCmdIDList.cmdidNodeConfigure)));
            }
        }
        #endregion

        private void WriteOutput(string message)
        {
            if (defaultOutputWindow == null) {
                var window = applicationObject.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                defaultOutputWindow = (EnvDTE.OutputWindow)window.Object;
            }
            var pane = defaultOutputWindow.OutputWindowPanes.Item("Debug");
            defaultOutputWindow.Parent.Visible = true;
            pane.Activate();
            pane.OutputString(message + "\r\n");
        }

        private void ShowMessage(string message)
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0, ref clsid,
                "NodeVsDebugger", message,
                string.Empty, 0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO, 0, // false
                out result));
        }

        private void MenuItemCallbackConfigure(object sender, EventArgs e)
        {
            string dir;
            var result = GetActiveProjectDir(out dir);
            if (result != null) {
                WriteOutput("ERROR: " + result);
                return;
            }
            var fname = Path.Combine(dir, ".nodevsdbg");
            if (!File.Exists(fname)) {
                File.WriteAllText(fname, Resources.ConfigComment +
                    JsonConvert.SerializeObject(CreatDefaultConfig(dir), Formatting.Indented));
            }
            applicationObject.ItemOperations.OpenFile(fname, EnvDTE.Constants.vsViewKindTextView);
        }

        private object CreatDefaultConfig(string dir)
        {
            var main = FindAnyJs(new[] { dir });
            if (main != null && main.Length > dir.Length) {
                main = main.Substring(dir.Length).TrimStart(new[] { '/', '\\' });
                main = main.Replace('\\', '/');
            }
            return new { mode = "run", main = main ?? "no *.js files found", mappings = new { } };
        }

        private string FindAnyJs(IList<string> paths)
        {
            var thisLevelFile = paths.SelectMany(p => Directory.GetFiles(p, "*.js", SearchOption.TopDirectoryOnly)).FirstOrDefault();
            if (thisLevelFile != null)
                return thisLevelFile;
            var thisLevelDirs = paths.SelectMany(p => Directory.GetDirectories(p).Where(d => d[0] != '.')).ToList();
            if (thisLevelDirs.Count > 0)
                return FindAnyJs(thisLevelDirs);
            return null;
        }

        private string GetActiveProjectDir(out string result)
        {
            result = null;
            try {
                var solution = applicationObject.Solution;
                if (solution == null)
                    return "Solutuion not found";
                var projs = solution.SolutionBuild.StartupProjects as object[];
                if (projs == null || projs.Length < 1)
                    return "Startup project not found";
                if (projs.Length > 1)
                    return "Multiple startup projects not supported";
                var proj = solution.Projects.Item(projs[0]);
                if (proj == null)
                    return "Startup project is corrupted";
                result = proj.FullName;
                if (!File.Exists(result)) {
                    try {
                        result = proj.Properties.Item("FullPath").Value.ToString();
                    } catch {
                        result = Path.GetDirectoryName(solution.FullName);
                    }
                } else {
                    result = Path.GetDirectoryName(result);
                }
            } catch (Exception ex) {
                result = null;
                return ex.ToString();
            }
            return null;
        }

        private void MenuItemCallbackAuto(object sender, EventArgs e)
        {
            if (applicationObject.Mode == EnvDTE.vsIDEMode.vsIDEModeDebug) {
                StartDefaultDebugger();
                return;
            }
            if (!TryLaunchProject()) {
                StartDefaultDebugger();
            }
        }

        private void MenuItemCallbackProject(object sender, EventArgs e)
        {
            if (applicationObject.Mode == EnvDTE.vsIDEMode.vsIDEModeDebug) {
                WriteOutput("ERROR: Debug session is already active");
                return;
            }
            if (!TryLaunchProject()) {
                WriteOutput("ERROR: This project is not configured for Node.js");
            }
        }

        bool TryLaunchProject()
        {
            string dir;
            var error = GetActiveProjectDir(out dir);
            if (error != null) {
                WriteOutput("ERROR: " + error);
                return true;
            }
            JObject conf;
            error = ReadAndValidateConfig(dir, out conf);
            if (error != null) {
                WriteOutput("ERROR: " + error);
                return true;
            }
            if (conf != null) {
                LaunchDebugTarget(Path.Combine(dir, ".nodevsdbg"), JsonConvert.SerializeObject(conf));
                return true;
            }
            return false;
        }

        /// <summary></summary>
        /// <param name="dir"></param>
        /// <param name="conf">parsed and avalidated config, or null if not does not exist or disabled</param>
        /// <returns>error text</returns>
        private string ReadAndValidateConfig(string dir, out JObject conf)
        {
            conf = null;
            var confFile = Path.Combine(dir, ".nodevsdbg");
            if (!File.Exists(confFile))
                return null;
            try {
                // read and parse config now to detect errors early:
                conf = JsonConvert.DeserializeObject(File.ReadAllText(confFile)) as JObject;
                if (conf == null)
                    return "invalid contents of '.nodevsdbg' configuration file";
                switch ((string)conf["mode"]) {
                    case null:
                    case "run":
                        //validate main module
                        var main = (string)conf["main"];
                        if (main != null && !Path.IsPathRooted(main))
                            conf["main"] = main = Path.Combine(dir, main);
                        if (main == null || !File.Exists(main))
                            return "main module not found. Specified: " + JsonConvert.SerializeObject(conf["main"]);
                        break;
                    case "connect":
                        break;
                    case "off":
                        conf = null;
                        return null;
                    default:
                        return "unknown \"mode\": " + JsonConvert.SerializeObject(conf["mode"]);
                }
            } catch (JsonReaderException ex) {
                return "malformed config file. " + ex.Message;
            } catch (Exception ex) {
                return ex.ToString();
            }
            return null;
        }

        private void MenuItemCallbackDocument(object sender, EventArgs e)
        {
            if (applicationObject.Mode == EnvDTE.vsIDEMode.vsIDEModeDebug) {
                WriteOutput("ERROR: Debug session is already active");
                return;
            }
            var doc = applicationObject.ActiveDocument;
            if (doc != null && (doc.Language == "JavaScript" || doc.FullName.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))) {
                // TODO: should we save on debug?
                LaunchDebugTarget(doc.FullName, null);
            } else {
                WriteOutput("ERROR: A JavaScript document must be active");
            }
        }

        private void LaunchDebugTarget(string exe, string args)
        {
            var result = TryLaunchDebugTarget(exe, args);
            if (result != 0)
                WriteOutput(string.Format("ERROR: Cannot start debug, LaunchDebugTargets = 0x{0:X8}", result));
        }

        private int TryLaunchDebugTarget(string exe, string args)
        {
            var dbg = (IVsDebugger)GetService(typeof(SVsShellDebugger));

            var info = new VsDebugTargetInfo {
                cbSize = (uint)Marshal.SizeOf(typeof(VsDebugTargetInfo)),
                dlo = DEBUG_LAUNCH_OPERATION.DLO_Custom,

                bstrExe = exe,
                bstrCurDir = Path.GetDirectoryName(exe),
                bstrArg = args, // command line parameters
                bstrRemoteMachine = null, // debug locally
                fSendStdoutToOutputWindow = 0, // Let stdout stay with the application.
                clsidCustom = new Guid(NodeVsDebugger.Constants.DebuggerGuid), // Set the launching engine the sample engine guid
                grfLaunch = 0,
            };

            var pInfo = Marshal.AllocCoTaskMem((int)info.cbSize);
            Marshal.StructureToPtr(info, pInfo, false);

            try {
                return dbg.LaunchDebugTargets(1, pInfo);
            } finally {
                if (pInfo != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pInfo);
                }
            }
        }

        private void StartDefaultDebugger()
        {
            if (defaultDebugStart == null) {
                try {
                    defaultDebugStart = applicationObject.Commands.Item("Debug.Start");
                } catch { }
            }
            if (defaultDebugStart != null && defaultDebugStart.IsAvailable)
                applicationObject.ExecuteCommand(defaultDebugStart.Name);
        }
    }
}
