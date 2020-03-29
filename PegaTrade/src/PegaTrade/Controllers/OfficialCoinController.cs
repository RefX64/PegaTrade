using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Layer.Models.Community;
using PegaTrade.ViewModel.Coins;
using PegaTrade.ViewModel.Community;

namespace PegaTrade.Controllers
{
    public class OfficialCoinController : BaseController
    {
        public OfficialCoinController(IMemoryCache cache, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _memoryCache = cache;
            _hostingEnvironment = env;
        }

        [HttpGet("Coins")]
        public async Task<IActionResult> ShowAllMarketCoins()
        {
            List<MarketCoin> marketCoins = (await GetAllCoinsMarketDetailsAPI()).Take(100).ToList();
            return View("ViewAllCoins", marketCoins);
        }

        [HttpGet("Coin/{identifier}", Name = "ViewOfficialCoin_Route")]
        public IActionResult Index(string identifier)
        {
            return View(new OfficialCoinVM
            {
                Identifier = identifier
            });
        }

        public async Task<PartialViewResult> GetOfficialCoinView(string identifier)
        {
            OfficialCoinVM vm = new OfficialCoinVM
            {
                OfficialCoin = await GetOfficialCoin(identifier),
                User = CurrentUser
            };

            if (vm.OfficialCoin == null) { return GeneratePartialViewError($"The coin '{identifier}' cannot be found."); }


            vm.ConversationsVM = new ConversationsVM
            {
                Threads = await GetThreadsByCategory(50, Types.ConvThreadCategory.OfficialCoins, officialCoinId: vm.OfficialCoin.OfficialCoinId),
                CurrentUser = CurrentUser,
                CurrentThread = new BBThread
                {
                    ThreadName = "officialCoin",
                    CategoryCode = Types.ConvThreadCategory.OfficialCoins,
                    OfficialCoinId = vm.OfficialCoin.OfficialCoinId
                }
            };

            return PartialView("_OfficialCoin", vm);
        }
    }
}