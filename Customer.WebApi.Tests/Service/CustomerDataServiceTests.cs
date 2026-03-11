using Microsoft.VisualStudio.TestTools.UnitTesting;
using Customer.WebApi.Service;

namespace Customer.WebApi.Service.Tests
{
    [TestClass()]
    public class CustomerDataServiceTests
    {
        [TestMethod()]
        public void InitCustomerScoreDataTest()
        {
            // Arrange
            var service = new CustomerDataService();

            // Act
            var data = service.InitCustomerScoreData();

            // Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(10, data.Count, "初始化数据条数应固定为 10 条。");

            // CustomerId 唯一
            var distinctCustomerIds = data.Select(x => x.CustomerId).Distinct().Count();
 
            CollectionAssert.Contains(data.Select(x => x.CustomerId).ToList(), 38819L);
            Assert.AreEqual(92m, data.Single(x => x.CustomerId == 38819L).Score);
        }
    }
}