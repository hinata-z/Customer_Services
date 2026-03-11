using Customer.WebApi.Model;
using System;
using System.Reflection.Metadata.Ecma335;

namespace Customer.WebApi.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomerDataService
    {
        private readonly ILogger<CustomerDataService> _logger;
        private static readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new List<(long CustomerId, decimal Score)> InitCustomerScoreData()
        {
            var initialData = new List<(long CustomerId, decimal Score)>
            {
                 (15514665, 124),
                   (81546541, 113),
                (1745431, 100),
                (76786448, 100),
                (254814111, 96),
                (53274324, 95),
                (6144320, 93),
                (8009471, 93),
                (11028481, 93),
                (38819, 92)
            };
            return initialData;


        }
        /// <summary>
        ///  随机生成n个用户数据
        /// </summary>
        /// <param name="n"></param>
        /// <param name="minScore"></param>
        /// <param name="maxScore"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public  List<(long CustomerId, decimal Score)> InitCustomerScoreData(long n, decimal minScore = 0, decimal maxScore = 200)
        {
            if (n <= 0) throw new ArgumentException("n must be a positive integer.", nameof(n));
            if (minScore > maxScore) throw new ArgumentException("minScore cannot be greater than maxScore.", nameof(minScore));

            var dataList = new List<(long, decimal)>();
            var usedIds = new HashSet<long>();
            while (dataList.Count < n)
            {
                long customerId;
                do
                {
                    long minId = (long)Math.Pow(10, 5); // 100000
                    long maxId = (long)Math.Pow(10, 12) - 1;
                    customerId = (long)(_random.NextDouble() * (maxId - minId) + minId);
                } while (!usedIds.Add(customerId));

                double randomScore = _random.NextDouble() * (double)(maxScore - minScore) + (double)minScore;
                // 保留以为小数
                decimal score = Math.Round((decimal)randomScore, 1);

                dataList.Add((customerId, score));
            }


            return dataList;
        }

    }
}
