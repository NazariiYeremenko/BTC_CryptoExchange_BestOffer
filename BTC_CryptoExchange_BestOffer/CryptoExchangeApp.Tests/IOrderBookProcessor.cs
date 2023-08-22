using CryptoExchangeApp.Models;

namespace CryptoExchangeApp.Tests
{
    public interface IOrderBookProcessor
    {
        Task<List<Offer>> FindBestBuyOfferAsync(List<OrderBook> orderBooksList, decimal desiredBtc);
        Task<List<Offer>> FindBestSellOfferAsync(List<OrderBook> orderBooksList, decimal desiredBtc);
    }
}