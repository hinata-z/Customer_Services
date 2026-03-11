using Customer.WebApi.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Customer.WebApi.Tests.Controllers;

[TestClass]
public class CustomerControllerTests
{
    private static CustomerController CreateController(IMemoryCache cache)
    {
        var dataService = new CustomerDataService();
        var logger = LoggerFactory.Create(b => { }).CreateLogger<CustomerController>();
        return new CustomerController(cache, dataService, logger);
    }

    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    private static async Task ResetAsync(CustomerController controller)
    {
        // 重置静态 Leaderboard/Scores，并清掉缓存
        await controller.InitializeLeaderboard();
    }

    [TestMethod]
    public async Task InitializeLeaderboard_ReturnsOk_WithMessage()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);

        var result = await controller.InitializeLeaderboard();

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual("Leaderboard initialized with sample data.", ok.Value);
    }

    [DataTestMethod]
    [DataRow(-1000.01)]
    [DataRow(1000.01)]
    public async Task UpdateScore_WhenOutOfRange_ReturnsBadRequest(double delta)
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        var result = await controller.UpdateScore(1, (decimal)delta);

        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        Assert.AreEqual("Score out of range.", bad.Value);
    }

    [TestMethod]
    public async Task UpdateScore_NewCustomer_AddsToLeaderboard_WhenScorePositive()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        var customerId = 999_000_001L;

        var update = await controller.UpdateScore(customerId, 10);
        var okUpdate = update.Result as OkObjectResult;
        Assert.IsNotNull(okUpdate);
        Assert.AreEqual(10m, (decimal)okUpdate.Value!);

        var leaderboard = await controller.GetByCustomerId(customerId, high: 0, low: 0);
        var okLeaderboard = leaderboard.Result as OkObjectResult;
        Assert.IsNotNull(okLeaderboard);

        var rows = okLeaderboard.Value as IEnumerable<CustomerController.CustomerRankDto>;
        Assert.IsNotNull(rows);

        var list = rows.ToList();
        Assert.AreEqual(1, list.Count);

        var row = list[0];
        Assert.AreEqual(customerId, row.CustomerId);
        Assert.AreEqual(10m, row.Score);
        Assert.IsTrue(row.Rank >= 1);
    }

    [TestMethod]
    public async Task UpdateScore_ExistingCustomer_DecreasesToZero_RemovesFromLeaderboard()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        // 来自 sample data：38819 => 92
        const long customerId = 38819;

        var update = await controller.UpdateScore(customerId, -92);
        var okUpdate = update.Result as OkObjectResult;
        Assert.IsNotNull(okUpdate);
        Assert.AreEqual(0m, (decimal)okUpdate.Value!);

        var get = await controller.GetByCustomerId(customerId, high: 0, low: 0);
        var notFound = get.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual("Customer not found in leaderboard.", notFound.Value);
    }

    [DataTestMethod]
    [DataRow(0, 1)]
    [DataRow(2, 1)]
    public async Task GetByRank_WhenInvalidRange_ReturnsBadRequest(int start, int end)
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        var result = await controller.GetByRank(start, end);

        var bad = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(bad);
        Assert.AreEqual("Invalid rank range.", bad.Value);
    }

    [TestMethod]
    public async Task GetByRank_ReturnsRanks_InRequestedWindow()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        var result = await controller.GetByRank(start: 1, end: 3);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);

        var rows = ok.Value as IEnumerable<CustomerController.CustomerRankDto>;
        Assert.IsNotNull(rows);

        var list = rows.ToList();
        Assert.AreEqual(3, list.Count);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list.Select(x => x.Rank).ToArray());
        Assert.IsTrue(list[0].Score >= list[1].Score);
        Assert.IsTrue(list[1].Score >= list[2].Score);
    }

    [TestMethod]
    public async Task GetByCustomerId_WhenNotExists_ReturnsNotFound()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        var result = await controller.GetByCustomerId(123456789, high: 0, low: 0);

        var notFound = result.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual("Customer not found in leaderboard.", notFound.Value);
    }

    [TestMethod]
    public async Task GetByCustomerId_ReturnsWindowAroundCustomer_WithCorrectRanks()
    {
        using var cache = CreateCache();
        var controller = CreateController(cache);
        await ResetAsync(controller);

        // sample data 中一定在榜：15514665(124) 指定 high/low 返回窗口
        const long customerId = 15514665;

        var result = await controller.GetByCustomerId(customerId, high: 1, low: 1);

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);

        var rows = ok.Value as IEnumerable<CustomerController.CustomerRankDto>;
        Assert.IsNotNull(rows);

        var list = rows.ToList();
        Assert.IsTrue(list.Count is >= 1 and <= 3);
        Assert.IsTrue(list.Any(x => x.CustomerId == customerId));

        var ranks = list.Select(x => x.Rank).ToList();
        CollectionAssert.AreEqual(ranks.OrderBy(x => x).ToList(), ranks);
        Assert.AreEqual(ranks.Min(), ranks.First());
        Assert.AreEqual(ranks.Max(), ranks.Last());
    }
}