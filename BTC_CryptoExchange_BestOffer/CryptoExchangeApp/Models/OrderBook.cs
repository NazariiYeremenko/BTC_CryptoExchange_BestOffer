namespace CryptoExchangeApp.Models;

public class OrderBook
{
    public string Id { get; set; }
    public decimal EurBalance { get; set; }
    public decimal BtcBalance { get; set; }
    public List<OrderContainer> Bids { get; set; }
    public List<OrderContainer> Asks { get; set; }

    public OrderBook(string id, List<OrderContainer> bids, List<OrderContainer> asks)
    {
        Id = id;
        Bids = bids;
        Asks = asks;
        EurBalance = GenerateRandomEurBalance(); // Generate random EUR balance between 0 and 10000
        BtcBalance = GenerateRandomBtcBalance(); // Generate random BTC balance between 0 and 10
    }

    private static decimal GenerateRandomEurBalance()
    {
        var random = new Random();
        return Math.Round((decimal)random.NextDouble() * 10000, 2);
    }

    private static decimal GenerateRandomBtcBalance()
    {
        var random = new Random();
        return Math.Round((decimal)random.NextDouble() * 10, 8);
    }
}

//lasses for JSON deserialization