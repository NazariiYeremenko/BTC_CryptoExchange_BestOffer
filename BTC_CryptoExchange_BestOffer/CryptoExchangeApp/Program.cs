using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CryptoExchangeApp
{
    internal class Program
    {
        public enum TradeType
        {
            Sell,
            Buy
        }
       private static void FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBTC)
{

    var bestOffersPerExchange = new List<List<Offer>>();

    foreach (var orderBook in orderBooksList)
    {
        var bestOffers = new List<Offer>();

        var remainingBTC = desiredBTC;
        //Variable to keep track of balance - to take into account only the number of the most profitable transactions, which theoretically will empty the balance of a certain exchanger.
        var finalEURBalance = orderBook.EURBalance;

        var sortedAsks = orderBook.Asks.OrderBy(ask => ask.Order.Price);

        foreach (var bid in sortedAsks)
        {
            if (remainingBTC > 0 && finalEURBalance > 0)
            {
                var totalEURRequired = bid.Order.Amount * bid.Order.Price;
                bestOffers.Add(new Offer(orderBook, bid, totalEURRequired));
                remainingBTC -= bid.Order.Amount;
                finalEURBalance -= bid.Order.Amount*bid.Order.Price;
            }
        }

        bestOffersPerExchange.Add(bestOffers);
    }

    var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBTC, TradeType.Buy);

    if (mostProfitableCombination is { Count: > 0 })
    {
        var totalEURSum = mostProfitableCombination.Sum(offer => offer.TotalEURRequired);
        Console.WriteLine($"Most profitable offers to buy {desiredBTC} BTC:");
        foreach (var offer in mostProfitableCombination)
        {
            Console.WriteLine($"Exchange ID: {offer.Exchange.Id}");
            Console.WriteLine($"EUR Balance: {offer.Exchange.EURBalance}");
            Console.WriteLine($"Whole desired amount of BTC to Sell: {desiredBTC}");
            Console.WriteLine($"Best Ask Price per BTC: {offer.BestOffer.Order.Price}");
            Console.WriteLine($"BTC to Buy: {offer.BestOffer.Order.Amount}");
            Console.WriteLine($"Total EUR: {offer.TotalEURRequired:F2}");
            Console.WriteLine($"Remaining BTC to Buy: {offer.RemainingBalance:F2}");
            Console.WriteLine();
        }
        Console.WriteLine($"Total EUR Sum: {totalEURSum:F2}");
    }
    else
    {
        Console.WriteLine($"No suitable combination found to buy {desiredBTC} BTC.");
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
            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBTC, TradeType.Sell );

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
                    Console.WriteLine($"Remaining BTC to Sell: {offer.RemainingBalance}"); 
                    Console.WriteLine();
                }
                Console.WriteLine($"Total EUR Sum: {totalEURSum:F2}");
            }
            else
            {
                Console.WriteLine($"No suitable combination found to sell {desiredBTC} BTC.");
            }
        }


private static List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredAmount, TradeType tradeType)
{
    int numExchanges = bestOffersPerExchange.Count;
    decimal remainingAmount = desiredAmount;
    List<Offer> mostProfitableCombination = new List<Offer>();

    while (remainingAmount > 0)
    {
        Offer bestOffer = null;
        int bestExchangeIndexI = -1;
        int bestExchangeIndexJ = -1;

        for (int i = 0; i < numExchanges; i++)
        {
            for (int j = 0; j < bestOffersPerExchange[i].Count; j++)
            {
                var currentOffer = bestOffersPerExchange[i][j];

                if (bestOffer != null)
                {
                    switch (tradeType)
                    {
                        case TradeType.Sell when currentOffer.BestOffer.Order.Price <= bestOffer.BestOffer.Order.Price:
                        case TradeType.Buy when currentOffer.BestOffer.Order.Price >= bestOffer.BestOffer.Order.Price:
                            continue;
/*                        default:
                            throw new ArgumentOutOfRangeException(nameof(tradeType), tradeType, null);*/
                    }
                }

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

        if (remainingAmount >= bestOffer.BestOffer.Order.Amount)
        {
            remainingAmount -= bestOffer.BestOffer.Order.Amount;
            bestOffer.RemainingBalance = remainingAmount;
            mostProfitableCombination.Add(bestOffer);
            bestOffersPerExchange[bestExchangeIndexI].RemoveAt(bestExchangeIndexJ);
        }
        else if (remainingAmount < bestOffer.BestOffer.Order.Amount)
        {
            var partialOffer = tradeType switch
            {
                TradeType.Sell => new Offer(bestOffer.Exchange,
                    new OrderContainer
                    {
                        Order = new Order { Amount = remainingAmount, Price = bestOffer.BestOffer.Order.Price }
                    }, remainingAmount * bestOffer.BestOffer.Order.Price, true),
                TradeType.Buy => new Offer(bestOffer.Exchange,
                    new OrderContainer
                    {
                        Order = new Order { Amount = remainingAmount, Price = bestOffer.BestOffer.Order.Price }
                    }, remainingAmount * bestOffer.BestOffer.Order.Price),
                _ => throw new ArgumentOutOfRangeException(nameof(tradeType), "Invalid trade type.")
            };

            mostProfitableCombination.Add(partialOffer);
            remainingAmount = 0;
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


