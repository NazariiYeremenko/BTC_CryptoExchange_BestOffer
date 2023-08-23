using System;
using System.Reflection;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using CryptoExchangeWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace CryptoExchangeWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : ControllerBase
    {

        [HttpGet("findBestOffer")]
        [SwaggerOperation(
            Description = "Finds the most profitable combination of orders based on the specified trade type and desired amount of BTC. TradeType: 0 - Buy, 1 - Sell."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "The most profitable combination", typeof(OfferWithTotalDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad request")]
        [Produces("application/json")] 
        public async Task<IActionResult> FindBestOffer([FromQuery] OfferRequest offerRequest)
        {
            try
            {
                var solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
                var filePath = Path.Combine(solutionDirectory ?? string.Empty, "order_books_data.json");

/*                if (offerRequest.TradeType != "buy" && offerRequest.TradeType != "sell")
                {
                    return BadRequest("Invalid trade type. Please specify 'buy' or 'sell'.");
                }

                var tradeType = offerRequest.TradeType == "buy" ? OrderBookProcessor.TradeType.Buy : OrderBookProcessor.TradeType.Sell;*/

                var orderBooksList = await OrderBookProcessor.LoadOrderBooksAsync(filePath);

                var finalOffer = new OfferWithTotalDto();
                if (offerRequest.TradeType == TradeType.Buy)
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