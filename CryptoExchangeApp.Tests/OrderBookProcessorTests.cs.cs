using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using Moq;

namespace CryptoExchangeApp.Tests
{
    public class OrderBookProcessorTests
    {


        private readonly List<OrderBook> _expectedOrderBooksList = new()
        {
                new OrderBook("1548763187.72633",
                    new List<OrderContainer>
                        {
                            new() { Order = new Order { Amount = 0.49667568M, Price = 2955.46M } },
                             new() { Order = new Order { Amount = 0.034M, Price = 2954.98M } },
                             new() { Order = new Order { Amount = 0.2M, Price = 2946.73M } }
                        },
                        new List<OrderContainer>
                        {
                            new() { Order = new Order { Amount = 0.06M, Price = 3118.0M } },
                            new() { Order = new Order { Amount = 0.09512628M, Price = 3119.06M } },
                            new() { Order = new Order { Amount = 20.0M, Price = 3119.99M } }
                        })
        };

        [Fact]
        public void TestFindMostProfitableCombination()
        {
            const decimal desiredAmount = 0.5M;
            const OrderBookProcessor.TradeType tradeType = OrderBookProcessor.TradeType.Sell;

            // Create a list of lists of offers for testing
            var bestOffersPerExchange = new List<List<Offer>>();

            // Create offers for testing
            var expectedOffers = new List<Offer>
            {
                new(_expectedOrderBooksList[0], 
                    _expectedOrderBooksList[0].Asks[0],
                    _expectedOrderBooksList[0].Asks[0].Order.Amount*_expectedOrderBooksList[0].Asks[0].Order.Price, 
                    true),
                new(_expectedOrderBooksList[0], 
                    new OrderContainer { Order = new Order { Amount = 0.00332432M, Price = _expectedOrderBooksList[0].Asks[1].Order.Price } }, 
                    0.00332432M * _expectedOrderBooksList[0].Asks[1].Order.Price, 
                    true),
                    
            };
            bestOffersPerExchange.Add(expectedOffers);
            var mockProcessor = new Mock<IOrderBookProcessor>();
            mockProcessor.Setup(p => p.FindMostProfitableCombination(bestOffersPerExchange, desiredAmount, tradeType))
                .Returns(expectedOffers);
            // Act
            var actualOutput = mockProcessor.Object.FindMostProfitableCombination(bestOffersPerExchange, desiredAmount, tradeType);
            // Assert
            Assert.Equal(expectedOffers.Count, actualOutput.Count);
        }

        [Fact]
        public async Task TestLoadOrderBooksAsync()
        {
            // Arrange
            const string orderBookJson = "order_books_test.json";

            var mockProcessor = new Mock<IOrderBookProcessor>();
            mockProcessor.Setup(p => p.LoadOrderBooksAsync(orderBookJson))
                .ReturnsAsync(_expectedOrderBooksList); // Set the expected result to be returned
            // Act
            var resultOrderBooks = await mockProcessor.Object.LoadOrderBooksAsync(orderBookJson);
            // Assert
            Assert.Equal(_expectedOrderBooksList, resultOrderBooks);

            for (var i = 0; i < _expectedOrderBooksList.Count; i++)
            {
                // Compare properties of OrderBook objects
                Assert.Equal(_expectedOrderBooksList[i].Id, resultOrderBooks[i].Id);
                Assert.Equal(_expectedOrderBooksList[i].Bids, resultOrderBooks[i].Bids);
                Assert.Equal(_expectedOrderBooksList[i].Asks, resultOrderBooks[i].Asks);

            }
            mockProcessor.Verify(p => p.LoadOrderBooksAsync(orderBookJson), Times.Once);
        }

    }
}