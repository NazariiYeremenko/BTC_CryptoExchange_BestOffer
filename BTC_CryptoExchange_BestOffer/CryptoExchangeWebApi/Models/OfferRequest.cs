using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CryptoExchangeApp.Processors;
using Swashbuckle.AspNetCore.Annotations;

namespace CryptoExchangeWebApi.Models
{
    public class OfferRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "DesiredAmount must be a positive number.")]
        public decimal DesiredAmount { get; set; }

        [Required]
        [DefaultValue(TradeType.Buy)] // Default value for TradeType
        [EnumDataType(typeof(TradeType), ErrorMessage = "TradeType must be 'buy' or 'sell'.")]
        public TradeType TradeType { get; set; }
    }

    public enum TradeType
    {
        Buy,
        Sell
    }
}