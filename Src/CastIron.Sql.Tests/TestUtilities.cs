using NUnit.Framework;

namespace CastIron.Sql.Tests
{
    public static class TestUtilities
    {
        public static void NeedsFixesFor(string cantHandle, string provider, string reason = "")
        {
            if (provider == cantHandle)
                Assert.Inconclusive($"Cannot execute this test for provider={provider}. Reason: {reason}");
        }
    }
}
