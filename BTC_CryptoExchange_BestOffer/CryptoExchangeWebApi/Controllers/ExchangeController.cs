using System;
using System.Reflection;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using CryptoExchangeWebApi.Models;
using Microsoft.AspNetCore.Mvc;


namespace CryptoExchangeWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : ControllerBase
    {

        [HttpGet("findBestOffer")]
        public async Task<IActionResult> FindBestOffer([FromQuery] OfferRequest offerRequest)
        {
            try
            {
                var solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
                var filePath = Path.Combine(solutionDirectory ?? string.Empty, "order_books_data.json");

                if (offerRequest.TradeType != "buy" && offerRequest.TradeType != "sell")
                {
                    return BadRequest("Invalid trade type. Please specify 'buy' or 'sell'.");
                }

                var tradeType = offerRequest.TradeType == "buy" ? OrderBookProcessor.TradeType.Buy : OrderBookProcessor.TradeType.Sell;

                var orderBooksList = await OrderBookProcessor.LoadOrderBooksAsync(filePath);

                var finalOffer = new OfferWithTotalDto();
                if (tradeType == OrderBookProcessor.TradeType.Buy)
                {
                    finalOffer.MostProfitableCombination = OrderBookProcessor.FindBestBuyOffer(orderBooksList, offerRequest.DesiredAmount)
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
                    finalOffer.MostProfitableCombination = OrderBookProcessor.FindBestSellOffer(orderBooksList, offerRequest.DesiredAmount)
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

                return Ok(finalOffer);
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }

    }
}