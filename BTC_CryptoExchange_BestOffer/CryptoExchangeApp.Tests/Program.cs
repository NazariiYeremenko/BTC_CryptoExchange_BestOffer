using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using CryptoExchangeApp.Tests;
using Newtonsoft.Json;

namespace CryptoExchangeApp
{
    internal class Program
    {
        public Program(IFileReader fileReader, IOrderBookProcessor mockProcessorObject)
        {
            throw new NotImplementedException();
        }

        private static async Task Main(string[] args)
        {
            var orderBooksList = await OrderBookProcessor.LoadOrderBooksAsync("order_books_data.json");

            try
            {
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
