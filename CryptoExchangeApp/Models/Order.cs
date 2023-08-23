namespace CryptoExchangeApp.Models;

public class Order
{
    public decimal Amount { get; set; }
    public decimal Price { get; set; }
}

//Class for JSON deserialization