using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using Customer.WebApi.Service;
using Customer.WebApi.Model;

[ApiController]
[Route("/api/v1/[controller]/[action]")]
public class CustomerController : ControllerBase
{
    private static readonly ConcurrentDictionary<long, decimal> Scores = new();
    private static readonly SortedSet<CustomerEntry> Leaderboard = new(new CustomerEntryComparer());
    private static readonly ReaderWriterLockSlim _leaderboardLock = new();

    private readonly CustomerDataService _customerDataService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(IMemoryCache cache, CustomerDataService customerDataService, ILogger<CustomerController> logger)
    {
        _cache = cache;
        _logger = logger;
        _customerDataService = customerDataService;

    }

    /// <summary>
    /// Initialize leaderboard with sample data
    /// </summary>
    /// <returns></returns>
    [HttpPost("/initialize")]
    public async Task<IActionResult> InitializeLeaderboard()
    {
        _leaderboardLock.EnterWriteLock();
        try
        {
            Leaderboard.Clear();
            var initialData=_customerDataService.InitCustomerScoreData();
            foreach (var (customerId, score) in initialData)
            {
                Scores[customerId] = score;
                if (score > 0)
                {
                    Leaderboard.Add(new CustomerEntry(customerId, score));
                }
            }
        }
        finally
        {
            _leaderboardLock.ExitWriteLock();
        }

        _cache.Remove("leaderboard"); // Clear cache
        return await Task.FromResult(Ok("Leaderboard initialized with sample data."));
    }

    /// <summary>
    /// Update customer score. Positive values increase the score, negative values decrease it. Range [-1000, 1000].
    /// If the customer does not exist, add the customer and set the score; if exists, update the score.
    /// </summary>
    /// <param name="customerid">Customer ID</param>
    /// <param name="score">Score adjustment value</param>
    /// <returns>The updated score</returns>
    [HttpPost("/customer/{customerid}/score/{score}")]
    public async Task<ActionResult<decimal>> UpdateScore(long customerid, decimal score)
    {
        if (score < -1000 || score > 1000) return BadRequest("Score out of range.");

        // Get current score, default to 0 if not exists
        decimal oldScore = Scores.GetOrAdd(customerid, 0);
        decimal newScore = oldScore + score;

        // Update score storage
        Scores[customerid] = newScore;
        _leaderboardLock.EnterWriteLock();
        try
        {
            // Remove old score entry
            Leaderboard.Remove(new CustomerEntry(customerid, oldScore));
            // Only add entries with score greater than 0
            if (newScore > 0)
            {
                Leaderboard.Add(new CustomerEntry(customerid, newScore));
            }
        }
        finally
        {
            _leaderboardLock.ExitWriteLock();
        }

        _cache.Remove("leaderboard"); // Clear cache
        return await Task.FromResult(Ok(newScore));
    }

    /// <summary>
    /// Get leaderboard by rank range
    /// </summary>
    /// <param name="start">Start rank</param>
    /// <param name="end">End rank</param>
    /// <returns>Leaderboard data</returns>
    [HttpGet("/leaderboard")]
    public async Task<ActionResult<IEnumerable<CustomerRankDto>>> GetByRank([FromQuery] int start, [FromQuery] int end)
    {
        if (start < 1 || end < start) return BadRequest("Invalid rank range.");

        var snapshot = _cache.GetOrCreate("leaderboard", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5); // Cache for 5 minutes
            _leaderboardLock.EnterReadLock();
            try
            {
                return Leaderboard.ToList();
            }
            finally
            {
                _leaderboardLock.ExitReadLock();
            }
        });

        List<CustomerRankDto> result = new();
        int rank = 1;
        foreach (var entry in snapshot)
        {
            if (rank >= start && rank <= end)
                result.Add(new CustomerRankDto(entry.CustomerId, entry.Score, rank));
            if (rank > end) break;
            rank++;
        }

        return await Task.FromResult(Ok(result));
    }

    /// <summary>
    /// Get leaderboard by customer ID
    /// </summary>
    /// <param name="customerid">Customer ID</param>
    /// <param name="high">High range (upward)</param>
    /// <param name="low">Low range (downward)</param>
    /// <returns>Leaderboard data</returns>
    [HttpGet("/leaderboard/{customerid}")]
    public async Task<ActionResult<IEnumerable<CustomerRankDto>>> GetByCustomerId(long customerid, [FromQuery] int high = 0, [FromQuery] int low = 0)
    {
        var snapshot = _cache.GetOrCreate("leaderboard", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);
            _leaderboardLock.EnterReadLock();
            try
            {
                return Leaderboard.ToList();
            }
            finally
            {
                _leaderboardLock.ExitReadLock();
            }
        });

        int idx = snapshot.FindIndex(e => e.CustomerId == customerid);
        if (idx == -1) return NotFound("Customer not found in leaderboard.");

        int start = Math.Max(0, idx - high);
        int end = Math.Min(snapshot.Count - 1, idx + low);

        var result = new List<CustomerRankDto>();
        for (int i = start; i <= end; i++)
        {
            result.Add(new CustomerRankDto(snapshot[i].CustomerId, snapshot[i].Score, i + 1));
        }

        return await Task.FromResult(Ok(result));
    }



    // Comparer
    private class CustomerEntryComparer : IComparer<CustomerEntry>
    {
        public int Compare(CustomerEntry x, CustomerEntry y)
        {
            int cmp = -x.Score.CompareTo(y.Score);
            if (cmp != 0) return cmp;
            return x.CustomerId.CompareTo(y.CustomerId);
        }
    }

    // DTO
    public record CustomerRankDto(long CustomerId, decimal Score, int Rank);
}