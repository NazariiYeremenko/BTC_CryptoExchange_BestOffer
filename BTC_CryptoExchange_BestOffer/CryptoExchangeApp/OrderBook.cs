namespace CryptoExchangeApp;

public class OrderBook
{
    public string Id { get; set; }
    public decimal EURBalance { get; private set; }
    public decimal BTCBalance { get; private set; }
    public List<OrderContainer> Bids { get; set; }
    public List<OrderContainer> Asks { get; set; }

    public OrderBook(string id)
    {
        Id = id;
        EURBalance = GenerateRandomBalance(); // Generate random EUR balance between 0 and 10000
        BTCBalance = GenerateRandomBTCBalance(); // Generate random BTC balance between 0 and 10
    }

    private decimal GenerateRandomBalance()
    {
        Random random = new Random();
        return Math.Round((decimal)random.NextDouble() * 10000, 2);
    }

    private decimal GenerateRandomBTCBalance()
    {
        Random random = new Random();
        return Math.Round((decimal)random.NextDouble() * 10, 8);
    }
}

//lasses for JSON deserialization