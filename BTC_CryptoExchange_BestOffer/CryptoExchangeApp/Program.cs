using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CryptoExchangeApp.Processors;
using CryptoExchangeApp.Models;

namespace CryptoExchangeApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var orderBooksList = await OrderBookProcessor.LoadOrderBooksAsync("order_books_data.json");

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
