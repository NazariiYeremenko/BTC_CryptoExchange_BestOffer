using System;
using System.Reflection;
using System.Threading.Tasks;
using CryptoExchangeApp.Models;
using CryptoExchangeApp.Processors;
using CryptoExchangeWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static CryptoExchangeApp.Processors.OrderBookProcessor;
using TradeType = CryptoExchangeWebApi.Models.TradeType;


namespace CryptoExchangeWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExchangeController : ControllerBase
    {
        [HttpGet("findBestOffer")]
        [SwaggerOperation(Description = "Finds the most profitable combination of orders based on the specified trade type and desired amount of BTC. TradeType: 0 - Buy, 1 - Sell.")]
        [SwaggerResponse(StatusCodes.Status200OK, "The most profitable combination", typeof(OfferWithTotalDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Bad request")]
        [Produces("application/json")] 
        public async Task<IActionResult> FindBestOffer([FromQuery] OfferRequest offerRequest)
        {
            try
            {
                var tradeType = Enum.GetName(typeof(TradeType), offerRequest.TradeType);
                var finalOffer = await GetOfferWithTotalDto(tradeType ?? string.Empty, offerRequest.DesiredAmount);
                return Ok(finalOffer);
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }

        
    }
}