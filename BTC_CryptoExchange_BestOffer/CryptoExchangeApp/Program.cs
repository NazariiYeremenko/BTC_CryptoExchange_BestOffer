using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CryptoExchangeApp
{
    class Program
    {
 static void FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBTC)
    {
        List<Offer> bestOffers = new List<Offer>();

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

            List<Offer> selectedOffers = new List<Offer>();
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

 static void FindBestSellOffer(List<OrderBook> orderBooksList, decimal desiredBTC)
{
    decimal totalBTCAvailable = orderBooksList.Sum(orderBook => orderBook.BTCBalance);
    
    if (totalBTCAvailable < desiredBTC)
    {
        Console.WriteLine("Not enough BTC available to sell.");
        return;
    }

    List<List<Offer>> bestOffersPerExchange = new List<List<Offer>>();

    foreach (var orderBook in orderBooksList)
    {
        List<Offer> bestOffers = new List<Offer>();

        // Create a list of bids sorted by total EUR gained in descending order
        var sortedBids = orderBook.Bids.OrderByDescending(bid =>  bid.Order.Price);
        decimal remainingBTC = orderBook.BTCBalance;

        foreach (var bid in sortedBids)
        {
            if (remainingBTC >= bid.Order.Amount)
            {
                decimal totalEURGained = bid.Order.Amount * bid.Order.Price;
                bestOffers.Add(new Offer(orderBook, bid, totalEURGained, true));
                remainingBTC -= bid.Order.Amount;
            }
        }

        bestOffersPerExchange.Add(bestOffers);
    }

    // Calculate the most profitable combination of deals among all exchanges
    List<Offer> mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBTC);

    // Print the most profitable combination
    if (mostProfitableCombination != null && mostProfitableCombination.Count > 0)
    {
        decimal totalEURSum = mostProfitableCombination.Sum(offer => offer.TotalEURGained);
        Console.WriteLine($"Most profitable offers to sell {desiredBTC} BTC:");
        foreach (var offer in mostProfitableCombination)
        {
            Console.WriteLine($"Exchange ID: {offer.Exchange.Id}");
            Console.WriteLine($"BTC Balance: {offer.Exchange.BTCBalance}");
            Console.WriteLine($"Desired BTC to Sell: {desiredBTC}");
            Console.WriteLine($"Best Bid Price per BTC: {offer.BestOffer.Order.Price}");
            Console.WriteLine($"BTC to Sell: {offer.BestOffer.Order.Amount}");
            Console.WriteLine($"Total EUR: {offer.TotalEURGained}");
            Console.WriteLine();
        }
        Console.WriteLine($"Total EUR Sum: {totalEURSum}");
    }
    else
    {
        Console.WriteLine($"No suitable combination found to sell {desiredBTC} BTC.");
    }
}


 static List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredBTC)
 {
     int numExchanges = bestOffersPerExchange.Count;
     decimal remainingBTC = desiredBTC;
     List<Offer> mostProfitableCombination = new List<Offer>();

     while (remainingBTC > 0)
     {
         Offer bestOffer = null;
         int bestExchangeIndex = -1;

         for (int i = 0; i < numExchanges; i++)
         {
             if (bestOffersPerExchange[i].Count > 0)
             {
                 var currentOffer = bestOffersPerExchange[i][0];

                 if (bestOffer == null || currentOffer.BestOffer.Order.Price < bestOffer.BestOffer.Order.Price)
                 {
                     bestOffer = currentOffer;
                     bestExchangeIndex = i;
                 }
             }
         }


         if (bestOffer == null)
         {
             // No more profitable offers left to consider
             break;
         }

         if (remainingBTC >= bestOffer.BestOffer.Order.Amount)
         {
             mostProfitableCombination.Add(bestOffer);
             remainingBTC -= bestOffer.BestOffer.Order.Amount;
             bestOffersPerExchange[bestExchangeIndex].RemoveAt(0);
         }
         else
         {
             // Only part of the offer's BTC amount is used
             var partialOffer = bestOffer;
             partialOffer.BestOffer.Order.Amount = remainingBTC;
/*             partialOffer.TotalEURGained = remainingBTC * bestOffer.BestOffer.Order.Price;*/
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
                string[] lines = File.ReadAllLines("order_books_data.json");

                List<OrderBook> orderBooksList = new List<OrderBook>();

                foreach (string line in lines)
                {
                    string[] parts = line.Split('\t');

                    if (parts.Length != 2)
                    {
                        Console.WriteLine($"Invalid line format: {line}");
                        continue;
                    }

                    string timestampStr = parts[0];
                    string jsonStr = parts[1];

                    string exchangeId = jsonStr.Substring(2, 8); // Extract exchange ID
                    OrderBook orderBook = JsonConvert.DeserializeObject<OrderBook>(jsonStr);
                    orderBook.Id = exchangeId; // Assign exchange ID to exchanger 

                    orderBooksList.Add(orderBook);
                }

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


