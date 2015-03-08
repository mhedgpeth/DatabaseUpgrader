using System;

namespace DatabaseUpgrader
{
    public class SchemaVersion
    {
        public long Id { get; set; }
        public int DatabaseVersion { get; set; }
        public string SoftwareVersion { get; set; }
        public DateTime ReleaseDate { get; set; }

        public override string ToString()
        {
            return string.Format("Version {0} released on {1} with database version of {2}",
                SoftwareVersion, ReleaseDate.ToShortDateString(), DatabaseVersion);
        }
    }
}