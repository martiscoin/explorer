using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using XOuranos.Explorer.Models;
using XOuranos.Explorer.Models.ApiModels;
using XOuranos.Explorer.Services;
using XOuranos.Explorer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace XOuranos.Explorer.Controllers
{
   [ApiExplorerSettings(IgnoreApi = true)]
   [Route("block-explorer")]
   public class BlockExplorerController : Controller
   {
      private readonly ExplorerSettings settings;
      private readonly ChainSettings chainSettings;
      private readonly IMemoryCache memoryCache;
      private readonly Status stats;
      private readonly BlockIndexService indexService;

      public BlockExplorerController(IMemoryCache memoryCache,
          BlockIndexService indexService,
          IOptions<ExplorerSettings> settings,
          IOptions<ChainSettings> chainSettings)
      {
         this.memoryCache = memoryCache;

         if (this.memoryCache.Get("BlockchainStats") != null)
         {
            stats = JsonConvert.DeserializeObject<Status>(this.memoryCache.Get("BlockchainStats").ToString());
         }
         else
         {
            stats = new Status { Error = "BlockchainStats not available yet." };
         }

         this.indexService = indexService;
         this.settings = settings.Value;
         this.chainSettings = chainSettings.Value;
      }

      [HttpGet]
      public IActionResult Index()
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         try
         {
            BlockModel latestBlock = indexService.GetLatestBlock();

            ViewBag.LatestBlock = latestBlock;
            ViewBag.BlockchainHeight = latestBlock.BlockIndex;

            var latestBlocks = new List<dynamic>();

            for (int i = 0; i <= 10; i++)
            {
               latestBlocks.Add(indexService.GetBlockByHeight(latestBlock.BlockIndex - i));
            }

            ViewBag.Blocks = latestBlocks;

            return View();
         }
         catch (Exception ex)
         {
            ViewBag.Error = ex;
            return View();
         }
      }

      [HttpGet]
      [Route("netstatsblocktime")]
      public dynamic GetNetworkStatsBlockTime()
      {
         List<dynamic> blocks = indexService.GetLast50Blocks();
         dynamic result = null;
         List<string> blockindex = new List<string>();
         List<double> blocktime = new List<double>();
         double avgBlocktime = 0;
         Int64 firstime = 0;
         foreach (Dictionary<string, object> block in blocks)
         {
            if (firstime <= 0)
            {
               firstime = (Int64)block.GetValueOrDefault("blockTime");
            }
            else
            {
               blockindex.Add(block.GetValueOrDefault("blockIndex").ToString());
               Int64 current = (Int64)block.GetValueOrDefault("blockTime");
               double dt = ((current - firstime));
               blocktime.Add(dt);
               avgBlocktime += dt;
               firstime = current;
            }
         }
         avgBlocktime = avgBlocktime / blocktime.Count;

         result = new
         {
            x = blockindex,
            y = blocktime,
            avg= avgBlocktime.ToString("0.00")
         };
         return result;
      }

      [HttpGet]
      [Route("netstatshashrate")]
      public dynamic GetNetworkStatsHashrate()
      {
         List<dynamic> blocks = indexService.GetLast50Blocks();
         dynamic result = null;
         List<string> blockindex = new List<string>();
         List<double> difficulty = new List<double>();
         double avgHashrate = 0;
         foreach (Dictionary<string, object> block in blocks)
         {
            blockindex.Add(block.GetValueOrDefault("blockIndex").ToString());
            difficulty.Add((double)block.GetValueOrDefault("difficulty") * 1000);
            avgHashrate += ((double)block.GetValueOrDefault("difficulty")) * 1000;
         }
         avgHashrate = avgHashrate / difficulty.Count;
         result = new
         {
            x = blockindex,
            y = difficulty,
            avg = avgHashrate.ToString("0.00")
         };
         return result;
      }

      [HttpGet]
      [Route("netstatsdifficulty")]
      public dynamic GetNetworkStatsDifficulty()
      {
         List<dynamic> blocks = indexService.GetLast50Blocks();
         dynamic result = null;
         List<string> blockindex = new List<string>();
         List<double> difficulty = new List<double>();
         double avgDifficulty = 0;
         foreach (Dictionary<string, object> block in blocks)
         {
            blockindex.Add(block.GetValueOrDefault("blockIndex").ToString());
            difficulty.Add((double)block.GetValueOrDefault("difficulty"));
            avgDifficulty += (double)block.GetValueOrDefault("difficulty");
         }
         avgDifficulty= avgDifficulty / difficulty.Count;
         result = new
         {
            x = blockindex,
            y = difficulty,
            avg=avgDifficulty.ToString("0.00")
         };
         return result;
      }

      [HttpGet]
      [Route("stats")]
      public IActionResult Stats()
      {
         ViewBag.BlockchainHeight = stats.SyncBlockIndex;
         ViewBag.BlockchainHeight = stats.SyncBlockIndex;
         ViewBag.difficulty = stats.blockchain.Difficulty;
         ViewBag.avgBlockPersistInSeconds = stats.AvgBlockPersistInSeconds;
         ViewBag.avgBlockSizeKb = stats.avgBlockSizeKb;
         ViewBag.height = stats.blockchain.Headers;
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;
         Dictionary<string,object> supply = indexService.GetSupply();
         ViewBag.circulating = supply.GetValueOrDefault("circulating").ToString();
         ViewBag.Mined = ((decimal.Parse(ViewBag.circulating) / 100_000_000) * 100).ToString("0.00");
         return View();
      }

      [HttpGet]
      [Route("network")]
      public IActionResult Network()
      {
         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;
         dynamic Peers = indexService.GetPeers();
         ViewBag.Peers = Peers;
         return View();
      }

      [HttpGet]
      [Route("top")]
      public IActionResult Top()
      {
         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;
         dynamic richlist = indexService.GetRichList();
         var list = new List<dynamic>();
         double total = 0;
         foreach (Dictionary<string,object> rich in richlist)
         {
            double b = ulong.Parse(rich.GetValueOrDefault("balance").ToString()) / 100000000.0;
            total += b;
            list.Add(new
            {
               address = rich.GetValueOrDefault("address"),
               balance = b ,
               per = 0,
            });
         }

         var newlist = new List<dynamic>();

         foreach (dynamic item in list)
         {
            newlist.Add(new
            {
               address = item.address,
               balance = item.balance,
               per = ((item.balance / total) * 100.0).ToString("0.0000"),
            });
         }

         ViewBag.RichList = newlist;
         return View();
      }

      [HttpGet, HttpPost]
      [Route("search")]
      public IActionResult Search(SearchBlockExplorer searchBlockExplorer)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         if (searchBlockExplorer.Query == null)
         {
            return RedirectToAction("Index");
         }
         else if (searchBlockExplorer.Query.Length == 34)
         {
            return RedirectToAction("Address", new { address = searchBlockExplorer.Query });
         }
         else if (searchBlockExplorer.Query.Length == 64)
         {
            return RedirectToAction("Transaction", new { transactionId = searchBlockExplorer.Query });
         }
         else if (searchBlockExplorer.Query.Length <= 8)
         {
            return RedirectToAction("Block", new { block = searchBlockExplorer.Query });
         }

         return RedirectToAction("Index");
      }

      [HttpGet]
      [Route("block/{block}")]
      public IActionResult Block(string block)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         BlockModel result = (block.ToLower() == "latest") ? indexService.GetLatestBlock() : indexService.GetBlockByHeight(int.Parse(block));
         if (result != null)
         {
            result.Transactions = indexService.GetBlockTransactions(int.Parse(block));
            result.TransactionCount = result.Transactions.Count;
         }
         
         return View(result);
      }

      [HttpGet]
      [Route("block/hash/{hash}")]
      public IActionResult BlockHash(string hash)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;
         BlockModel block = indexService.GetBlockByHash(hash);
         return View("Block", block);
      }

      [HttpGet]
      [Route("address/{address}")]
      public IActionResult Address(string address)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         return View(indexService.GetTransactionsByAddress(address));
      }

      [HttpGet]
      [Route("transaction/{transactionId}")]
      public IActionResult Transaction(string transactionId)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         TransactionDetailsModel trx = indexService.GetTransaction(transactionId);

         if (trx != null && trx.TransactionId == null)
         {
            return RedirectToAction("BlockHash", new { hash = transactionId });
         }

         if (trx.IsCoinbase)
         {
            ViewBag.TransactionType = "Coinbase";
         }
         else if (trx.IsCoinstake && trx.Outputs.Exists(o => o.OutputType == "TX_PUBKEY"))
         {
            ViewBag.TransactionType = "Stake Reward";
         }
         // Make a prediction if this is a cold stake activation transaction or a cold stake reward.
         else if (trx.IsCoinstake && trx.Outputs.Exists(o => o.OutputType == "TX_COLDSTAKE"))
         {
            ViewBag.TransactionType = "Cold Stake Reward";
         }
         else if (!trx.IsCoinstake && trx.Outputs.Exists(o => o.OutputType == "TX_COLDSTAKE"))
         {
            ViewBag.TransactionType = "Cold Stake Activation";
         }
         // This prediction can be greatly improved when we can parse the script of input, which should clearly show us this is cold stake rewards.
         else if (!trx.IsCoinstake && !trx.IsCoinbase && trx.Outputs.Count == 1 && trx.Inputs.GroupBy(z => z.InputTransactionId).Any(z => z.Count() > 1))
         {
            ViewBag.transactionType = "Cold Stake Withdraw";
         }
         else
         {
            ViewBag.TransactionType = "Normal";
         }

         return View(trx);
      }
   }
}
