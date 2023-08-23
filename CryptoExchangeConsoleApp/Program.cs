using CryptoExchangeConsoleApp.Processors;
using static CryptoExchangeApp.Processors.OrderBookProcessor;

namespace CryptoExchangeConsoleApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                var orderBooksList = await LoadOrderBooksAsync();

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
                            ConsolePrinter.PrintOffers(FindBestBuyOffer(orderBooksList, amount), amount, TradeType.Buy);
                        }
                        else
                        {
                            ConsolePrinter.PrintOffers(FindBestSellOffer(orderBooksList, amount), amount, TradeType.Sell);
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