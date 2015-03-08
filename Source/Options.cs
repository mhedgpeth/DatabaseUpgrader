using CommandLine;
using DatabaseUpgrader.Annotations;

namespace DatabaseUpgrader
{
    public class Options
    {
        public static Options Parse(params string[] args)
        {
            var options = new Options();
            var result = Parser.Default.ParseArguments(args, options);
            return result ? options : null;
        }

        [Option(DefaultValue = false,
            HelpText =
                "If declared, will only check that an upgrade is required and skip the actual upgrade. This is for only-if situations"
            )]
        public bool CheckIfUpgradeRequired { get; [UsedImplicitly] set; }

        [Option('c', "connectionString", Required = true,
            HelpText = "the connection string that connects to the database")]
        public string ConnectionString { get; [UsedImplicitly] set; }

        [Option('v', "softwareVersion", Required = false,
            HelpText = "the software version to add to the version table if upgrading the database")]
        public string SoftwareVersion { get; [UsedImplicitly] set; }

        [Option('d', "schemaDirectory", Required = false,
            HelpText = "the directory that contains the numbered schema index files")]
        public string SchemaDirectory { get; [UsedImplicitly] set; }

        [Option('i', "initialize", Required = false, DefaultValue = false,
            HelpText = "that you want to initialize the database")]
        public bool Initialize { get; set; }
    }
}