using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagementSystem.IntegrationTests
{
    public class CiSmokeFailTests
    {
        [Fact]
        public void Ci_Should_Fail_Intentionally()
        {
            Assert.Fail("Intentional CI failure test");
        }
    }
}
