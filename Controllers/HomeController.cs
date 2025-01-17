using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Martiscoin.Explorer.Models;
using Martiscoin.Explorer.Services;
using Martiscoin.Explorer.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QRCoder.Core;

namespace Martiscoin.Explorer.Controllers
{
   [ApiExplorerSettings(IgnoreApi = true)]
   public class HomeController : Controller
   {
      private readonly IMemoryCache memoryCache;
      private readonly TickerService tickerService;
      private readonly WeightService weightService;
      private readonly CurrencyService currencyService;
      private readonly ExplorerSettings settings;
      private readonly ChainSettings chainSettings;
      private readonly ILogger<HomeController> log;

      public HomeController(IMemoryCache memoryCache,
          ILogger<HomeController> log,
          TickerService tickerService,
          WeightService weightService,
          CurrencyService currencyService,
          IOptions<ExplorerSettings> settings,
          IOptions<ChainSettings> chainSettings)
      {
         this.memoryCache = memoryCache;
         this.log = log;
         this.settings = settings.Value;
         this.chainSettings = chainSettings.Value;
         this.tickerService = tickerService;
         this.weightService = weightService;
         this.currencyService = currencyService;
      }

      public IActionResult Index()
      {
         //if (!settings.Features.Home)
         //{
         //   return Redirect("/block-explorer");
         //}

         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;
         ViewBag.Ticker = settings.Ticker;
         
         ViewBag.Url = Request.Host.ToString();

         if (settings.Features.POSWeight)
         {
            string networkWeight = weightService.GetNetworkWeight();
            ViewBag.NetworkWeight = networkWeight;
         }

         if (settings.Features.Ticker)
         {
            IRequestCultureFeature rqf = Request.HttpContext.Features.Get<IRequestCultureFeature>();
            RegionInfo regionInfo = currencyService.GetRegionaInfo(rqf);
            Ticker ticker = null;

            try
            {
               ticker = tickerService.GetTicker(regionInfo.ISOCurrencySymbol);
               if (ticker != null && ticker.PriceBtc != null)
               {
                  ViewBag.PriceBtc = decimal.Parse(ticker.PriceBtc.ToString()).ToString("0.0000");
               }
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Failed to get ticker information.");
               ticker = new Ticker();
            }

            return View(ticker);
         }
         else
         {
            return View();
         }
      }

      [HttpGet]
      [Route("about")]
      public IActionResult About()
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         return View();
      }

      [Route("qr/{value}")]
      public IActionResult Qr(string value)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;

         var memoryStream = new MemoryStream();

         QRCodeGenerator qrGenerator = new QRCodeGenerator();
         QRCodeData qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.L);
         var qrCode = new QRCode(qrCodeData);
         qrCode.GetGraphic(20, Color.Black, Color.White, false).Save(memoryStream, ImageFormat.Png);
         return File(memoryStream.ToArray(), "image/png");
      }
   }
}
