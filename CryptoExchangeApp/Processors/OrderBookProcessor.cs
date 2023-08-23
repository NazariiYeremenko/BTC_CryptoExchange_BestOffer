using CryptoExchangeApp.Models;
using Newtonsoft.Json;

namespace CryptoExchangeApp.Processors
{
    public class OrderBookProcessor
    {
        public static async Task<List<OrderBook>> LoadOrderBooksAsync()
        {
            var desiredProjectPath = GetPathToReadFile();
            var orderBooks = new List<OrderBook>();
            var lines = await File.ReadAllLinesAsync(desiredProjectPath);
            foreach (var line in lines)
            {
                await DeserializeJsonObjectFromStringLine(line, orderBooks);
            }
            return SortAsksAndBidsInOrderBooks(orderBooks);
        }

        private static string GetPathToReadFile()
        {
            const string rootFolder = "BTC_CryptoExchange_BestOffer";
            // Navigate to the root folder from the current directory
            var currentDirectory = Directory.GetCurrentDirectory();
            while (!Path.GetFileName(currentDirectory).Equals(rootFolder))
            {
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                if (currentDirectory == null)
                {
                    throw new Exception("Root folder not found.");
                }
            }
            // Combine the solution root folder with the file path
            return Path.Combine(currentDirectory, "order_books_data.json");
        }

        private static async Task DeserializeJsonObjectFromStringLine(string line, List<OrderBook> orderBooks)
        {
            var parts = line?.Split('\t');

            if (parts != null && parts.Length != 2)
            {
                Console.WriteLine($"Invalid line format: {line}");
                return;
            }
            var exchangeId = parts?[0];
            var jsonStr = parts?[1];

            if (jsonStr == null) return;
            var orderBook = await Task.Run(() => JsonConvert.DeserializeObject<OrderBook>(jsonStr));
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

        public static List<OrderBook> SortAsksAndBidsInOrderBooks(List<OrderBook> orderBooks)
        {
            foreach (var orderBook in orderBooks)
            {
                orderBook.Asks = orderBook.Asks.OrderBy(ask => ask.Order?.Price ?? 0).ToList();
                orderBook.Bids = orderBook.Bids.OrderByDescending(bid => bid.Order?.Price ?? 0).ToList();
            }
            return orderBooks;
        }


        public static List<Offer> FindBestBuyOffer(List<OrderBook> orderBooksList, decimal desiredBtc)
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
                        // Calculate the maximum BTC amount from this bid that can be purchased with the remaining EUR balance on exchanger
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
                    else
                    {
                        break; // Exit the loop if remaining BTC or BTC balance is non-positive
                    }
                }
                bestOffersPerExchange.Add(bestOffers);
            }
            // Calculate the most profitable combination of deals among all exchanges
            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBtc, TradeType.Buy);
            return mostProfitableCombination;
        }

        public static List<Offer> FindBestSellOffer(List<OrderBook> orderBooksList, decimal desiredBtc)
        {
            var totalBtcAvailable = orderBooksList.Sum(orderBook => orderBook.BtcBalance);

            if (totalBtcAvailable < desiredBtc)
            {
                Console.WriteLine("Not enough BTC available to sell.");
                return null;
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
                }
                bestOffersPerExchange.Add(bestOffers);
            }
            var mostProfitableCombination = FindMostProfitableCombination(bestOffersPerExchange, desiredBtc, TradeType.Sell);

            return mostProfitableCombination;
        }

        public static List<Offer> FindMostProfitableCombination(List<List<Offer>> bestOffersPerExchange, decimal desiredAmount, TradeType tradeType)
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
                    CreatePartialOrder(tradeType, bestOffer, remainingAmount, mostProfitableCombination);
                    remainingAmount = 0;
                }
            }
            return mostProfitableCombination;
        }

        private static void CreatePartialOrder(TradeType tradeType, Offer bestOffer, decimal remainingAmount, List<Offer> mostProfitableCombination)
        {
            if (mostProfitableCombination == null) throw new ArgumentNullException(nameof(mostProfitableCombination));
            // Only part of the offer's BTC amount is used
            if (bestOffer.BestOffer.Order == null) return;
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

        public static async Task<OfferWithTotalDto> GetOfferWithTotalDto(string tradeType, decimal desiredAmount)
        {
            var orderBooksList = await LoadOrderBooksAsync();
            var finalOffer = new OfferWithTotalDto();

            if (tradeType == "Buy")
            {
                finalOffer.MostProfitableCombination = FindBestBuyOffer(orderBooksList, desiredAmount)
                    .Select(offer => new SimplifiedOffer
                    {
                        BestOffer = offer.BestOffer,
                        TotalEurRequired = offer.TotalEURRequired,
                        RemainingBtcToBuy = offer.RemainingBalance,
                        ExchangerId = offer.Exchange.Id,
                    })
                    .ToList();
                foreach (var offer in finalOffer.MostProfitableCombination)
                {
                    finalOffer.TotalEur += offer.TotalEurRequired;
                }
            }
            else
            {
                finalOffer.MostProfitableCombination = FindBestSellOffer(orderBooksList, desiredAmount)
                    .Select(offer => new SimplifiedOffer
                    {
                        BestOffer = offer.BestOffer,
                        TotalEurGained = offer.TotalEURGained,
                        RemainingBtcToSell = offer.RemainingBalance,
                        ExchangerId = offer.Exchange.Id,
                    })
                    .ToList();
                foreach (var offer in finalOffer.MostProfitableCombination)
                {
                    finalOffer.TotalEur += offer.TotalEurGained;
                }
            }
            return finalOffer;
        }
        public enum TradeType
        {
            Sell,
            Buy
        }
    }
}
