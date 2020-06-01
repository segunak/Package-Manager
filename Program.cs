using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Management.Automation;
using System.Collections.ObjectModel;

/// <summary>
/// Program to automate a portion of the SCCM package creation process. It creates the relevant files and scripts then places 
/// them in the correct folders, based on input data passed in by the user. 
/// </summary>

namespace PackageManager
{
    internal static class Program
    {
        private static Logger Logger = Logger.LoggerInstance;
        private static readonly string CurrentFolder = Directory.GetCurrentDirectory();

        static void Main(string[] args)
        {
            Logger.Log("Package Manager Start");

            PackageData packageData = new PackageData();
            packageData.InputInstallFormsFolder = CreateFolder(CurrentFolder + Path.DirectorySeparatorChar, "install-forms");
            packageData.InputInstallFirmwareFolder = CreateFolder(CurrentFolder + Path.DirectorySeparatorChar, "install-firmware");
            packageData.InputRollbackFormsFolder = CreateFolder(CurrentFolder + Path.DirectorySeparatorChar, "rollback-forms");
            packageData.InputRollbackFirmwareFolder = CreateFolder(CurrentFolder + Path.DirectorySeparatorChar, "rollback-firmware");

            if (args.Length != 1)
            {
                ShowUsage();
                CreateSampleInputFile();
                FatalError("This program requires a json file passed as it's only argument. An example input file has been provided in the " +
                    "same directory from whence you ran the executable. Please try again");
            }

            string inputFile = args[0];
            bool inputFileIsValid = ValidateInputFile(inputFile, ".json");
            bool inputFoldersAreValid = ValidateInputFolders(packageData);

            if (!inputFileIsValid || !inputFoldersAreValid)
            {
                ShowUsage();
                CreateSampleInputFile();
                FatalError("The input file or input folders were not in a valid state. An example input file has been provided in the" +
                    "same directory from whence you ran the executable. Please try again.");
            }

            ReadInputFileData(inputFile, packageData);
            SetPackageContentBooleans(packageData);
            CreatePackageFolders(packageData);

            FileBuilder fileBuilder = new FileBuilder(packageData);
            BuildPackageFiles(fileBuilder, packageData);

            if (packageData.HasRollback)
            {
                HandleRollbackItems(packageData, fileBuilder);
            }

            ManifestPackage(packageData);
            CleanExecutableDirectory(packageData);

            Logger.Log("Package Manager End");
            Logger.Dispose();
        }

        /// <summary>
        /// Creates an example input file for package users to reference. 
        /// </summary>
        private static void CreateSampleInputFile()
        {
            string sampleInputFile = CreateFile(CurrentFolder, "sample-input.json");

            if (!string.IsNullOrEmpty(sampleInputFile))
            {
                using (StreamWriter sw = new StreamWriter(sampleInputFile))
                {
                    sw.Write(PackageManager.Properties.Resources.sample_input);
                    Logger.Log("Sample input file created successfully");
                }
            }
        }

        /// <summary>
        /// Creates a folder given a desired path for said folder, if it does not already exist. 
        /// </summary>
        /// <param name="desiredPath">Desired location for the folder</param>
        /// <param name="folderName">Desired name for the folder</param>
        /// <returns>The path to the folder. </returns>
        private static string CreateFolder(string desiredPath, string folderName)
        {
            Logger.Log("Creating the " + folderName + " folder at " + desiredPath);
            string folder = desiredPath + Path.DirectorySeparatorChar + folderName;

            if (Directory.Exists(folder))
            {
                Logger.Log("The " + folder + " folder already exists");
            }
            else
            {
                Directory.CreateDirectory(folder);
                Logger.Log("Folder created successfully");
            }

            return folder;
        }

        /// <summary>
        /// Show the correct usage for the program. 
        /// </summary>
        private static void ShowUsage()
        {
            Logger.Log("Input was incorrect. Usage displayed");
            Console.WriteLine();
            Console.WriteLine("You attempted to create a package without providing the proper input. This program " +
                "takes a single json file argument. Furthermore, four folders must be present in the same directory as the executbale." +
                " Those 4 folders must be named \"install-forms\", \"rollback-forms\", \"install-firmware\" and \"rollback-firmware\". ");
            Console.WriteLine();
            Console.WriteLine("If those folders did not exist prior to your running of this tool, they have been created for you. " +
                "At minimum, you must put something in one of the install folders. Install forms go in the install-forms" +
                " folder. Install firmware goes in the install-firmware folder. Same idea for Rollback forms and firmware. If there is no rollback file(s)" +
                " for the package, leave the folder(s) empty.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Take heed of these requirements and please re-run the executable");
            Console.WriteLine();
            Console.ResetColor();
        }

        /// <summary>
        /// Handles a fatal error in the program. Logs an error message, show's it to the user and exits. 
        /// </summary>
        /// <param name="message"></param>
        private static void FatalError(string message)
        {
            Logger.Log(message);
            Logger.Log("Package Manager End");
            Logger.Dispose();

            Console.Error.Write("Fatal Error: " + message);
            Console.ReadKey();
            Environment.Exit(1);
        }

        /// <summary>
        /// Handles all items related to a rollback, if the package has one. 
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        /// <param name="fileBuilder"> The file building object, containing methods to build each file type for the package </param>
        private static void HandleRollbackItems(PackageData packageData, FileBuilder fileBuilder)
        {
            packageData.RollbackFolder = CreateFolder(packageData.RootPackageFolder, "rollback");
            string rollbackReadMe = CreateFile(packageData.RollbackFolder, "readme.md");
            string rollbackScript = CreateFile(packageData.RollbackFolder, "rollback.cmd");

            if (packageData.HasFormsRollback)
            {
                packageData.RollbackForms = GetFolderFiles(packageData.InputRollbackFormsFolder);
                packageData.FinalRollbackFormsFolder = packageData.RollbackFolder + "\\Forms";
                Directory.Move(packageData.InputRollbackFormsFolder, packageData.FinalRollbackFormsFolder);
            }

            if (packageData.HasFirmwareRollback)
            {
                packageData.RollbackFirmware = GetFolderFiles(packageData.InputRollbackFirmwareFolder);
                packageData.FinalRollbackFirmwareFolder = packageData.RollbackFolder + "\\Firmware";
                Directory.Move(packageData.InputRollbackFirmwareFolder, packageData.FinalRollbackFirmwareFolder);
            }

            fileBuilder.BuildRollbackReadMe(rollbackReadMe);
            fileBuilder.BuildRollbackScript(rollbackScript);
        }

        /// <summary>
        /// Creates the core folders for the package. 
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        private static void CreatePackageFolders(PackageData packageData)
        {
            packageData.RootPackageFolder = CreateFolder(CurrentFolder, packageData.PackageName);
            packageData.InstallFolder = CreateFolder(packageData.RootPackageFolder, "install");
            packageData.ScriptsFolder = CreateFolder(packageData.RootPackageFolder, "scripts");
            packageData.VerifyFolder = CreateFolder(packageData.RootPackageFolder, "verify");

            if (packageData.HasForms)
            {
                packageData.InstallForms = GetFolderFiles(packageData.InputInstallFormsFolder);
                packageData.FinalInstallFormsFolder = packageData.InstallFolder + "\\Forms";
                Directory.Move(packageData.InputInstallFormsFolder, packageData.FinalInstallFormsFolder);
            }

            if (packageData.HasFirmware)
            {
                packageData.InstallFirmware = GetFolderFiles(packageData.InputInstallFirmwareFolder);
                packageData.FinalInstallFirmwareFolder = packageData.InstallFolder + "\\Firmware";
                Directory.Move(packageData.InputInstallFirmwareFolder, packageData.FinalInstallFirmwareFolder);
            }
        }

        /// <summary>
        /// Sets the booleans that determine what key items are going to be a part of the package being created. 
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        private static void SetPackageContentBooleans(PackageData packageData)
        {
            packageData.HasForms = !IsDirectoryEmpty(packageData.InputInstallFormsFolder);
            packageData.HasFirmware = !IsDirectoryEmpty(packageData.InputInstallFirmwareFolder);
            packageData.HasFormsRollback = !IsDirectoryEmpty(packageData.InputRollbackFormsFolder);
            packageData.HasFirmwareRollback = !IsDirectoryEmpty(packageData.InputRollbackFirmwareFolder);
        }

        /// <summary>
        /// Build all of the various files associated with a package. 
        /// </summary>
        /// <param name="fileBuilder"> The file builder object, which contains methods for building each file type. </param>
        /// <param name="packageData"> The data object for the package </param>
        private static void BuildPackageFiles(FileBuilder fileBuilder, PackageData packageData)
        {
            string rootReadMe = CreateFile(packageData.RootPackageFolder, "readme.md");
            string changeLogFile = CreateFile(packageData.RootPackageFolder, "changelog.txt");
            string verifyReadMe = CreateFile(packageData.VerifyFolder, "readme.md");
            string touchScript = CreateFile(packageData.ScriptsFolder, "touch.cmd");
            string installReadMe = CreateFile(packageData.InstallFolder, "readme.md");
            string installScript = CreateFile(packageData.InstallFolder, "install.cmd");

            fileBuilder.BuildRootReadMe(rootReadMe);
            fileBuilder.BuildChangeLog(changeLogFile);
            fileBuilder.BuildVerifyReadMe(verifyReadMe);
            fileBuilder.BuildTouchScript(touchScript);
            fileBuilder.BuildInstallReadMe(installReadMe);
            fileBuilder.BuildInstallScript(installScript);
        }

        /// <summary>
        /// Checks the validity of the input file. Has to match desired extension and actually exist. 
        /// </summary>
        /// <param name="filePath"> Path to the input file. </param>
        /// <param name="desiredExtension"> Desired extension of the input file </param>
        private static bool ValidateInputFile(string filePath, string desiredExtension)
        {
            return File.Exists(filePath) && Path.GetExtension(filePath) == desiredExtension;
        }

        /// <summary>
        /// Check's the validity of the input folders for the program. Forgive me for this boolean logic. 
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        /// <returns> True or false, true if things are good, false if they aren't. </returns>
        private static bool ValidateInputFolders(PackageData packageData)
        {
            return Directory.Exists(packageData.InputInstallFormsFolder) && Directory.Exists(packageData.InputRollbackFormsFolder)
                   && Directory.Exists(packageData.InputInstallFirmwareFolder) && Directory.Exists(packageData.InputRollbackFirmwareFolder)
                   && (!IsDirectoryEmpty(packageData.InputInstallFormsFolder) || !IsDirectoryEmpty(packageData.InputInstallFirmwareFolder));
        }

        /// <summary>
        /// Checks if a folder (directory) is empty. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns> True or false, true it's empty, false it is not </returns>
        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        /// <summary>
        /// Handle the creation of the file manifest for the package. 
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        private static void ManifestPackage(PackageData packageData)
        {
            if (ExecuteManifestScript(packageData))
            {
                Logger.Log("Manifest created successfully");
            }
            else
            {
                Console.WriteLine("Warning, the manifest file for this package was not able to be generated. All other package " +
                    "items were created successfully.");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Execute the PowerShell script that creates a manifest of the root package folder.
        /// </summary>
        /// <param name="packageData"></param>
        private static bool ExecuteManifestScript(PackageData packageData)
        {
            Logger.Log("Beginning execution of Manifest script for the " + packageData.PackageName + " package");

            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                PowerShellInstance.AddScript(PackageManager.Properties.Resources.Get_Md5Manifest);
                PowerShellInstance.AddParameter("Target", packageData.RootPackageFolder);
                PowerShellInstance.AddParameter("Recurse", true);

                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();
                PSObject outputItem = PSOutput.FirstOrDefault();

                if (outputItem != null)
                {
                    string outputManifest = packageData.RootPackageFolder + Path.DirectorySeparatorChar + packageData.PackageName + ".manfiest.json";
                    File.Create(outputManifest).Dispose();

                    using (var sw = new StreamWriter(outputManifest, true))
                    {
                        sw.Write(outputItem.BaseObject.ToString());
                    }
                }
                else
                {
                    Logger.Log("Failed to create a manifest file for the package");
                    return false;
                }
            }

            Logger.Log("Manifest file created successfully");
            return true;
        }

        /// <summary>
        /// Reads the input file to the program. This is where paramaters are provided for creating the package. 
        /// </summary>
        /// <param name="inputFile"> Path to the input file. </param>
        /// <param name="packageData"> Data object for the package. </param>
        private static void ReadInputFileData(string inputFile, PackageData packageData)
        {
            try
            {
                Logger.Log("Reading input file");

                using (var streamReader = new StreamReader(new FileStream(inputFile, FileMode.Open, FileAccess.Read)))
                {
                    while (!streamReader.EndOfStream)
                    {
                        dynamic inputJson = JsonConvert.DeserializeObject(streamReader.ReadToEnd());

                        packageData.PackageName = inputJson.PACKAGE_NAME;
                        packageData.Date = inputJson.DATE;
                        packageData.BusinessItem = inputJson.BUSINESS_ITEM;
                        packageData.Prerequisites = inputJson.PREREQUISITES.ToObject<string[]>();
                        packageData.PackageDescription = inputJson.PACKAGE_DESCRIPTION;
                    }
                }

                Logger.Log("Finished reading input file");
            }
            catch (Exception e)
            {
                FatalError("Fatal Error reading input file: " + e.Message);
            }
        }

        /// <summary>
        ///  This method gets all the files inside of a folder (forms or firmware for this project) and returns the path to each
        ///  of those files in an array. 
        /// </summary>
        /// <param name="folderToProcess"></param>
        /// <returns> A string array with each file in the folder </returns>
        private static string[] GetFolderFiles(string folderToProcess)
        {
            Logger.Log("Getting forms from the " + folderToProcess + " folder");
            string[] theFiles = null;
            theFiles = Directory.GetFiles(folderToProcess);
            Logger.Log("successfully processed forms folder");

            return theFiles;
        }

        /// <summary>
        /// Creates a file and puts it in the directory you specify. 
        /// </summary>
        /// <param name="rootDirectory">The folder where the file should be placed</param>
        /// <param name="fileName">Desired name of the file</param>
        /// <returns></returns>
        private static string CreateFile(string rootDirectory, string fileName)
        {
            Logger.Log("Creating the " + fileName + " file in the " + rootDirectory + " folder");

            string theFile = rootDirectory + Path.DirectorySeparatorChar + fileName;

            if (!File.Exists(theFile))
            {
                File.Create(theFile).Dispose();
                Logger.Log(fileName + " file created successfully");
            }
            else
            {
                Logger.Log(fileName + " already exists as a file");
                return string.Empty;
            }

            return theFile;
        }

        /// <summary>
        /// Clean up the folder where the executable resides by getting rid of any remaining input folders.
        /// </summary>
        /// <param name="packageData"> The data object for the package </param>
        private static void CleanExecutableDirectory(PackageData packageData)
        {
            if (Directory.Exists(packageData.InputInstallFormsFolder)) Directory.Delete(packageData.InputInstallFormsFolder);
            if (Directory.Exists(packageData.InputInstallFirmwareFolder)) Directory.Delete(packageData.InputInstallFirmwareFolder);
            if (Directory.Exists(packageData.InputRollbackFormsFolder)) Directory.Delete(packageData.InputRollbackFormsFolder);
            if (Directory.Exists(packageData.InputRollbackFirmwareFolder)) Directory.Delete(packageData.InputRollbackFirmwareFolder);
        }
    }
}
