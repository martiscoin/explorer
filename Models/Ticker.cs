

using System;

namespace XOuranos.Explorer.Models
{
   public class Ticker
   {
      public string Symbol { get; set; }

      public decimal Price { get; set; }

      public decimal PriceBtc { get; set; }

      public double Last24Change { get; set; }
   }
}
