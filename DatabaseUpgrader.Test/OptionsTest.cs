using NUnit.Framework;

namespace DatabaseUpgrader.Test
{
    [TestFixture]
    public class OptionsTest
    {
        const string Directory = ".";
        const string ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword";
        private const string Version = "1.0.0.0";
        [Test]
        public void Parse_ShouldParseCheckIfUpgradeRequired()
        {
            var options = Options.Parse("--requiresUpgrade", CreateConnectionStringArgument(), string.Format("-d{0}", Directory));

            AssertOptions(options);
        }

        private static void AssertOptions(Options options, bool expectedCheckIfUpgradeRequired = true, string expectedVersion = null, bool expectedInitialize = false, string expectedDirectory = Directory)
        {
            Assert.That(options, Is.Not.Null);
            Assert.That(options.RequiresUpgrade, Is.EqualTo(expectedCheckIfUpgradeRequired));
            Assert.That(options.ConnectionString, Is.EqualTo(ConnectionString));
            Assert.That(options.SchemaDirectory, Is.EqualTo(expectedDirectory));
            Assert.That(options.SoftwareVersion, Is.EqualTo(expectedVersion));
            Assert.That(options.Initialize, Is.EqualTo(expectedInitialize));
            Assert.That(options.RequiresInitialize, Is.False);
        }

        [Test]
        public void Parse_ShouldParseUpgrade()
        {
            var options = Options.Parse(string.Format("-v{0}", Version), CreateConnectionStringArgument(), string.Format("-d{0}", Directory));

            AssertOptions(options, false, Version);
        }

        private static string CreateConnectionStringArgument()
        {
            return string.Format("-c{0}", ConnectionString);
        }

        [Test]
        public void Parse_ShouldParseInitialize()
        {
            var options = Options.Parse("--initialize", CreateConnectionStringArgument());
            AssertOptions(options, false, null, true, null);
        }

        [Test]
        public void Parse_ShouldParseRequiresInitialize()
        {
            var options = Options.Parse("--requiresInitialize", CreateConnectionStringArgument());
            Assert.That(options.RequiresInitialize, Is.True);
        }
    }
}