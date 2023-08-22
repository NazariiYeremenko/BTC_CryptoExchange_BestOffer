using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using Newtonsoft.Json;

namespace CryptoExchangeApp.Processors
{
    public class OrderBookProcessor
    {
        public static async Task<List<OrderBook>> LoadOrderBooksAsync(string filePath)
        {
            var orderBooks = new List<OrderBook>();

            var lines = await File.ReadAllLinesAsync(filePath);

            foreach (var line in lines)
            {
                var parts = line?.Split('\t');

                if (parts != null && parts.Length != 2)
                {
                    Console.WriteLine($"Invalid line format: {line}");
                    continue;
                }

                var exchangeId = parts?[0];
                var jsonStr = parts?[1];

                if (jsonStr == null) continue;

                var orderBook = JsonConvert.DeserializeObject<OrderBook>(jsonStr);
                try
                {
                    if (orderBook != null)
                    {
                        if (exchangeId != null) orderBook.Id = exchangeId;
                        orderBooks.Add(orderBook);
                    }
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine($"NullReferenceException: {ex.Message}");
                }
            }
            
            return SortAsksAndBidsInOrderBooks(orderBooks);
        }

        public static List<OrderBook> SortAsksAndBidsInOrderBooks(List<OrderBook> orderBooks)
        {
            foreach (var orderBook in orderBooks)
            {
                orderBook.Asks = orderBook.Asks.OrderBy(ask => ask.Order?.Price ?? 0).ToList();
                orderBook.Bids = orderBook.Bids.OrderByDescending(bid => bid.Order?.Price ?? 0).ToList();
            }

            return orderBooks;
        }


        public static void FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBtc)
        {
            var bestOffersPerExchange = new List<List<Offer>>();

            foreach (var orderBook in orderBooksList)
            {
                var bestOffers = new List<Offer>();

                var remainingBtc = desiredBtc;
                //Variable to keep track of EUR balance - to take into account only the number of the most profitable transactions, which theoretically will empty the balance of a certain exchanger.
                var finalEurBalance = orderBook.EurBalance;


                foreach (var ask in orderBook.Asks)
                {
                    if (remainingBtc > 0 && finalEurBalance > 0 && ask.Order != null)
                    {
                        var btcToUse = Math.Min(finalEurBalance / ask.Order.Price, ask.Order.Amount);

                        // Calculate the total EUR required to purchase the maximum BTC amount
                        var totalEurRequired = btcToUse * ask.Order.Price;

                        // Create a new Order object for the offer that can possibly be partial
                        var order = new Order
                        {
                            Amount = btcToUse,
                            Price = ask.Order.Price
                        };

                        bestOffers.Add(new Offer(orderBook, new OrderContainer { Order = order }, totalEurRequired));

                        // Update remaining BTC and EUR balances based on the partial deal
                        remainingBtc -= btcToUse;
                        finalEurBalance -= totalEurRequired;
                    }
                    // Calculate the maximum BTC amount from this bid that can be purchased with the remaining EUR balance on exchanger
                }

                bestOffersPerExchange.Add(bestOffers);

            }

            // Calculate the most profitable combination of deals among all exchanges
            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBtc, TradeType.Buy);

            PrintOffers(mostProfitableCombination, desiredBtc, TradeType.Buy);
        }

        public static void FindBestSellOffer(List<OrderBook> orderBooksList, decimal desiredBtc)
        {
            var totalBtcAvailable = orderBooksList.Sum(orderBook => orderBook.BtcBalance);

            if (totalBtcAvailable < desiredBtc)
            {
                Console.WriteLine("Not enough BTC available to sell.");
                return;
            }

            var bestOffersPerExchange = new List<List<Offer>>();

            foreach (var orderBook in orderBooksList)
            {
                var bestOffers = new List<Offer>();

                var remainingBtc = desiredBtc;
                //Variable to keep track of BTC balance - to take into account only the number of the most profitable transactions, which theoretically will empty the balance of a certain exchanger.
                var finalBtcBalance = orderBook.BtcBalance;


                foreach (var bid in orderBook.Bids)
                {
                    if (remainingBtc <= 0 || finalBtcBalance <= 0 || bid.Order != null)
                    {
                        var eurToUse = Math.Min(finalBtcBalance * bid.Order.Price, bid.Order.Amount * bid.Order.Price);

                        // Calculate the BTC amount to be used in the offer
                        var btcToUse = Math.Min(finalBtcBalance, bid.Order.Amount);

                        // Create a new Order object for the offer that can possibly be partial
                        var order = new Order
                        {
                            Amount = btcToUse,
                            Price = bid.Order.Price
                        };

                        bestOffers.Add(new Offer(orderBook, new OrderContainer { Order = order }, eurToUse, true));

                        // Update remaining BTC and EUR balances based on the deal
                        remainingBtc -= btcToUse;
                        finalBtcBalance -= btcToUse;
                    }
                    else
                    {
                        break; // Exit the loop if remaining BTC or BTC balance is non-positive
                    }
                    
                    // Calculate how much EUR is required to purchase amount of BTC from bid (fully or partial) 
                }

                bestOffersPerExchange.Add(bestOffers);


            }

            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBtc, TradeType.Sell);

            PrintOffers(mostProfitableCombination, desiredBtc, TradeType.Sell);
        }

        private static List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredAmount, TradeType tradeType)
        {
            var numExchanges = bestOffersPerExchange.Count;
            var remainingAmount = desiredAmount;
            var mostProfitableCombination = new List<Offer>();

            while (remainingAmount > 0)
            {
                Offer? bestOffer = null;
                var bestExchangeIndexI = -1;
                var bestExchangeIndexJ = -1;

                for (var i = 0; i < numExchanges; i++)
                {
                    for (var j = 0; j < bestOffersPerExchange[i].Count; j++)
                    {
                        bestOffer = IterateThroughBestOffers(bestOffersPerExchange, tradeType, i, j, bestOffer, ref bestExchangeIndexI, ref bestExchangeIndexJ);
                    }
                }

                if (bestOffer == null)
                {
                    // No more profitable offers left to consider
                    break;
                }

                if (bestOffer.BestOffer.Order != null && remainingAmount >= bestOffer.BestOffer.Order.Amount)
                {
                    remainingAmount -= bestOffer.BestOffer.Order.Amount;
                    bestOffer.RemainingBalance = remainingAmount;
                    mostProfitableCombination.Add(bestOffer);
                    bestOffersPerExchange[bestExchangeIndexI].RemoveAt(bestExchangeIndexJ);
                }
                else if (bestOffer.BestOffer.Order != null && remainingAmount < bestOffer.BestOffer.Order.Amount)
                {
                    // Only part of the offer's BTC amount is used
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

        private static Offer IterateThroughBestOffers(List<List<Offer>> bestOffersPerExchange, TradeType tradeType, int i, int j,
            Offer? bestOffer, ref int bestExchangeIndexI, ref int bestExchangeIndexJ)
        {
            var currentOffer = bestOffersPerExchange[i][j];

            if (bestOffer != null)
            {
                switch (tradeType)
                {
                    case TradeType.Sell when bestOffer.BestOffer.Order != null
                                             && currentOffer.BestOffer.Order != null
                                             && currentOffer.BestOffer.Order.Price <= bestOffer.BestOffer.Order.Price:
                    case TradeType.Buy when bestOffer.BestOffer.Order != null
                                            && currentOffer.BestOffer.Order != null
                                            && currentOffer.BestOffer.Order.Price >= bestOffer.BestOffer.Order.Price:
                        return bestOffer;
                }
            }

            bestOffer = currentOffer;
            bestExchangeIndexI = i;
            bestExchangeIndexJ = j;
            return bestOffer;
        }

        private static void PrintOffers(List<Offer> offers, decimal desiredAmount, TradeType tradeType)
        {
            if (offers.Count > 0)
            {
                var totalEurSum = tradeType == TradeType.Sell
                    ? offers.Sum(offer => offer.TotalEURGained)
                    : offers.Sum(offer => offer.TotalEURRequired);

                var action = tradeType == TradeType.Sell ? "sell" : "buy";
                decimal remainingBalance = -1;
                Console.WriteLine($"Most profitable offers to {action} {desiredAmount} BTC:");
                foreach (var offer in offers)
                {
                    Console.WriteLine($"Exchanger ID: {offer.Exchange.Id}");
                    Console.WriteLine($"{(tradeType == TradeType.Sell ? "BTC" : "EUR")} Balance: {(tradeType == TradeType.Sell ? offer.Exchange.BtcBalance : offer.Exchange.EurBalance)}");
                    Console.WriteLine($"Whole desired amount of BTC to {action}: {desiredAmount}");
                    Console.WriteLine($"Best {(tradeType == TradeType.Sell ? "Bid" : "Ask")} Price per BTC: {offer.BestOffer.Order!.Price}");
                    Console.WriteLine($"BTC to {action}: {offer.BestOffer.Order.Amount}");
                    Console.WriteLine($"Total EUR: {(tradeType == TradeType.Sell ? offer.TotalEURGained : offer.TotalEURRequired):F2}");
                    Console.WriteLine($"Remaining BTC to {action}: {offer.RemainingBalance}");
                    Console.WriteLine();
                    remainingBalance = offer.RemainingBalance;
                }
                Console.WriteLine($"Total EUR Sum: {totalEurSum:F2}");
                if (remainingBalance > 0)
                {
                    //handle the situation when user will empty all his balances
                    Console.WriteLine($"There is not enough money among all of the exchanger's EUR balance to purchase {remainingBalance} BTC. You can purchase only {desiredAmount - remainingBalance} BTC");
                }
            }
            else
            {
                Console.WriteLine($"No suitable combination found to {(tradeType == TradeType.Sell ? "sell" : "buy")} {desiredAmount} BTC.");
            }
        }

        private enum TradeType
        {
            Sell,
            Buy
        }
    }
}
