namespace Martiscoin.Explorer.Models.ApiModels
{
   public class Status
   {
      public string CoinTag { get; set; }

      public string Progress { get; set; }

      public long TransactionsInPool { get; set; }

      public long SyncBlockIndex { get; set; }

      public long BlocksPerMinute { get; set; }

      public decimal AvgBlockPersistInSeconds { get; set; }

      public decimal avgBlockSizeKb { get; set; }

      public string Error { get; set; }

      public BlockChainInfo blockchain { get; set; }

      public NetworkInfo NetworkInfo { get; set; }
   }
}
