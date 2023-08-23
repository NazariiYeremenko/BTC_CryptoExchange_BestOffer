using System.ComponentModel.DataAnnotations;

namespace CryptoExchangeWebApi.Models;

public class OfferRequest
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "DesiredAmount must be a positive number.")]
    public decimal DesiredAmount { get; set; }

    [Required]
    public string TradeType { get; set; }
}