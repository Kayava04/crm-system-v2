using Shared.Kernel.Common;

namespace Crm.UnitTests.Common;

public class BulkOperationResponseTests
{
    [Fact]
    public void From_counts_successes_and_failures()
    {
        var results = new List<BulkItemResult>
        {
            new(0, true, Guid.NewGuid(), "a", []),
            new(1, false, null, "b", ["bad"]),
            new(2, true, Guid.NewGuid(), "c", []),
        };

        var response = BulkOperationResponse.From(results);

        Assert.Equal(3, response.Total);
        Assert.Equal(2, response.Succeeded);
        Assert.Equal(1, response.Failed);
        Assert.Same(results, response.Results);
    }

    [Fact]
    public void Empty_results_give_zero_counts()
    {
        var response = BulkOperationResponse.From([]);

        Assert.Equal(0, response.Total);
        Assert.Equal(0, response.Succeeded);
        Assert.Equal(0, response.Failed);
    }
}
