using System;
using System.IO;
using System.Linq;

/// <summary>
/// This class contains methods that build, line by line, the various files that are a part of a package. 
/// </summary>

namespace PackageManager
{
    public class FileBuilder
    {
        private readonly Logger Logger;
        private readonly PackageData PackageData;

        public FileBuilder(PackageData packageData)
        {
            Logger = Logger.LoggerInstance;
            PackageData = packageData;
        }

        /// <summary>
        /// Build the change log file line by line. 
        /// </summary>
        /// <param name="changeLogFile">The change log file</param>
        public void BuildChangeLog(string changeLogFile)
        {
            Logger.Log("Building changelog.txt file");

            if (new FileInfo(changeLogFile).Length == 0)
            {
                using (var sw = new StreamWriter(changeLogFile, true))
                {
                    sw.WriteLine("RSO Package: " + PackageData.PackageName);
                    sw.WriteLine();
                    sw.WriteLine(PackageData.Date + " v1");
                    sw.WriteLine(" - initial version");
                }
                Logger.Log("Changelog.txt successfully built");
            }
        }

        /// <summary>
        /// Build the touch.cmd script. 
        /// </summary>S
        /// <param name="touchScript">The touch.cmd script</param>
        public void BuildTouchScript(string touchScript)
        {
            Logger.Log("Creating touch.cmd script");

            if (new FileInfo(touchScript).Length == 0)
            {
                using (var sw = new StreamWriter(touchScript, true))
                {
                    sw.Write(PackageManager.Properties.Resources.touch);
                    Logger.Log("Touch script sucessfully created.");
                }
            }
        }

        /// <summary>
        ///  Build the root readme.md file line by line with input parameters. 
        /// </summary>
        /// <param name="rootReadMe">The root readme.md file</param>
        public void BuildRootReadMe(string rootReadMe)
        {
            Logger.Log("Building root readme.md file");
            if (new FileInfo(rootReadMe).Length == 0)
            {
                using (var sw = new StreamWriter(rootReadMe, true))
                {
                    sw.WriteLine("# ReadMe - About " + PackageData.PackageName.TrimStart());
                    sw.WriteLine();
                    sw.WriteLine(PackageData.PackageDescription.TrimStart());
                    sw.WriteLine();
                    sw.WriteLine("* Business Item: " + PackageData.BusinessItem.TrimStart());

                    if (PackageData.HasRollback)
                    {
                        sw.WriteLine();
                        sw.WriteLine("Rollback instructions are provided in the `rollback` folder.");
                    }
                }
                Logger.Log("Root readme.md file successfully built");
            }
        }

        /// <summary>
        /// Build the install folder readme.md file with input parameters. 
        /// </summary>
        /// <param name="installReadMe">The install folder readme.md file</param>
        public void BuildInstallReadMe(string installReadMe)
        {
            Logger.Log("Building the install readme.md file");
            if (new FileInfo(installReadMe).Length == 0)
            {
                using (var sw = new StreamWriter(installReadMe, true))
                {
                    sw.WriteLine("# ReadMe - " + PackageData.PackageName.TrimStart() + " Installation");
                    sw.WriteLine();
                    sw.WriteLine("## Description");
                    sw.Write("This process will stage the ");

                    string[] installFiles = new string[0];
                    string namingSyntax = "files";

                    OutputDeterminator(
                    () =>
                    {
                        installFiles = PackageData.InstallForms.Concat(PackageData.InstallFirmware).ToArray();
                    },
                    () =>
                    {
                        installFiles = PackageData.InstallForms;
                        namingSyntax = PackageData.InstallForms.Length > 1 ? "forms" : "form";
                    },
                    () =>
                    {
                        installFiles = PackageData.InstallFirmware;
                        namingSyntax = PackageData.InstallFirmware.Length > 1 ? "files" : "file";
                    });

                    if (installFiles.Length > 1)
                    {
                        for (int i = installFiles.Length - 1; i >= 0; i--)
                        {
                            string fileName = Path.GetFileName(installFiles[i]);

                            if (i > 0)
                            {
                                sw.Write("`" + fileName + "`, ");
                            }
                            else if (i == 0)
                            {
                                sw.Write("and `" + fileName + "` ");
                            }
                        }
                        sw.Write("{0} for installation to the Ingenico iSC350 CDU via the NCRDiag utility", namingSyntax);
                        sw.WriteLine();
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.Write("`" + Path.GetFileName(installFiles[0]) + "` " + namingSyntax + " for installation to the Ingenico iSC350 CDU via the NCRDiag utility");
                        sw.WriteLine();
                        sw.WriteLine();
                    }

                    sw.WriteLine("## Pre-Requisites");
                    string[] preReqs = PackageData.Prerequisites;
                    for (int i = 0; i < preReqs.Length; i++)
                    {
                        sw.WriteLine("* " + preReqs[i]);
                    }
                    sw.WriteLine();
                    sw.WriteLine("## Installation Process");

                    int lineNumber = 1;
                    sw.WriteLine("{0}. Kill desktop shell & desktop", lineNumber);

                    OutputDeterminator(
                    () =>
                    {
                        sw.WriteLine("{0}. Update the timestamp (touch) on the {1} included in the `Forms` folder", ++lineNumber, namingSyntax);
                        sw.WriteLine("{0}. Copy the contents of the included `Forms` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms`", ++lineNumber);
                        sw.WriteLine("{0}. Copy the contents of the included `Firmware` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware`", ++lineNumber);
                    },
                    () =>
                    {
                        sw.WriteLine("{0}. Update the timestamp (touch) on the {1} included in the `Forms` folder", ++lineNumber, namingSyntax);
                        sw.WriteLine("{0}. Copy the contents of the included `Forms` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms`", ++lineNumber);
                    },
                    () =>
                    {
                        sw.WriteLine("{0}. Copy the contents of the included `Firmware` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware`", ++lineNumber);
                    });

                    sw.WriteLine("{0}. Append text `[DATE TIME]RSO:" + PackageData.PackageName + " package installed` to `C:\\NCRDiagnostics\\DeviceData\\iSC350\\config.log` (replacing DATE and TIME with actual date and time stamps", ++lineNumber);
                    sw.WriteLine("{0}. Reboot the machine", ++lineNumber);
                    sw.WriteLine();
                    sw.WriteLine("Upon booting, the machine will install the {0} to the CDU as part of the NCRDiag startup step.", namingSyntax);
                }
                Logger.Log("Install readme.md successfully built");
            }
        }

        /// <summary>
        /// Build the install.cmd script line by line with input parameters. 
        /// </summary>
        /// <param name="installScript">The install script</param>
        public void BuildInstallScript(string installScript)
        {
            Logger.Log("Building install.cmd script");
            if (new FileInfo(installScript).Length == 0)
            {
                using (var sw = new StreamWriter(installScript, true))
                {
                    sw.WriteLine("@Echo Off");
                    sw.WriteLine("setLocal enableDelayedExpansion");
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine(":: example/test script for installation of the " + PackageData.PackageName + " package");
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine("set \"ERRLEV=0\"");
                    sw.WriteLine("set \"SCRIPTHOME=%~dp0\"");
                    sw.WriteLine("set \"SCRIPTNAME=%~nx1\"");

                    if (PackageData.HasForms) sw.WriteLine("set \"FORMSDIR=C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms\"");
                    if (PackageData.HasFirmware) sw.WriteLine("set \"FIRMWAREDIR=C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware\"");

                    sw.WriteLine();
                    sw.WriteLine(":KILL_RSS");
                    sw.WriteLine("echo:Killing desktopshell...");
                    sw.WriteLine("taskkill /F /FI \"IMAGENAME eq desktopshell.exe\"");
                    sw.WriteLine();
                    sw.WriteLine("echo:Killing desktop...");
                    sw.WriteLine("taskkill /F /FI \"IMAGENAME eq desktop.exe\"");
                    sw.WriteLine();
                    sw.WriteLine(":COPY_FILES");
                    sw.WriteLine("echo:Copying files...");

                    if (PackageData.HasForms) sw.Write("xcopy /y \"!SCRIPTHOME!Forms\\*\" \"!FORMSDIR!\"");
                    if (PackageData.HasFirmware) sw.Write(" && xcopy /y \"!SCRIPTHOME!Firmware\\*\" \"!FIRMWAREDIR!\"");

                    sw.Write(" && goto :UPDATE_TIMESTAMP");
                    sw.WriteLine();
                    sw.WriteLine();
                    sw.WriteLine(":: Handle filecopy failure");
                    sw.WriteLine("set ERRLEV=1");
                    sw.WriteLine("1>&2 echo:ERROR: failed to copy files");
                    sw.WriteLine("goto :ERR");
                    sw.WriteLine();

                    if (PackageData.HasForms)
                    {
                        sw.WriteLine(":UPDATE_TIMESTAMP");
                        foreach (string form in PackageData.InstallForms)
                        {
                            sw.WriteLine(":: Update timestamp on " + Path.GetFileName(form));
                            sw.WriteLine("call \"!SCRIPTHOME!..\\scripts\\touch.cmd\" \"!FORMSDIR!\\" + Path.GetFileName(form) + "\" || goto :TOUCH_ERR");
                        }
                    }

                    sw.WriteLine();
                    sw.WriteLine("goto :LOG_PACKAGE_INSTALL");
                    sw.WriteLine();
                    sw.WriteLine(":TOUCH_ERR");
                    sw.WriteLine("set ERRLEV=2");
                    sw.WriteLine("1>&2 echo:ERROR: failed to update timestamp");
                    sw.WriteLine("goto :ERR");
                    sw.WriteLine();
                    sw.WriteLine(":LOG_PACKAGE_INSTALL");
                    sw.WriteLine("echo:Logging config change...");
                    sw.WriteLine("call :GET_DATESTAMP");
                    sw.WriteLine("echo:[!DATESTAMP! !TIME!]RSO:" + PackageData.PackageName + " package installed >> \"C:\\NCRDiagnostics\\DeviceData\\iSC350\\config.log\"");
                    sw.WriteLine();
                    sw.WriteLine(":REBOOT_TERMINAL");
                    sw.WriteLine("echo:Scheduling reboot...");
                    sw.WriteLine("shutdown.exe -r -d p:4:1 -c \"install " + PackageData.PackageName + " package\" -t 30");
                    sw.WriteLine();
                    sw.WriteLine("goto :END");
                    sw.WriteLine();
                    sw.WriteLine(":ERR");
                    sw.WriteLine("if \"!ERRLEV!\"==\"0\" (");
                    sw.WriteLine("    set ERRLEV=1");
                    sw.WriteLine("    1>&2 echo:ERROR: !SCRIPTNAME! failed");
                    sw.WriteLine(")");
                    sw.WriteLine("goto :END");
                    sw.WriteLine();
                    sw.WriteLine(":END");
                    sw.WriteLine("exit /b %ERRLEV%");
                    sw.WriteLine();
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine(":GET_DATESTAMP");
                    sw.WriteLine("call :PROCESS_DATE %DATE:/= %");
                    sw.WriteLine("exit /b");
                    sw.WriteLine();
                    sw.WriteLine(":PROCESS_DATE");
                    sw.WriteLine("set \"DATESTAMP=%4.%2.%3\"");
                    sw.WriteLine("exit /b");
                }
                Logger.Log("Install.cmd script successfully built");
            }
        }

        /// <summary>
        /// Build the rollback folder (if applicable) readme.md file line by line with input parameters. 
        /// </summary>
        /// <param name="rollbackReadMe">The rollback readme.md file</param>
        public void BuildRollbackReadMe(string rollbackReadMe)
        {
            Logger.Log("Building rollback readme.md file");
            if (new FileInfo(rollbackReadMe).Length == 0)
            {
                using (var sw = new StreamWriter(rollbackReadMe, true))
                {
                    sw.WriteLine("# ReadMe - " + PackageData.PackageName.TrimStart() + " Rollback");
                    sw.WriteLine();
                    sw.WriteLine("## Description");
                    sw.Write("This process will rollback the ");

                    string[] rollbackFiles = new string[0];
                    string namingSyntax = "files";

                    OutputDeterminator(
                    () =>
                    {
                        rollbackFiles = PackageData.RollbackForms.Concat(PackageData.RollbackFirmware).ToArray();
                    },
                    () =>
                    {
                        rollbackFiles = PackageData.RollbackForms;
                        namingSyntax = PackageData.RollbackForms.Length > 1 ? "forms" : "form";
                    },
                    () =>
                    {
                        rollbackFiles = PackageData.RollbackFirmware;
                        namingSyntax = PackageData.RollbackFirmware.Length > 1 ? "files" : "file";
                    });

                    if (rollbackFiles.Length > 1)
                    {
                        for (int i = rollbackFiles.Length - 1; i >= 0; i--)
                        {
                            string fileName = Path.GetFileName(rollbackFiles[i]);

                            if (i > 0)
                            {
                                sw.Write("`" + fileName + "`,");
                            }
                            else if (i == 0)
                            {
                                sw.Write(" and `" + fileName + "` ");
                            }
                        }
                        sw.Write("{0} for installation to the Ingenico iSC350 CDU via the NCRDiag utility", namingSyntax);
                        sw.WriteLine();
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.Write("`" + Path.GetFileName(rollbackFiles[0]) + "` " + namingSyntax + " for installation to the Ingenico iSC350 CDU via the NCRDiag utility");
                        sw.WriteLine();
                        sw.WriteLine();
                    }

                    sw.WriteLine("## Rollback Process");
                    int lineNumber = 1;
                    sw.WriteLine("{0}. Kill desktop shell & desktop", lineNumber);

                    OutputDeterminator(
                    () =>
                    {
                        sw.WriteLine("{0}. Update the timestamp (touch) on the {1} included in the `Forms` folder", ++lineNumber, namingSyntax);
                        sw.WriteLine("{0}. Copy the contents of the included `Forms` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms`", ++lineNumber);
                        sw.WriteLine("{0}. Copy the contents of the included `Firmware` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware`", ++lineNumber);
                    },
                    () =>
                    {
                        sw.WriteLine("{0}. Update the timestamp (touch) on the {1} included in the `Forms` folder", ++lineNumber, namingSyntax);
                        sw.WriteLine("{0}. Copy the contents of the included `Forms` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms`", ++lineNumber);
                    },
                    () =>
                    {
                        sw.WriteLine("{0}. Copy the contents of the included `Firmware` folder into `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware`", ++lineNumber);
                    });

                    sw.WriteLine("{0}. Append text `[DATE TIME]RSO:" + PackageData.PackageName + " package rollback applied` to `C:\\NCRDiagnostics\\DeviceData\\iSC350\\config.log` (replacing DATE and TIME with actual date and time stamps", ++lineNumber);
                    sw.WriteLine("{0}. Reboot the machine", ++lineNumber);
                    sw.WriteLine();
                    sw.WriteLine("Upon booting, the machine will install the previous version of the {0} to the CDU as part of the NCRDiag startup step.", namingSyntax);
                }

                Logger.Log("Rollback readme.md file successfully built");
            }
        }

        /// <summary>
        /// Build the rollback.cmd script (if applicable) line by line with input parameters. 
        /// </summary>
        /// <param name="rollbackScript">The rollback script.</param>
        public void BuildRollbackScript(string rollbackScript)
        {
            Logger.Log("Building rollback.cmd script");
            if (new FileInfo(rollbackScript).Length == 0)
            {
                using (var sw = new StreamWriter(rollbackScript, true))
                {
                    sw.WriteLine("@Echo Off");
                    sw.WriteLine("setLocal enableDelayedExpansion");
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine(":: example/test script for rollback of the " + PackageData.PackageName + " package");
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine("set \"ERRLEV=0\"");
                    sw.WriteLine("set \"SCRIPTHOME=%~dp0\"");
                    sw.WriteLine("set \"SCRIPTNAME=%~nx1\"");

                    if (PackageData.HasForms) sw.WriteLine("set \"FORMSDIR=C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms\"");
                    if (PackageData.HasFirmware) sw.WriteLine("set \"FIRMWAREDIR=C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware\"");

                    sw.WriteLine();
                    sw.WriteLine(":KILL_RSS");
                    sw.WriteLine("echo:Killing desktopshell...");
                    sw.WriteLine("taskkill /F /FI \"IMAGENAME eq desktopshell.exe\"");
                    sw.WriteLine();
                    sw.WriteLine("echo:Killing desktop...");
                    sw.WriteLine("taskkill /F /FI \"IMAGENAME eq desktop.exe\"");
                    sw.WriteLine();
                    sw.WriteLine(":COPY_FILES");
                    sw.WriteLine("echo:Copying files...");

                    if (PackageData.HasForms) sw.Write("xcopy /y \"!SCRIPTHOME!Forms\\*\" \"!FORMSDIR!\"");
                    if (PackageData.HasFirmware) sw.Write(" && xcopy /y \"!SCRIPTHOME!Firmware\\*\" \"!FIRMWAREDIR!\"");

                    sw.Write(" && goto :UPDATE_TIMESTAMP");
                    sw.WriteLine();
                    sw.WriteLine();
                    sw.WriteLine(":: Handle filecopy failure");
                    sw.WriteLine("set ERRLEV=1");
                    sw.WriteLine("1>&2 echo:ERROR: failed to copy files");
                    sw.WriteLine("goto :ERR");
                    sw.WriteLine();

                    if (PackageData.HasForms)
                    {
                        sw.WriteLine(":UPDATE_TIMESTAMP");
                        foreach (string form in PackageData.RollbackForms)
                        {
                            sw.WriteLine(":: Update timestamp on " + Path.GetFileName(form));
                            sw.WriteLine("call \"!SCRIPTHOME!..\\scripts\\touch.cmd\" \"!FORMSDIR!\\" + Path.GetFileName(form) + "\" || goto :TOUCH_ERR");
                        }
                    }

                    sw.WriteLine();
                    sw.WriteLine("goto :LOG_PACKAGE_INSTALL");
                    sw.WriteLine();
                    sw.WriteLine(":TOUCH_ERR");
                    sw.WriteLine("set ERRLEV=2");
                    sw.WriteLine("1>&2 echo:ERROR: failed to update timestamp");
                    sw.WriteLine("goto :ERR");
                    sw.WriteLine();
                    sw.WriteLine(":LOG_PACKAGE_INSTALL");
                    sw.WriteLine("echo:Logging config change...");
                    sw.WriteLine("call :GET_DATESTAMP");
                    sw.WriteLine("echo:[!DATESTAMP! !TIME!]RSO:" + PackageData.PackageName + " package rolled back >> \"C:\\NCRDiagnostics\\DeviceData\\iSC350\\config.log\"");
                    sw.WriteLine();
                    sw.WriteLine(":REBOOT_TERMINAL");
                    sw.WriteLine("echo:Scheduling reboot...");
                    sw.WriteLine("shutdown.exe -r -d p:4:1 -c \"rollback " + PackageData.PackageName + " package\" -t 30");
                    sw.WriteLine();
                    sw.WriteLine("goto :END");
                    sw.WriteLine();
                    sw.WriteLine(":ERR");
                    sw.WriteLine("if \"!ERRLEV!\"==\"0\" (");
                    sw.WriteLine("    set ERRLEV=1");
                    sw.WriteLine("    1>&2 echo:ERROR: !SCRIPTNAME! failed");
                    sw.WriteLine(")");
                    sw.WriteLine("goto :END");
                    sw.WriteLine();
                    sw.WriteLine(":END");
                    sw.WriteLine("exit /b %ERRLEV%");
                    sw.WriteLine();
                    sw.WriteLine(":::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
                    sw.WriteLine(":GET_DATESTAMP");
                    sw.WriteLine("call :PROCESS_DATE %DATE:/= %");
                    sw.WriteLine("exit /b");
                    sw.WriteLine();
                    sw.WriteLine(":PROCESS_DATE");
                    sw.WriteLine("set \"DATESTAMP=%4.%2.%3\"");
                    sw.WriteLine("exit /b");
                }
                Logger.Log("Rollback.cmd script successfully built");
            }
        }

        /// <summary>
        /// Build the verify.md file with input parameters. 
        /// </summary>
        /// <param name="verifyReadMe">The verify readme.md file</param>
        public void BuildVerifyReadMe(string verifyReadMe)
        {
            Logger.Log("Building verify readme.md file");
            if (new FileInfo(verifyReadMe).Length == 0)
            {
                using (var sw = new StreamWriter(verifyReadMe, true))
                {
                    sw.WriteLine("# ReadMe - " + PackageData.PackageName.TrimStart() + " Verification");
                    sw.WriteLine();
                    sw.WriteLine("To verify that files have been staged correctly:");
                    sw.WriteLine();

                    OutputDeterminator(
                    () =>
                    {
                        sw.WriteLine("Ensure that the md5 hash of the files as they appear in `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms` " +
                            "and `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware` match the md5 hashes listed in `" + PackageData.PackageName + ".manifest.json`");
                    },
                    () =>
                    {
                        sw.WriteLine("Ensure that the md5 hash of the forms as they appear in `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Forms` matches the " +
                            "md5 hashes listed in `" + PackageData.PackageName + ".manifest.json`");
                    },
                    () =>
                    {
                        sw.WriteLine("Ensure that the md5 hash of the files as they appear in `C:\\NCRDiagnostics\\DeviceData\\iSC350\\Firmware` matches the " +
                             "md5 hashes listed in `" + PackageData.PackageName + ".manifest.json`");
                    });
                }

                Logger.Log("Verify readme.md file successfully built");
            }
        }

        /// <summary>
        /// The purpose of this method is to peform actions based on the type of files that the package includes. Previously, I was 
        /// doing the boolean comparisons you see here in 5 different places. This method is meant to centralize that comparison logic.
        /// </summary>
        /// <param name="action1"> The action to be performed if the package has both form and firmware files. </param>
        /// <param name="action2"> The action to be performed if the package has only forms. </param>
        /// <param name="action3"> The action to be performed if the package has only firmware. </param>
        private void OutputDeterminator(Action action1, Action action2, Action action3)
        {
            if (PackageData.HasForms && PackageData.HasFirmware)
            {
                action1();
            }
            else if (PackageData.HasForms)
            {
                action2();
            }
            else if (PackageData.HasFirmware)
            {
                action3();
            }
        }
    }
}
