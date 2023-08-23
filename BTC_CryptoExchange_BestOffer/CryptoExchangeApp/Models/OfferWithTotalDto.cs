using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchangeApp.Models
{
    public class OfferWithTotalDto
    {
        public List<SimplifiedOffer> MostProfitableCombination { get; set; }
        public decimal TotalEur { get; set; }


    }
}
