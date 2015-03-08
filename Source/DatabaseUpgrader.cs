using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using log4net;
using Microsoft.SqlServer.Management.Common;

namespace DatabaseUpgrader
{
    public class DatabaseUpgrader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (DatabaseUpgrader));
        private readonly string m_ConnectionString;
        private readonly string m_SoftwareVersion;
        private readonly string[] m_UpgradeFiles;

        public DatabaseUpgrader(string connectionString, string softwareVersion, string scriptsPath)
        {
            m_ConnectionString = connectionString;
            m_SoftwareVersion = softwareVersion;
            m_UpgradeFiles = !string.IsNullOrEmpty(scriptsPath)
                ? Directory.GetFiles(scriptsPath, "*.sql")
                : new string[0];
            Log.DebugFormat("Created for version {0} going to scripts path {1}", softwareVersion, scriptsPath);
        }

        public bool Initialize()
        {
            using (var connection = new SqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "CREATE TABLE [dbo].[Version]( " +
                        "[Id] int IDENTITY(1, 1), " +
                        "[DatabaseVersion] [int] NOT NULL, " +
                        "[SoftwareVersion] [nvarchar](25) NOT NULL, " +
                        "[ReleaseDate] [datetime] NOT NULL " +
                        "CONSTRAINT [PK_Version] PRIMARY KEY CLUSTERED " +
                        "( " +
                        "[Id] ASC " +
                        ")WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]" +
                        ") ON [PRIMARY]";
                    command.ExecuteNonQuery();
                    Log.Info("Adding initial version to the database");
                    return AddToDatabase(new SchemaVersion()
                    {
                        Id = 0,
                        DatabaseVersion = 0,
                        SoftwareVersion = "0.0.0.0",
                        ReleaseDate = DateTime.Now
                    });
                }
            }
        }

        public bool RequiresUpgrade()
        {
            SchemaVersion currentSchemaVersion = RetrieveCurrentVersionFromDatabase(m_ConnectionString);
            if (currentSchemaVersion == null)
            {
                Log.Warn("This database has never been upgraded, so an upgrade is reuired");
                return true;
            }
            bool requiresUpgrade = OrderScriptsToUpgradeDatabase(currentSchemaVersion, m_UpgradeFiles).Length > 0;
            Log.InfoFormat("This database does{0} require upgrade", requiresUpgrade ? string.Empty : " not");
            return requiresUpgrade;
        }

        public bool UpgradeSchema()
        {
            SchemaVersion currentSchemaVersion = RetrieveCurrentVersionFromDatabase(m_ConnectionString);
            if (currentSchemaVersion == null)
            {
                Log.Warn("This database has never been upgraded, so starting from the beginning");
                currentSchemaVersion = new SchemaVersion {DatabaseVersion = 0, Id = 0};
            }
            var orderedScriptsToExecute = OrderScriptsToUpgradeDatabase(currentSchemaVersion, m_UpgradeFiles);
            if (orderedScriptsToExecute.Length == 0)
            {
                Log.InfoFormat("The database version {0} is current so no upgrade is needed.",
                    currentSchemaVersion.DatabaseVersion);
                return true;
            }
            bool result = UpdateScripts(orderedScriptsToExecute);
            if (result)
            {
                result = AddUpdatedVersionToDatabase(DatabaseVersionFor(orderedScriptsToExecute.LastOrDefault()));
            }
            return result;
        }

        private bool UpdateScripts(IEnumerable<string> items)
        {
            using (var connection = new SqlConnection(m_ConnectionString))
            {
                var serverConnection = new ServerConnection(connection) {BatchSeparator = "GO"};
                foreach (var item in items)
                {
                    Log.DebugFormat("Processing file {0}", item);
                    int result = serverConnection.ExecuteNonQuery(ReadFile(item));
                    Log.DebugFormat("Result from SQL query: {0}", result);
                }
            }
            return true;
        }

        private static string ReadFile(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return reader.ReadToEnd();
            }
        }

        private bool AddUpdatedVersionToDatabase(int destination)
        {
            var upgradedVersion = new SchemaVersion
            {
                DatabaseVersion = destination,
                ReleaseDate = DateTime.Now,
                SoftwareVersion = m_SoftwareVersion
            };
            Log.InfoFormat("Updating database to make latest version {0}", upgradedVersion);
            bool result = AddToDatabase(upgradedVersion);
            return result;
        }

        private bool AddToDatabase(SchemaVersion schemaVersion)
        {
            using (var connection = new SqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "insert into Version (DatabaseVersion, SoftwareVersion, ReleaseDate) "
                        + "VALUES (@DatabaseVersion, @SoftwareVersion, @ReleaseDate)";
                    command.Parameters.AddWithValue("DatabaseVersion", schemaVersion.DatabaseVersion);
                    command.Parameters.AddWithValue("ReleaseDate", schemaVersion.ReleaseDate);
                    command.Parameters.AddWithValue("SoftwareVersion", schemaVersion.SoftwareVersion);
                    int rowCount = command.ExecuteNonQuery();
                    Log.InfoFormat("Execute insert affected {0} rows", rowCount);
                    return rowCount == 1;
                }
            }
        }


        public static string[] OrderScriptsToUpgradeDatabase(SchemaVersion currentSchemaVersion,
            IEnumerable<string> upgradeFiles)
        {
            var scriptsToProcess = new List<string>();
            foreach (var file in upgradeFiles)
            {
                int scriptVersion = DatabaseVersionFor(file);
                Log.InfoFormat(
                    "Checking if file {0} with version {1} should be in the upgrade when the current version is {2}",
                    file, scriptVersion, currentSchemaVersion.DatabaseVersion);
                if (scriptVersion > currentSchemaVersion.DatabaseVersion)
                {
                    Log.InfoFormat("Processing item {0} because it should be processed after {1}", file,
                        currentSchemaVersion.DatabaseVersion);
                    scriptsToProcess.Add(file);
                }
                else
                {
                    Log.InfoFormat("File {0} has already been upgraded so it will be skipped", file);
                }
            }
            return scriptsToProcess.OrderBy(DatabaseVersionFor).ToArray();
        }

        public static int DatabaseVersionFor(string file)
        {
            int numericPart = 0;
            string fileName = file.Substring(file.IndexOf('\\') + 1);
            if (fileName.EndsWith(".sql"))
            {
                string numericPartString = fileName.Substring(0, fileName.Length - 4);
                Log.DebugFormat("Parsing file {0} with numeric part as {1}", file, numericPartString);
                int.TryParse(numericPartString, out numericPart);
            }
            return numericPart;
        }

        private SchemaVersion RetrieveCurrentVersionFromDatabase(string connectionString)
        {
            SchemaVersion currentSchemaVersion = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand query = connection.CreateCommand())
                {
                    query.CommandText =
                        "select top 1 Id, DatabaseVersion, SoftwareVersion, ReleaseDate from Version order by DatabaseVersion desc";
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            currentSchemaVersion = new SchemaVersion
                            {
                                Id = reader.GetInt32(0),
                                DatabaseVersion = reader.GetInt32(1),
                                SoftwareVersion = reader.GetString(2),
                                ReleaseDate = reader.GetDateTime(3)
                            };
                            Log.InfoFormat("Latest version in database is {0}", currentSchemaVersion);
                        }
                    }
                }
            }
            return currentSchemaVersion;
        }
    }
}