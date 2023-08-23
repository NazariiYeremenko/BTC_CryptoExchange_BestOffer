using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchangeApp.Models
{
    public class SimplifiedOffer
    {
        public OrderContainer BestOffer { get; set; }
        public decimal TotalEurRequired { get; set; }
        public decimal TotalEurGained { get; set; }
        public decimal RemainingBtcToBuy { get; set; }
        public decimal RemainingBtcToSell { get; set; }
        public string ExchangerId { get; set; }
    }
}

