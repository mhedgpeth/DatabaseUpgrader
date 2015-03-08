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
            var options = Options.Parse("--checkIfUpgradeRequired", string.Format("-c{0}", ConnectionString), string.Format("-d{0}", Directory));

            AssertOptions(options);
        }

        private static void AssertOptions(Options options, bool expectedCheckIfUpgradeRequired = true, string expectedVersion = null)
        {
            Assert.That(options, Is.Not.Null);
            Assert.That(options.CheckIfUpgradeRequired, Is.EqualTo(expectedCheckIfUpgradeRequired));
            Assert.That(options.ConnectionString, Is.EqualTo(ConnectionString));
            Assert.That(options.SchemaDirectory, Is.EqualTo(Directory));
            Assert.That(options.SoftwareVersion, Is.EqualTo(expectedVersion));
        }

        [Test]
        public void Parse_ShouldParseUpgrade()
        {
            var options = Options.Parse(string.Format("-c{0}", ConnectionString), string.Format("-d{0}", Directory),
                "-v" + Version);

            AssertOptions(options, false, Version);
        }

    }
}