using System.Reflection;
using CommandLine;
using log4net;

namespace DatabaseUpgrader
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        private static int Main(string[] args)
        {
            Log.InfoFormat("Database Upgrader version {0}", Assembly.GetCallingAssembly().GetName().Version);
            Log.Info("Arguments:");
            for (int i = 0; i < args.Length; i++)
            {
                Log.InfoFormat("{0}: {1}", i + 1, args[i]);
            }
            var options = Options.Parse(args);
            
            if (options != null)
            {
                var upgrader = new DatabaseUpgrader(options.ConnectionString, options.SoftwareVersion, options.SchemaDirectory);
                if (options.RequiresInitialize)
                {
                    return ConvertTo(upgrader.RequiresInitialize(), -5, "Requires Initialize");
                }
                if (options.Initialize)
                {
                    return ConvertTo(upgrader.Initialize(), -4, "Initialize");
                }
                if (options.RequiresUpgrade)
                {
                    Log.Info("Checking if an upgrade is even needed");
                    return ConvertTo(upgrader.RequiresUpgrade(), -1, "Check if Upgrade Required");
                }
                Log.Info("Checking if an upgrade is needed, and if so, performing the upgrade");
                return ConvertTo(upgrader.UpgradeSchema(), -2, "Upgrade Schema");
            }
            Log.ErrorFormat("Could not parse arguments. Check the help text");
            return ConvertTo(false, -3, "Argument Parsing");
        }

        private static int ConvertTo(bool functionResult, int failureReturnValue, string description)
        {
            var returnValue = functionResult ? 0 : failureReturnValue;
            Log.InfoFormat("Result of {0} is {1} so return value will be {2}",
                description, functionResult ? "passed" : "failed", returnValue);
            return returnValue;
        }
    }
}