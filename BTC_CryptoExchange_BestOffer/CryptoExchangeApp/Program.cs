using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CryptoExchangeApp
{
    internal class Program
    {
        private static void FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBTC)
        {
        var bestOffers = new List<Offer>();

        foreach (var orderBook in orderBooksList)
        {
            foreach (var ask in orderBook.Asks)
            {
                decimal totalEURRequired = desiredBTC * ask.Order.Price;

                if (totalEURRequired <= orderBook.EURBalance)
                {
                    bestOffers.Add(new Offer(orderBook, ask, totalEURRequired));
                }
            }
        }

        if (bestOffers.Count > 0)
        {
            // Sort offers by totalEURRequired in ascending order
            bestOffers.Sort((offer1, offer2) => offer1.TotalEURRequired.CompareTo(offer2.TotalEURRequired));

            var selectedOffers = new List<Offer>();
            decimal totalEURSum = 0;

            foreach (var offer in bestOffers)
            {
                if (totalEURSum + offer.TotalEURRequired <= offer.Exchange.EURBalance)
                {
                    selectedOffers.Add(offer);
                    totalEURSum += offer.TotalEURRequired;

                    if (totalEURSum >= desiredBTC * selectedOffers[0].BestOffer.Order.Price)
                    {
                        break;
                    }
                }
            }

            if (totalEURSum >= desiredBTC * selectedOffers[0].BestOffer.Order.Price)
            {
                Console.WriteLine($"Best offers to buy {desiredBTC} BTC:");
                foreach (var selectedOffer in selectedOffers)
                {
                    Console.WriteLine($"Exchange ID: {selectedOffer.Exchange.Id}");
                    Console.WriteLine($"EUR Balance: {selectedOffer.Exchange.EURBalance}");
                    Console.WriteLine($"Desired BTC to Buy: {desiredBTC}");
                    Console.WriteLine($"Best Ask Price per BTC: {selectedOffer.BestOffer.Order.Price}");
                    Console.WriteLine($"BTC to Buy: {selectedOffer.TotalEURRequired / selectedOffer.BestOffer.Order.Price}");
                    Console.WriteLine($"Total EUR: {selectedOffer.TotalEURRequired}");
                    Console.WriteLine();
                }
                Console.WriteLine($"Total EUR Sum: {totalEURSum}");
            }
            else
            {
                Console.WriteLine($"Not enough funds available to buy {desiredBTC} BTC.");
            }
        }
        else
        {
            Console.WriteLine($"No suitable offer found to buy {desiredBTC} BTC.");
        }
        }

        private static void FindBestSellOffer(List<OrderBook> orderBooksList, decimal desiredBTC)
        {
            var totalBTCAvailable = orderBooksList.Sum(orderBook => orderBook.BTCBalance);

            if (totalBTCAvailable < desiredBTC)
            {
                Console.WriteLine("Not enough BTC available to sell.");
                return;
            }

            var bestOffersPerExchange = new List<List<Offer>>();

            foreach (var orderBook in orderBooksList)
            {
                var bestOffers = new List<Offer>();

                var remainingBTC = desiredBTC;
                //Variable to keep track of balance - to take into account only the number of the most profitable transactions, which theoretically will empty the balance of a certain exchanger.
                var finalBTCbalance = orderBook.BTCBalance;
                // Create a list of bids sorted by total EUR gained in descending order
                var sortedBids = orderBook.Bids.OrderByDescending(bid => bid.Order.Price);

                foreach (var bid in sortedBids)
                {
                    if (remainingBTC > 0 && finalBTCbalance > 0)
                    {
                        var totalEURGained = bid.Order.Amount * bid.Order.Price;
                        bestOffers.Add(new Offer(orderBook, bid, totalEURGained, true));
                        remainingBTC -= bid.Order.Amount;
                        finalBTCbalance -= bid.Order.Amount;
                    }
                }

                bestOffersPerExchange.Add(bestOffers);
            }

            // Calculate the most profitable combination of deals among all exchanges
            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBTC);

            // Print the most profitable combination
            if (mostProfitableCombination is { Count: > 0 })
            {
                var totalEURSum = mostProfitableCombination.Sum(offer => offer.TotalEURGained);
                Console.WriteLine($"Most profitable offers to sell {desiredBTC} BTC:");
                foreach (var offer in mostProfitableCombination)
                {
                    Console.WriteLine($"Exchanger ID: {offer.Exchange.Id}");
                    Console.WriteLine($"Exchanger' BTC Balance: {offer.Exchange.BTCBalance}"); 
                    Console.WriteLine($"Whole desired amount of BTC to Sell: {desiredBTC}");
                    Console.WriteLine($"Best Bid Price per BTC: {offer.BestOffer.Order.Price}");
                    Console.WriteLine($"BTC to Sell: {offer.BestOffer.Order.Amount}");
                    Console.WriteLine($"Total EUR: {offer.TotalEURGained:F2}");
                    Console.WriteLine($"Remaining BTC to Sell: {offer.RemainingBTC}"); 
                    Console.WriteLine();
                }
                Console.WriteLine($"Total EUR Sum: {totalEURSum:F2}");
            }
            else
            {
                Console.WriteLine($"No suitable combination found to sell {desiredBTC} BTC.");
            }
        }


        private static List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredBTC)
        {
            var numExchanges = bestOffersPerExchange.Count;
            var remainingBTC = desiredBTC;
            var mostProfitableCombination = new List<Offer>();

            while (remainingBTC > 0)
            {
                Offer bestOffer = null;
                var bestExchangeIndexI = -1;
                var bestExchangeIndexJ = -1;

                for (var i = 0; i < numExchanges; i++)
                {
                    for (var j = 0; j < bestOffersPerExchange[i].Count; j++)
                    {
                        var currentOffer = bestOffersPerExchange[i][j];

                        if (bestOffer != null && currentOffer.BestOffer.Order.Price <= bestOffer.BestOffer.Order.Price)
                            continue;
                        bestOffer = currentOffer;
                        bestExchangeIndexI = i;
                        bestExchangeIndexJ = j;
                    }
                }

                if (bestOffer == null)
                {
                    // No more profitable offers left to consider
                    break;
                }

                if (remainingBTC >= bestOffer.BestOffer.Order.Amount)
                {
                    remainingBTC -= bestOffer.BestOffer.Order.Amount;
                    bestOffer.RemainingBTC = remainingBTC;
                    mostProfitableCombination.Add(bestOffer);
                    bestOffersPerExchange[bestExchangeIndexI].RemoveAt(bestExchangeIndexJ);
                }
                else if(remainingBTC < bestOffer.BestOffer.Order.Amount)
                {
                    // Only part of the offer's BTC amount is used
                    var partialOffer = new Offer(
                        bestOffer.Exchange,
                        new OrderContainer { Order = new Order { Amount = remainingBTC, Price = bestOffer.BestOffer.Order.Price } },
                        remainingBTC * bestOffer.BestOffer.Order.Price,
                        true
                    );
            
                    mostProfitableCombination.Add(partialOffer);
                    remainingBTC = 0;
                }

            }

            return mostProfitableCombination;
        }

        static void Main(string[] args)
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

                    var exchangeId = parts[0]; // Extract exchanger' ID
                    var jsonStr = parts[1];

                    var orderBook = JsonConvert.DeserializeObject<OrderBook>(jsonStr);
                    try
                    {
                        if (orderBook == null) continue;
                        orderBook.Id = exchangeId; // Assign exchange ID to exchanger 
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
                    var action = Console.ReadLine().ToLower();

                    if (action != "buy" && action != "sell")
                    {
                        Console.WriteLine("Invalid input. Please enter 'buy' or 'sell'.");
                        continue;
                    }

                    Console.Write("How much BTC do you want to " + action + "? ");
                    if (decimal.TryParse(Console.ReadLine(), out decimal amount))
                    {
                        if (action == "buy")
                        {
                            FindBestBuyOffer(orderBooksList, amount);
                        }
                        else
                        {
                            FindBestSellOffer(orderBooksList, amount);
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


