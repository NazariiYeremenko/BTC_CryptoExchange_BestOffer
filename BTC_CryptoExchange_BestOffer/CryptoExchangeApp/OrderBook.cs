namespace CryptoExchangeApp;

//lasses for JSON deserialization
public class OrderBook
{
    public DateTime AcqTime { get; set; }
    public List<Order> Bids { get; set; }
    public List<Order> Asks { get; set; }
}