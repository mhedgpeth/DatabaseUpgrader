using CommandLine;
using log4net;

namespace DatabaseUpgrader
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        private static int Main(string[] args)
        {
            Log.InfoFormat("Database Upgrader");
            Log.Info("Arguments:");
            for (int i = 0; i < args.Length; i++)
            {
                Log.InfoFormat("{0}: {1}", i + 1, args[i]);
            }
            var options = Options.Parse(args);
            
            if (options != null)
            {
                var upgrader = new DatabaseUpgrader(options.ConnectionString, options.SoftwareVersion, options.SchemaDirectory);
                if (options.Initialize)
                {
                    return upgrader.Initialize() ? 0 : -4;
                }
                if (options.CheckIfUpgradeRequired)
                {
                    Log.Info("Checking if an upgrade is even needed");
                    return upgrader.RequiresUpgrade() ? 0 : -1;
                }
                Log.Info("Checking if an upgrade is needed, and if so, performing the upgrade");
                return upgrader.UpgradeSchema() ? 0 : -2;
            }
            Log.ErrorFormat("Could not parse arguments. Check the help text");
            return -3;
        }
    }
}