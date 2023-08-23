using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CryptoExchangeApp.Processors;
using CryptoExchangeApp.Models;
using static CryptoExchangeApp.Processors.OrderBookProcessor;

namespace CryptoExchangeApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
                var filePath = Path.Combine(solutionDirectory ?? string.Empty, "order_books_data.json");
                var orderBooksList = await LoadOrderBooksAsync(filePath);

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
                            PrintOffers(FindBestBuyOffer(orderBooksList, amount), amount, TradeType.Buy);
                            
                        }
                        else
                        {
                            PrintOffers(FindBestSellOffer(orderBooksList, amount), amount, TradeType.Sell);
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