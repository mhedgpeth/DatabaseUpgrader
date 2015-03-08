using NUnit.Framework;

namespace DatabaseUpgrader.Test
{
    [TestFixture]
    public class OptionsTest
    {
        const string Directory = ".";
        const string ConnectionString = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword";
        private const string Version = "1.2.3.4";
        [Test]
        public void Parse_ShouldParseCheckIfUpgradeRequired()
        {
            var options = Options.Parse("--checkIfUpgradeRequired", CreateConnectionStringArgument(), string.Format("-d{0}", Directory));

            AssertOptions(options);
        }

        private static void AssertOptions(Options options, bool expectedCheckIfUpgradeRequired = true, string expectedVersion = null, bool expectedInitialize = false, string expectedDirectory = Directory)
        {
            Assert.That(options, Is.Not.Null);
            Assert.That(options.CheckIfUpgradeRequired, Is.EqualTo(expectedCheckIfUpgradeRequired));
            Assert.That(options.ConnectionString, Is.EqualTo(ConnectionString));
            Assert.That(options.SchemaDirectory, Is.EqualTo(expectedDirectory));
            Assert.That(options.SoftwareVersion, Is.EqualTo(expectedVersion));
            Assert.That(options.Initialize, Is.EqualTo(expectedInitialize));
        }

        [Test]
        public void Parse_ShouldParseUpgrade()
        {
            var options = Options.Parse(CreateConnectionStringArgument(), string.Format("-d{0}", Directory),
                "-v" + Version);

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

    }
}