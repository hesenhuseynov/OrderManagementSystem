using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagementSystem.IntegrationTests
{
    [CollectionDefinition("IntegrationTests",DisableParallelization =true)]
    public sealed class IntegrationTestCollection:ICollectionFixture<IntegrationTestFixture>
    {
    }
}
