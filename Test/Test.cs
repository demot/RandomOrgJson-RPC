using NUnit.Framework;
using System;
using Demot.RandomOrgJsonRPC;

namespace Demot.RandomOrgJsonRPCUnitTest
{
    [TestFixture ()]
    public class Test
    {
        const string ApiKey = "YOUR-API-KEY";

        [Test ()]
        public void TestClientInit ()
        {
            var failingKeys = new [] {
                null, String.Empty, "", " ", "\t", " \t"
            };
            foreach (var key in failingKeys) {
                var ex = Assert.Throws<ArgumentException> (() => new RandomJsonRPCClient (key));
                Assert.That (ex.Message, Is.EqualTo ("apiKey"));
            }

            var passingKeys = new [] {
                ApiKey,
                "4c233626-d83a-460e-b8c5-174c67d01fb5",
                "8a5c555d-927c-4a35-b7b6-607a0f9d3655",
                "9a74f90f-84f7-4f12-b6ca-c44944ec4b29"
            };
            foreach (var key in passingKeys) {
                Assert.DoesNotThrow (() => new RandomJsonRPCClient (key));
            }
        }
    }
}
