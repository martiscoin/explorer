using System.Collections.Generic;
using XOuranos.Explorer.Models.ApiModels;
using XOuranos.Explorer.Settings;
using Microsoft.Extensions.Options;

namespace XOuranos.Explorer.Services
{
   public class BlockIndexService : ServiceBase
   {
      private readonly ExplorerSettings settings;

      public BlockIndexService() : base(string.Empty)
      {

      }

      public BlockIndexService(IOptions<ExplorerSettings> settings) : base(settings.Value.Indexer?.ApiUrl)
      {
         this.settings = settings.Value;
      }

      public BlockModel GetBlockByHeight(long blockHeight)
      {
         return Execute<BlockModel>(GetRequest($"/query/block/index/{blockHeight}"));
      }

      public List<string> GetBlockTransactions(long blockHeight)
      {
         dynamic obj = Execute<dynamic>(GetRequest($"/query/block/index/{blockHeight}/transactions"));
         var list=new List<string>();
         foreach(Dictionary<string,object> item in obj)
         {
            list.Add(item.GetValueOrDefault("transactionHash").ToString());
         }
         return list;
      }

      public BlockModel GetBlockByHash(string blockHash)
      {
         return Execute<BlockModel>(GetRequest($"/query/block/{blockHash}/transactions"));
      }

      public BlockModel GetLatestBlock()
      {
         return Execute<BlockModel>(GetRequest("/query/block/Latest"));
      }

      public AddressModel GetTransactionsByAddress(string adddress)
      {
         return Execute<AddressModel>(GetRequest($"/query/address/{adddress}/transactions"));
      }

      public TransactionDetailsModel GetTransaction(string transactionId)
      {
         return Execute<TransactionDetailsModel>(GetRequest($"/query/transaction/{transactionId}"));
      }

      public List<dynamic> GetPeers()
      {
         return Execute<dynamic>(GetRequest($"/stats/peers"));
      }

      public dynamic GetSupply()
      {
         return Execute<dynamic>(GetRequest($"/insight/supply"));
      }

      public List<dynamic> GetRichList()
      {
         return Execute<dynamic>(GetRequest($"/insight/richlist?offset=0&limit=100"));
      }

      public List<dynamic> GetLast50Blocks()
      {
         return Execute<dynamic>(GetRequest($"/query/block?limit=30"));
      }

      public string GetStatistics()
      {
         string result = Execute(GetRequest("/stats"));
         return result;
      }
   }
}