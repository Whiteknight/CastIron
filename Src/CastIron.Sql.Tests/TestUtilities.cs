using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    public static class TestUtilities
    {
        public static void NeedsFixesFor(string cantHandle, string provider)
        {
            if (provider == cantHandle)
                Assert.Inconclusive($"Cannot execute this test for provider={provider}");
        }
    }
}
