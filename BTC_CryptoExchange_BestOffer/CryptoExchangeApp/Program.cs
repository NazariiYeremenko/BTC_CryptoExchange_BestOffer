using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using CryptoExchangeApp.Processors;
using CryptoExchangeApp.Models;

namespace CryptoExchangeApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var lines = File.ReadAllLines("order_books_data.json");

                var orderBooksList = new List<OrderBook>();

                foreach (var line in lines)
                {
                    var parts = line.Split('\t');

                    if (parts.Length != 2)
                    {
                        Console.WriteLine($"Invalid line format: {line}");
                        continue;
                    }

                    var exchangeId = parts[0];
                    var jsonStr = parts[1];

                    var orderBook = JsonConvert.DeserializeObject<OrderBook>(jsonStr);
                    try
                    {
                        if (orderBook == null) continue;
                        orderBook.Id = exchangeId;
                        orderBooksList.Add(orderBook);
                    }
                    catch (NullReferenceException ex)
                    {
                        Console.WriteLine($"NullReferenceException: {ex.Message}");
                    }
                }

                while (true)
                {
                    Console.Write("Do you want to buy or sell? ");
                    var action = Console.ReadLine()?.ToLower();

                    if (action != "buy" && action != "sell")
                    {
                        Console.WriteLine("Invalid input. Please enter 'buy' or 'sell'.");
                        continue;
                    }

                    Console.Write($"How much BTC do you want to {action}? ");
                    if (decimal.TryParse(Console.ReadLine(), out var amount))
                    {
                        if (action == "buy")
                        {
                            OrderBookProcessor.FindBestBuyOffer(orderBooksList, amount);
                        }
                        else
                        {
                            OrderBookProcessor.FindBestSellOffer(orderBooksList, amount);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a valid decimal number.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}
