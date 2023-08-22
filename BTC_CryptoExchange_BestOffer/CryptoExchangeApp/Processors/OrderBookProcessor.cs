using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using static CryptoExchangeApp.Program;

namespace CryptoExchangeApp.Processors
{
    internal class OrderBookProcessor
    {
        public static void FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBtc)
        {
            var bestOffersPerExchange = new List<List<Offer>>();

            foreach (var orderBook in orderBooksList)
            {
                var bestOffers = new List<Offer>();

                var remainingBtc = desiredBtc;
                var finalEurBalance = orderBook.EurBalance;

                var sortedAsks = orderBook.Asks.OrderBy(ask => ask.Order?.Price ?? 0);

                foreach (var bid in sortedAsks)
                {
                    if (remainingBtc > 0 && finalEurBalance > 0)
                    {
                        if (bid.Order != null)
                        {
                            var totalEurRequired = bid.Order.Amount * bid.Order.Price;
                            bestOffers.Add(new Offer(orderBook, bid, totalEurRequired));
                        }

                        if (bid.Order == null) continue;
                        remainingBtc -= bid.Order.Amount;
                        finalEurBalance -= bid.Order.Amount * bid.Order.Price;
                    }
                }

                bestOffersPerExchange.Add(bestOffers);
            }

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
                var finalBtcBalance = orderBook.BtcBalance;

                var sortedBids = orderBook.Bids.OrderByDescending(bid => bid.Order?.Price ?? 0);

                foreach (var bid in sortedBids)
                {
                    if (remainingBtc <= 0 || finalBtcBalance <= 0) continue;
                    if (bid.Order != null)
                    {
                        var totalEurGained = bid.Order.Amount * bid.Order.Price;
                        bestOffers.Add(new Offer(orderBook, bid, totalEurGained, true));
                    }
                    else continue;
                    remainingBtc -= bid.Order.Amount;
                    finalBtcBalance -= bid.Order.Amount;
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
                                    continue;
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

                if (bestOffer.BestOffer.Order != null && remainingAmount >= bestOffer.BestOffer.Order.Amount)
                {
                    remainingAmount -= bestOffer.BestOffer.Order.Amount;
                    bestOffer.RemainingBalance = remainingAmount;
                    mostProfitableCombination.Add(bestOffer);
                    bestOffersPerExchange[bestExchangeIndexI].RemoveAt(bestExchangeIndexJ);
                }
                else if (bestOffer.BestOffer.Order != null && remainingAmount < bestOffer.BestOffer.Order.Amount)
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

        private static void PrintOffers(List<Offer> offers, decimal desiredAmount, TradeType tradeType)
        {
            if (offers.Count > 0)
            {
                var totalEurSum = tradeType == TradeType.Sell
                    ? offers.Sum(offer => offer.TotalEURGained)
                    : offers.Sum(offer => offer.TotalEURRequired);

                var action = tradeType == TradeType.Sell ? "sell" : "buy";

                Console.WriteLine($"Most profitable offers to {action} {desiredAmount} BTC:");
                foreach (var offer in offers)
                {
                    Console.WriteLine($"Exchanger ID: {offer.Exchange.Id}");
                    Console.WriteLine($"{(tradeType == TradeType.Sell ? "EUR" : "BTC")} Balance: {offer.Exchange.BtcBalance}");
                    Console.WriteLine($"Whole desired amount of BTC to {action}: {desiredAmount}");
                    Console.WriteLine($"Best {(tradeType == TradeType.Sell ? "Bid" : "Ask")} Price per BTC: {offer.BestOffer.Order.Price}");
                    Console.WriteLine($"BTC to {action}: {offer.BestOffer.Order.Amount}");
                    Console.WriteLine($"Total EUR: {(tradeType == TradeType.Sell ? offer.TotalEURGained : offer.TotalEURRequired):F2}");
                    Console.WriteLine($"Remaining BTC to {action}: {offer.RemainingBalance}");
                    Console.WriteLine();
                }
                Console.WriteLine($"Total EUR Sum: {totalEurSum:F2}");
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
