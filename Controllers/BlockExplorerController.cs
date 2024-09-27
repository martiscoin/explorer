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
using Blockcore.NBitcoin.BouncyCastle.math;
using Blockcore.NBitcoin;

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

                //for (int i = 0; i <= 10; i++)
                //{
                //    latestBlocks.Add(indexService.GetBlockByHeight(latestBlock.BlockIndex - i));
                //}
                int page = 1;
                if (!string.IsNullOrEmpty(Request.Query["page"]))
                {
                    int.TryParse(Request.Query["page"], out page);
                }
                page = page <= 0 ? 1 : page;
                page = page - 1;
                var offset = page * 10 > latestBlock.BlockIndex ? latestBlock.BlockIndex - 10 : page * 10;
                latestBlocks.Clear();
                var finds = indexService.GetLastBlocks(10, offset, 1);
                foreach (Dictionary<string, object> block in finds)
                {
                    latestBlocks.Add(new
                    {
                        BlockIndex = block.GetValueOrDefault("height"),
                        BlockTime = block.GetValueOrDefault("time"),
                        TransactionCount = block.GetValueOrDefault("txCount"),
                        BlockSize = block.GetValueOrDefault("size")
                    });
                }
                ViewBag.PageIndex = page + 1;
                ViewBag.PageTotal = latestBlock.BlockIndex % 10 == 0 ? latestBlock.BlockIndex / 10 : latestBlock.BlockIndex / 10 + 1;
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
                    var diff = ((double)block.GetValueOrDefault("difficulty"));
                    if (diff > 3000)
                    {
                        firstime = current;
                        continue;
                    }
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
                avg = avgBlocktime.ToString("0.00")
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
            List<double> hashrates = new List<double>();
            double avgHashrate = 0;
            foreach (Dictionary<string, object> block in blocks)
            {
                blockindex.Add(block.GetValueOrDefault("blockIndex").ToString());
                var diff = ((double)block.GetValueOrDefault("difficulty"));
                if (diff > 3000) continue;
                var currentDifficulty = BigInteger.ValueOf((long)diff);
                var hashrate = currentDifficulty.Multiply(BigInteger.ValueOf(2).Pow(256))
                                             .Divide(new Target(new byte[] { 0x1d, 0x00, 0xff, 0xff }).ToBigInteger()).Divide(BigInteger.ValueOf(10 * 60))
                                             .LongValue / 1_000_000.0;
                avgHashrate += hashrate;
                hashrates.Add(hashrate);
            }
            avgHashrate = avgHashrate / hashrates.Count;
            result = new
            {
                x = blockindex,
                y = hashrates,
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
                var diff = ((double)block.GetValueOrDefault("difficulty"));
                if (diff > 3000) continue;
                blockindex.Add(block.GetValueOrDefault("blockIndex").ToString());
                difficulty.Add((double)block.GetValueOrDefault("difficulty"));
                avgDifficulty += (double)block.GetValueOrDefault("difficulty");
            }
            avgDifficulty = avgDifficulty / difficulty.Count;
            result = new
            {
                x = blockindex,
                y = difficulty,
                avg = avgDifficulty.ToString("0.00")
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
            Dictionary<string, object> supply = indexService.GetSupply();
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
            double to25 = 0;
            double to50 = 0;
            double to75 = 0;
            double to100 = 0;
            int i = 0;
            foreach (Dictionary<string, object> rich in richlist)
            {
                double b = ulong.Parse(rich.GetValueOrDefault("balance").ToString()) / 100000000.0;
                total += b;
                list.Add(new
                {
                    address = rich.GetValueOrDefault("address"),
                    balance = b,
                    per = 0,
                });
                if (i < 25)
                {
                    to25 += b;
                }
                else if (i < 50)
                {
                    to50 += b;
                }
                else if (i < 75)
                {
                    to75 += b;
                }
                else
                {
                    to100 += b;
                }
                i++;
            }

            Dictionary<string, object> supply = indexService.GetSupply();
            double totlsupply = (double)supply.GetValueOrDefault("circulating");//×Ü²ú³ö


            var newlist = new List<dynamic>();

            foreach (dynamic item in list)
            {
                newlist.Add(new
                {
                    address = item.address,
                    balance = item.balance,
                    per = ((item.balance / totlsupply) * 100.0).ToString("0.0000"),
                });
            }

            ViewBag.t1 = new { 
                top= to25.ToString("0.0000"),
                per= ((to25 / totlsupply) * 100.0).ToString("0.00")
            };
            ViewBag.t2 = new
            {
                top = to50.ToString("0.0000"),
                per = ((to50 / totlsupply) * 100.0).ToString("0.00")
            };
            ViewBag.t3 = new
            {
                top = to75.ToString("0.0000"),
                per = ((to75 / totlsupply) * 100.0).ToString("0.00")
            };
            ViewBag.t4 = new
            {
                top = to100.ToString("0.0000"),
                per = ((to100 / totlsupply) * 100.0).ToString("0.00")
            };
            ViewBag.t5 = new
            {
                top= total.ToString("0.0000"),
                per = ((total / totlsupply) * 100.0).ToString("0.00")
            };
            ViewBag.t6 = new
            {
                top = Math.Abs(totlsupply - total).ToString("0.0000"),
                per = (100.0 - double.Parse(ViewBag.t5.per)).ToString("0.00")
            };
            ViewBag.t7 = new
            {
                top = totlsupply.ToString("0.0000"),
                per = 100.0.ToString("0.00")
            };

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

            int page = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                int.TryParse(Request.Query["page"], out page);
            }
            page = page <= 0 ? 1 : page;
            page = page - 1;
            var offset = page * 10;

            ViewBag.BlockchainHeight = stats.SyncBlockIndex;
            AddressModel model = indexService.GetTransactionsByAddress(address);
            var finds = indexService.GetTransactionsListByAddress(address, offset);
            foreach (Dictionary<string, object> f in finds)
            {
                var hash = (string)f.GetValueOrDefault("transactionHash");
                var hashset = indexService.GetTransaction(hash);
                var time = (int)(hashset.Timestamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                model.Transactions.Add(new TransactionModel()
                {
                    BlockIndex = (long)f.GetValueOrDefault("blockIndex"),
                    TransactionHash = hash,
                    Value = (long)f.GetValueOrDefault("value"),
                    Time = time
                });
            }
            ViewBag.PageIndex = page + 1;
            return View(model);
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
