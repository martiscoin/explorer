namespace Martiscoin.Explorer.Models.ApiModels
{
   public class TransactionInputModel
   {
      public int InputIndex { get; set; }

      public string InputAddress { get; set; }

      public string CoinBase { get; set; }

      public string InputTransactionId { get; set; }

      public string ScriptSig { get; set; }

      public string ScriptSigAsm { get; set; }

      public string WitScript { get; set; }

      public string SequenceLock { get; set; }
   }
}
