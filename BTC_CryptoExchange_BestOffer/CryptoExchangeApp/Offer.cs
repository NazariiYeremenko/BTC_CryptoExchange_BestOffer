namespace CryptoExchangeApp;

public class Offer
{
    public OrderBook Exchange { get; }
    public OrderContainer BestOffer { get; }
    public decimal TotalEURRequired { get; }
    public decimal TotalEURGained { get; }
    public decimal RemainingBTC { get; set;  }

    public Offer(OrderBook exchange, OrderContainer offer, decimal totalEURRequired)
    {
        Exchange = exchange;
        BestOffer = offer;
        TotalEURRequired = totalEURRequired;
        TotalEURGained = 0;
    }

    public Offer(OrderBook exchange, OrderContainer offer, decimal totalEURGained, bool isSell)
    {
        Exchange = exchange;
        BestOffer = offer;
        TotalEURRequired = 0;
        TotalEURGained = totalEURGained;
    }
}