using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using Moq;
using Xunit;

namespace CryptoExchangeApp.Tests
{
    public class OrderBookProcessorTests
    {
        [Fact]
        public async Task TestFindBestBuyOfferAsync()
        {
            // Arrange
            var orderBookJson = "{\"AcqTime\":\"2019-01-29T11:59:59.5930067Z\",\"Bids\":[{\"Order\":{\"Id\":null,\"Time\":\"0001-01-01T00:00:00\",\"Type\":\"Buy\",\"Kind\":\"Limit\",\"Amount\":0.49667568,\"Price\":2955.46}},{\"Order\":{\"Id\":null,\"Time\":\"0001-01-01T00:00:00\",\"Type\":\"Buy\",\"Kind\":\"Limit\",\"Amount\":0.034,\"Price\":2954.98}}],\"Asks\":[{\"Order\":{\"Id\":null,\"Time\":\"0001-01-01T00:00:00\",\"Type\":\"Sell\",\"Kind\":\"Limit\",\"Amount\":0.01,\"Price\":2960.4}},{\"Order\":{\"Id\":null,\"Time\":\"0001-01-01T00:00:00\",\"Type\":\"Sell\",\"Kind\":\"Limit\",\"Amount\":0.5,\"Price\":2960.42}}]}";
            
            var mockFileReader = new Mock<IFileReader>();
            mockFileReader.Setup(fr => fr.ReadAllTextAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(orderBookJson));

            var desiredBtc = 1;
            var orderBooksList = new List<OrderBook>();
            var mockProcessor = new Mock<IOrderBookProcessor>();
            mockProcessor.Setup(p => p.FindBestBuyOfferAsync(orderBooksList, It.IsAny<decimal>()))
                .Returns((Task<List<Offer>>)Task.CompletedTask);


/*            var actualResult = OrderBookProcessor.FindBestBuyOffer(orderBooksList,desiredBtc);*/

            // Assert
            mockProcessor.Verify(p => p.FindBestBuyOfferAsync(orderBooksList, It.IsAny<decimal>()), Times.Once);
        }
    }
}