using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagementSystem.IntegrationTests
{
    public  class CiSmokeFailTests
    {
        [Fact]
        public  void Ci_Should_Fail_Intentionaly()
        {
            Assert.Fail("Intentional CI Failure Test");
        }
    }
}
