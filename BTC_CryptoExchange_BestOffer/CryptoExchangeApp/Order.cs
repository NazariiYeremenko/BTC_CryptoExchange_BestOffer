﻿namespace CryptoExchangeApp;

//Class for JSON deserialization
public class Order
{
    public string Id { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; }
    public string Kind { get; set; }
    public decimal Amount { get; set; }
    public decimal Price { get; set; }
}