using CryptoExchangeApp.Models;
using static CryptoExchangeApp.Processors.OrderBookProcessor;

namespace CryptoExchangeConsoleApp.Processors
{
    public class ConsolePrinter
    {
        public static void PrintOffers(List<Offer> offers, decimal desiredAmount, TradeType tradeType)
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
    }
}
