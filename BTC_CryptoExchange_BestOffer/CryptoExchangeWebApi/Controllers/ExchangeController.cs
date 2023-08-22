using System;
using System.Reflection;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using Microsoft.AspNetCore.Mvc;


namespace CryptoExchangeWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : ControllerBase
    {
        private readonly OrderBookProcessor _orderBookProcessor;

        public ExchangeController(OrderBookProcessor orderBookProcessor, IWebHostEnvironment env)
        {
            _orderBookProcessor = orderBookProcessor;
        }

        [HttpPost("findBestOffer")]
        public async Task<IActionResult> FindBestOffer([FromBody] OfferRequest offerRequest)
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

                List<Offer> mostProfitableCombination;
                if (tradeType == OrderBookProcessor.TradeType.Buy)
                {
                    mostProfitableCombination = OrderBookProcessor.FindBestBuyOffer(orderBooksList, offerRequest.DesiredAmount);
                }
                else
                {
                    mostProfitableCombination = OrderBookProcessor.FindBestSellOffer(orderBooksList, offerRequest.DesiredAmount);
                }

                return Ok(mostProfitableCombination);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class OfferRequest
    {
        public string TradeType { get; set; }
        public decimal DesiredAmount { get; set; }
    }
}