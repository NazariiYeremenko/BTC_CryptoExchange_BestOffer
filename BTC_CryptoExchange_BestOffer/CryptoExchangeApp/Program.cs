using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoExchangeApp
{
    class Program
    {

        static void Main(string[] args)
        {
            try
            {
                string json = File.ReadAllText("order_books.json");
                OrderBook orderBooks = JsonConvert.DeserializeObject<OrderBook>(json);

                while (true)
                {
                    Console.Write("Do you want to buy or sell? ");
                    string action = Console.ReadLine().ToLower();

                    if (action != "buy" && action != "sell")
                    {
                        Console.WriteLine("Invalid input. Please enter 'buy' or 'sell'.");
                        continue;
                    }

                    Console.Write("How much BTC do you want to " + action + "? ");
                    if (decimal.TryParse(Console.ReadLine(), out decimal amount))
                    {
                        decimal totalBtc = 0;
                        decimal totalAmount = 0;

                        if (action == "buy")
                        {
                            decimal totalCost = 0;

                            foreach (var ask in orderBooks.Asks)
                            {
                                decimal price = ask.Price;
                                decimal quantity = ask.Amount;

                                if (quantity <= amount)
                                {
                                    totalCost += price * quantity;
                                    totalBtc += quantity;
                                    amount -= quantity;
                                }
                                else
                                {
                                    totalCost += price * amount;
                                    totalBtc += amount;
                                    break;
                                }

                                if (amount <= 0)
                                    break;
                            }

                            Console.WriteLine("You bought " + totalBtc.ToString("0.00000000") + " BTC for EUR " + totalCost.ToString("0.00"));
                        }
                        else
                        {
                            decimal totalRevenue = 0;

                            foreach (var bid in orderBooks.Bids)
                            {
                                decimal price = bid.Price;
                                decimal quantity = bid.Amount;

                                if (quantity <= amount)
                                {
                                    totalRevenue += price * quantity;
                                    totalBtc += quantity;
                                    amount -= quantity;
                                }
                                else
                                {
                                    totalRevenue += price * amount;
                                    totalBtc += amount;
                                    break;
                                }

                                if (amount <= 0)
                                    break;
                            }

                            Console.WriteLine("You sold " + totalBtc.ToString("0.00000000") + " BTC for EUR " + totalRevenue.ToString("0.00"));
                        }

                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid amount. Please enter a valid number.");
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
