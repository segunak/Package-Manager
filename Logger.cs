using System;
using System.IO;
using System.Reflection;

/// <summary>
/// There is nothing interesting here. It is a Logging class. Singleton implementation. The instance is returned using an
/// an auto property. 
/// </summary>

namespace PackageManager
{
    public class Logger : IDisposable
    {
        private static readonly Lazy<Logger> lazy = new Lazy<Logger>(() => new Logger());
        public static Logger LoggerInstance { get { return lazy.Value; } }

        private readonly StreamWriter FileLogger;
        private bool disposed = false;

        private Logger()
        {
            string logDirectory = Directory.GetCurrentDirectory() + "\\PackageManagerLogs";

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFileName = Path.Combine(logDirectory, "PackageManager_" + DateTime.Now.ToString("MMM-dd-yyyy-hh-mm") + ".log");
            FileLogger = new StreamWriter(logFileName, false);
        }

        /// <summary>
        /// Log a message. 
        /// </summary>
        /// <param name="message">The message to log</param>
        internal void Log(string message)
        {
            FileLogger.WriteLine(DateTime.Now + "         " + message);
            FileLogger.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    FileLogger.Dispose();
                }

                // Note disposing has been done. May also dispose of unmanaged resources here. 
                disposed = true;
            }
        }
    }
}
