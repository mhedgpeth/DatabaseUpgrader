using CommandLine;
using SqlSchemaMannager.Annotations;

namespace SqlSchemaMannager
{
    public class Options
    {
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

        [Option('d', "schemaDirectory", Required = true,
            HelpText = "the directory that contains the numbered schema index files")]
        public string SchemaDirectory { get; [UsedImplicitly] set; }
    }
}