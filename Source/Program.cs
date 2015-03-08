using CommandLine;
using log4net;

namespace SqlSchemaMannager
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        private static int Main(string[] args)
        {
            var options = new Options();
            var result = Parser.Default.ParseArguments(args, options);
            if (result)
            {
                var upgrader = new DatabaseUpgrader.DatabaseUpgrader(options.ConnectionString, options.SoftwareVersion, options.SchemaDirectory);
                if (options.CheckIfUpgradeRequired)
                {
                    Log.Info("Checking if an upgrade is even needed");
                    result = upgrader.RequiresUpgrade();
                }
                else
                {
                    Log.Info("Checking if an upgrade is needed, and if so, performing the upgrade");
                    result = upgrader.UpgradeSchema();
                }
            }
            else
            {
                Log.ErrorFormat("Could not parse arguments. Check the help text");
            }
            return result ? 0 : -1;
        }
    }
}