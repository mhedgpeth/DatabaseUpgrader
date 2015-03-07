using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using log4net;
using Microsoft.SqlServer.Management.Common;

namespace SqlSchemaMannager
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
            m_UpgradeFiles = Directory.GetFiles(scriptsPath, "*.sql");
        }

        public bool RequiresUpgrade()
        {
            SchemaVersion currentSchemaVersion = RetrieveCurrentVersionFromDatabase(m_ConnectionString);
            if (currentSchemaVersion == null)
            {
                Log.Error("You must have a version defined in the upgrade table to continue");
                return false;
            }
            int destination = HighestFileNumberPast(currentSchemaVersion);
            Log.InfoFormat("The database is currently at {0} and the highest schema available is {1}");
            bool requiresUpgrade = currentSchemaVersion.DatabaseVersion < destination;
            Log.InfoFormat("This database does{0} require upgrade", requiresUpgrade ? string.Empty : " not");
            return requiresUpgrade;
        }

        public bool UpgradeSchema()
        {
            SchemaVersion currentSchemaVersion = RetrieveCurrentVersionFromDatabase(m_ConnectionString);
            if (currentSchemaVersion == null)
            {
                Log.Error("You must have a version defined in the upgrade table to continue");
                return false;
            }
            int destination = HighestFileNumberPast(currentSchemaVersion);
            if (currentSchemaVersion.DatabaseVersion == destination)
            {
                Log.InfoFormat("The database version {0} is current so no upgrade is needed.", destination);
                return true;
            }
            var orderedScriptsToExecute = OrderScriptsToUpgradeDatabase(currentSchemaVersion, destination);
            bool result = UpdateScripts(orderedScriptsToExecute);
            if (result)
            {
                result = AddUpdatedVersionToDatabase(destination);
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
                        "insert into Version (Id, DatabaseVersion, SoftwareVersion, ReleaseDate) "
                        + "VALUES ((SELECT MAX(Id) + 1 FROM Version), @DatabaseVersion, @SoftwareVersion, @ReleaseDate)";
                    command.Parameters.AddWithValue("DatabaseVersion", schemaVersion.DatabaseVersion);
                    command.Parameters.AddWithValue("ReleaseDate", schemaVersion.ReleaseDate);
                    command.Parameters.AddWithValue("SoftwareVersion", schemaVersion.SoftwareVersion);
                    int rowCount = command.ExecuteNonQuery();
                    Log.InfoFormat("Execute insert affected {0} rows", rowCount);
                    return rowCount == 1;
                }
            }
        }


        private IEnumerable<string> OrderScriptsToUpgradeDatabase(SchemaVersion currentSchemaVersion, int destination)
        {
            var scriptsToProcess = new List<string>();
            foreach (var file in m_UpgradeFiles)
            {
                int scriptVersion = DatabaseVersionFor(file);
                if (scriptVersion > currentSchemaVersion.DatabaseVersion)
                {
                    Log.InfoFormat("Processing item {0} because it should be processed after {1}", file,
                        destination);
                    scriptsToProcess.Add(file);
                }
            }
            return scriptsToProcess.OrderBy(DatabaseVersionFor);
        }

        private int HighestFileNumberPast(SchemaVersion schemaVersion)
        {
            int highestVersion = schemaVersion.DatabaseVersion;
            foreach (var file in m_UpgradeFiles)
            {
                int numericPart = DatabaseVersionFor(file);
                if (numericPart > highestVersion)
                {
                    Log.DebugFormat("File {0} exists as an upgrade to existing version {1}",
                        file, schemaVersion.DatabaseVersion);
                    highestVersion = numericPart;
                }
                else
                {
                    Log.DebugFormat("File {0} has already been processed against this version because it is before {1} (is version {2}",
                        file, schemaVersion.DatabaseVersion, numericPart);
                }
            }
            return highestVersion;
        }

        private int DatabaseVersionFor(string file)
        {
            int numericPart = 0;
            string fileName = file.Substring(file.IndexOf('/') + 1);
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
                                Id = reader.GetInt64(0),
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