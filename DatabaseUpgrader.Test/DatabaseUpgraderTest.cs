using System.Linq;
using NUnit.Framework;

namespace DatabaseUpgrader.Test
{
    [TestFixture]
    public class DatabaseUpgraderTest
    {
        [TestCase("1.sql", 1)]
        [TestCase("25.sql", 25)]
        [TestCase("something.sql", 0)]
        [TestCase("scripts\\1.sql", 1)]
        [Test]
        public void DatabaseVersionFor_ShouldParse(string file, int expected)
        {
            Assert.That(DatabaseUpgrader.DatabaseVersionFor(file), Is.EqualTo(expected));
        }

        private static readonly string[] UpgradeFiles = { "something.sql", "3.sql", "2.sql", "1.sql" };

       
        private static SchemaVersion CreateSchemaVersionFor(int currentSchemaVersion)
        {
            return new SchemaVersion() {DatabaseVersion = currentSchemaVersion};
        }

        [Test]
        public void OrderScriptsToUpgradeDatabase_ShouldProperlyOrderScripts()
        {
            var result = DatabaseUpgrader.OrderScriptsToUpgradeDatabase(CreateSchemaVersionFor(1), UpgradeFiles).ToArray();
            Assert.That(result, Is.Not.Contains("1.sql"));
            Assert.That(result[0], Is.EqualTo("2.sql"));
            Assert.That(result[1], Is.EqualTo("3.sql"));
        }
    }
}