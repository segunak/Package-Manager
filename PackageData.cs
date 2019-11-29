/// <summary>
/// Class for the core data elements of a package. 
/// </summary>

namespace PackageManager
{
    public class PackageData
    {
        // Initial Package Input Folders
        public string InputInstallFormsFolder { get; set; }
        public string InputInstallFirmwareFolder { get; set; }
        public string InputRollbackFormsFolder { get; set; }
        public string InputRollbackFirmwareFolder { get; set; }

        // Key input data for the package, read from the input file. 
        public string Date { get; set; }
        public string PackageName { get; set; }
        public string BusinessItem { get; set; }
        public string[] Prerequisites { get; set; }
        public string PackageDescription { get; set; }

        // Booleans for determining what items are a part of the package. 
        public bool HasForms { get; set; }
        public bool HasFirmware { get; set; }
        public bool HasFormsRollback { get; set; }
        public bool HasFirmwareRollback { get; set; }

        // Regardless of whether it's Forms or Firmware, determine if a package has a rollback in general. 
        public bool HasRollback
        {
            get { return HasFormsRollback || HasFirmwareRollback; }
        }

        // Forms and/or Firmware for install/rollback
        public string[] InstallForms { get; set; }
        public string[] RollbackForms { get; set; }
        public string[] InstallFirmware { get; set; }
        public string[] RollbackFirmware { get; set; }

        // Folders included in the final package output.  
        public string InstallFolder { get; set; }
        public string ScriptsFolder { get; set; }
        public string RollbackFolder { get; set; }
        public string VerifyFolder { get; set; }
        public string RootPackageFolder { get; set; }
        public string FinalInstallFormsFolder { get; set; }
        public string FinalRollbackFormsFolder { get; set; }
        public string FinalInstallFirmwareFolder { get; set; }
        public string FinalRollbackFirmwareFolder { get; set; }
    }
}
