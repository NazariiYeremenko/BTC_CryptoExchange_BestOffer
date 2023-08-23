using CryptoExchangeApp.Models;
using static CryptoExchangeApp.Processors.OrderBookProcessor;

namespace CryptoExchangeApp.Tests
{
    public interface IOrderBookProcessor
    {
        List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredAmount, TradeType tradeType);
        Task<List<OrderBook>> LoadOrderBooksAsync(string filePath);
    }
}