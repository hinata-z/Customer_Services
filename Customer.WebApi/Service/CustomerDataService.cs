using Customer.WebApi.Model;
using System.Reflection.Metadata.Ecma335;

namespace Customer.WebApi.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomerDataService
    {
        private readonly ILogger<CustomerDataService> _logger;

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

    }
}
