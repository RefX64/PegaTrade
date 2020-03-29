using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Helpers;
using PegaTrade.Layer.Language;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.ViewModel;
using PegaTrade.ViewModel.Coins;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace PegaTrade.Controllers
{
    [TypeFilter(typeof(AuthorizedUser))]
    public class CryptoController : BaseController
    {
        //public CryptoController() { }
        public CryptoController(IMemoryCache cache, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _memoryCache = cache;
            _hostingEnvironment = env;
        }

        #region Publicly Available Methods (Don't need to login)
        [HttpGet("ViewPortfolio/{username}/{portfolioName}", Name = "ViewPublicPortfolio_Route")]
        public IActionResult ViewPortfolio(string username, string portfolioName)
        {
            return View("ViewPortfolio", new ViewUser
            {
                Username = username,
                PortfolioName = portfolioName
            });
        }
        #endregion

        #region Get/Fetch Data

        [Route("Dashboard")]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<PartialViewResult> FullDashboard()
        {
            FullDashboardVM vm = new FullDashboardVM
            {
                User = CurrentUser,
                ConversationsVM = new ViewModel.Community.ConversationsVM
                {
                    CurrentThread = new Layer.Models.Community.BBThread
                    {
                        ThreadName = "mainDashboard",
                        CategoryCode = Types.ConvThreadCategory.MainDashboard,
                    },
                    Threads =  await GetThreadsByCategory(50, Types.ConvThreadCategory.MainDashboard),
                    CurrentUser = CurrentUser
                }
            };

            return PartialView("_FullDashboard", vm);
        }

        public async Task<PartialViewResult> LoadPortfolioViewMode(PortfolioRequest request, string username, string portfolioName, bool coinsOnly = false)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(portfolioName)) { return null; }

            request.ViewUser = new ViewUser
            {
                Username = username,
                PortfolioName = portfolioName,
            };
            request.ViewUser.PortfolioName = Utilities.FormatPortfolioName(request.ViewUser.PortfolioName);
            request.ViewOtherUser = true;

            ResultsPair<ViewUser> viewUserResult = AuthorizationLogic.AuthorizeViewUser(request.ViewUser.Username, request.ViewUser.PortfolioName);
            if (!viewUserResult.Result.IsSuccess) { return GeneratePartialViewError(viewUserResult.Result.Message); }
            
            request.ViewUser = viewUserResult.Value;

            if (coinsOnly)
            {
                request.PortfolioID = request.ViewUser.SelectedPortfolioID;
                CoinsVM coinsVM = await GenerateCoinsVM(request);
                return PartialView("_FullCoins", coinsVM);
            }

            PortfolioVM vm = await GeneratePortfolioVM(request);
            return PartialView("_Portfolio", vm);
        }

        public async Task<PartialViewResult> Portfolio()
        {
            PortfolioVM vm = await GeneratePortfolioVM(new PortfolioRequest { Currency = Types.CoinCurrency.USD });
            return PartialView("_Portfolio", vm);
        }

        public async Task<PartialViewResult> GetAllCoinsFromPortfolio(PortfolioRequest request)
        {
            if (!CurrentUser.HasPortfolio(request.PortfolioID)) { return GeneratePartialViewError(Lang.PortfolioNotFound); }

            CoinsVM vm = await GenerateCoinsVM(request);
            return PartialView("_FullCoins", vm);
        }

        private async Task<PortfolioVM> GeneratePortfolioVM(PortfolioRequest request)
        {
            PortfolioVM vm = new PortfolioVM
            {
                ViewOtherUser = request.ViewOtherUser,
                ViewUser = request.ViewUser,
                Portfolios = request.ViewOtherUser ? request.ViewUser?.Portfolios : CurrentUser.Portfolios
            };

            if (request.ViewOtherUser && request.ViewUser != null)
            {
                vm.SelectedPortfolio = request.PortfolioID = request.ViewUser.SelectedPortfolioID;
                vm.CoinsVM = await GenerateCoinsVM(request);
            }
            else if (CurrentUser.IsValidUser() || CurrentUser.IsDemoUser())
            {
                request.PortfolioID = vm.Portfolios.FirstOrDefault(x => x.IsDefault)?.PortfolioId ?? vm.Portfolios.First().PortfolioId;
                vm.CoinsVM = await GenerateCoinsVM(request);
            }
            
            return vm;
        }

        private async Task<CoinsVM> GenerateCoinsVM(PortfolioRequest request)
        {
            return new CoinsVM((await GetCoinsBasedOnPortfolioId(request)).ToCoinDisplay(request.Currency))
            {
                DisplayCurrency = request.Currency,
                ViewOtherUser = request.ViewOtherUser
            };
        }

        private async Task<List<CryptoCoin>> GetCoinsBasedOnPortfolioId(PortfolioRequest request)
        {
            // Only let ViewPortfolio viewers allow getting that user's coins. Portfolio(public) would have been validated by this point.
            if (!request.ViewOtherUser && !CurrentUser.HasPortfolio(request.PortfolioID)) { return new List<CryptoCoin>(); }

            List<CryptoCoin> coins = await CryptoLogic.GetAllUserCoinsByPortfolioIdAsync(request.PortfolioID);
            return await UpdateCoinsCurrentPrice(coins, request.UseCombinedDisplay, request.Currency);
        }

        #endregion

        #region Portfolio Management (CRUD)

        public async Task<JsonResult> GetAllPortfolioSelectList(bool updateUser = false)
        {
            List<Portfolio> portfolioList = await CryptoLogic.GetAllUserPortfolioAsync(CurrentUser);
            if (updateUser)
            {
                CurrentUser.Portfolios = await CryptoLogic.GetAllUserPortfolioAsync(CurrentUser);
                SubmitCurrentUserUpdate();
            }

            return Json(portfolioList.ToSelectList(x => x.Name, x => x.PortfolioId.ToString()));
        }

        public async Task<PartialViewResult> GetPortfolioList(bool isUpdated = false)
        {
            if (isUpdated)
            {
                CurrentUser.Portfolios = await CryptoLogic.GetAllUserPortfolioAsync(CurrentUser);
                SubmitCurrentUserUpdate();
            }

            PortfolioVM vm = new PortfolioVM
            {
                Portfolios = CurrentUser.Portfolios
            };

            return PartialView("Crud/_PortfolioList", vm);
        }

        public PartialViewResult CreateNewPortfolio()
        {
            PortfolioVM vm = new PortfolioVM { IsCreateMode = true };
            return PartialView("Crud/_ManagePortfolio", vm);
        }

        public ActionResult UpdatePortfolio(int portfolioId)
        {
            Portfolio portfolio = CurrentUser.Portfolios.FirstOrDefault(x => x.PortfolioId == portfolioId);
            if (portfolio == null) { return Json(ResultsItem.Error("Unable to find the correct portfolio associated with logged in user.")); }

            PortfolioVM vm = new PortfolioVM { Portfolio = portfolio, IsCreateMode = false };
            return PartialView("Crud/_ManagePortfolio", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ExecutePortfolioChanges(PortfolioVM vm)
        {
            if (!ModelState.IsValid) { return Json(ResultsItem.Error(ModelState.GetAllErrorsString())); }

            int maxPortfolioAllowed = SubscriptionLogic.GetMaxAllowedPortfolioPerUser(CurrentUser.PTUserInfo.SubscriptionLevel);
            if (vm.IsCreateMode && CurrentUser.Portfolios.Count >= maxPortfolioAllowed)
            {
                return Json(ResultsItem.Error($"We're sorry, you can only have {maxPortfolioAllowed} portfolios max for your subscription level: {CurrentUser.PTUserInfo.SubscriptionLevel.ToString()}"));
            }

            var portfolioResultPair = vm.IsCreateMode ? (await CryptoLogic.InsertNewPortfolio(CurrentUser, vm.Portfolio.Name, vm.Portfolio.DisplayType, vm.Portfolio.IsDefault))
                                                      : CryptoLogic.UpdatePortfolio(vm.Portfolio.PortfolioId, vm.Portfolio.Name, vm.Portfolio.DisplayType, vm.Portfolio.IsDefault, CurrentUser);
            
            return Json(portfolioResultPair.Result);
        }

        [HttpPost]
        public async Task<JsonResult> DeletePortfolio(int portfolioId)
        {
            ResultsItem deleteResult = await CryptoLogic.DeletePortfolio(portfolioId, CurrentUser);
            if (deleteResult.IsSuccess)
            {
                CurrentUser.Portfolios = await CryptoLogic.GetAllUserPortfolioAsync(CurrentUser);
                SubmitCurrentUserUpdate();
            }
            return Json(deleteResult);
        }

        #endregion

        #region Coins Management (CRUD)

        /// <summary>
        /// The main "Add" method. It will load: Manual Add, Import API, Import .cvi in tabs
        /// </summary>
        [HttpGet]
        public async Task<PartialViewResult> AddNewCoins(int portfolioId, bool isUpdated = false)
        {
            if (CurrentUser.ExchangeApiList.IsNullOrEmpty() || isUpdated)
            {
                CurrentUser.ExchangeApiList = await CryptoLogic.GetAllUserExchangeAPIAsync(CurrentUser.UserId);
                SubmitCurrentUserUpdate();
            }

            CoinManagementVM vm = new CoinManagementVM
            {
                PortfolioList = CurrentUser.Portfolios.ToSelectList(x => x.Name, x => x.PortfolioId.ToString()),
                IsCreateMode = true,
                SelectedPortfolioID = portfolioId,
                ExistingSavedImportAPIList = CurrentUser.ExchangeApiList.ToSelectList(x => (string.IsNullOrEmpty(x.Name) ? x.Exchange.ToString() : x.Name), x => x.Id.ToString()),
                Coin = new CryptoCoin
                {
                    OrderDate = DateTime.Today
                }
            };

            return PartialView("_AddNewCoins", vm);
        }

        [HttpGet]
        public async Task<PartialViewResult> UpdateCoin(long coinId)
        {
            CryptoCoin coin = await CryptoLogic.GetSingleCoinByUser(coinId, CurrentUser);
            if (coin == null) { return GeneratePartialViewError(Lang.PortfolioNotFound); }

            coin.SoldCoinCurrency = coin.CoinCurrency;
            CoinManagementVM vm = new CoinManagementVM
            {
                Coin = coin,
                IsCreateMode = false,
                SoldDate = DateTime.Today,
                SelectedPortfolioID = coin.PortfolioId
            };

            return PartialView("Crud/_ManageCoin", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ExecuteCoinChanges(CoinManagementVM vm)
        {
            ResultsItem validationResult = CoinManagementValidation(vm, isSoldMode: false);
            if (!validationResult.IsSuccess) { return Json(validationResult); }

            vm.Coin.OrderType = Types.OrderType.Buy;
            vm.Coin.Exchange = Types.Exchanges.Custom;
            vm.Coin.PortfolioId = vm.SelectedPortfolioID;

            if (vm.Coin.TotalPricePaidUSD.GetValueOrDefault() == 0)
            {
                vm.Coin.TotalPricePaidUSD = vm.Coin.OrderDate.Date == DateTime.Now.Date ? vm.Coin.Shares * CryptoLogic.GetLatestPriceOfSymbol(vm.Coin.Symbol, await GetAllCoinsMarketDetailsAPI())
                                                                                        : CryptoLogic.GenerateTotalPricePaidUSD(vm.Coin, await GetAllHistoricCoinPrices());
            }

            var coinResult = vm.IsCreateMode ? await CryptoLogic.InsertCoinsToUserPortfolioAsync(new List<CryptoCoin> { vm.Coin }, CurrentUser, vm.SelectedPortfolioID)
                                             : await CryptoLogic.UpdateUserCoinAsync(vm.Coin, CurrentUser);

            return Json(coinResult);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> MarkCoinSold(CoinManagementVM vm)
        {
            ResultsItem validationResult = CoinManagementValidation(vm, isSoldMode: true);
            if (!validationResult.IsSuccess) { return Json(validationResult); }

            CryptoCoin fetchedCoin = await CryptoLogic.GetSingleCoinByUser(vm.Coin.CoinId, CurrentUser);
            if (fetchedCoin == null) { return Json(ResultsItem.Error("This coin cannot be found or belogns to someone else.")); }

            fetchedCoin.OrderType = Types.OrderType.Sell;
            fetchedCoin.OrderDate = vm.SoldDate;
            fetchedCoin.SoldCoinCurrency = vm.Coin.SoldCoinCurrency;
            fetchedCoin.SoldPricePerUnit = vm.Coin.SoldPricePerUnit;
            fetchedCoin.TotalSoldPricePaidUSD = vm.Coin.TotalSoldPricePaidUSD;
            if (fetchedCoin.TotalSoldPricePaidUSD.GetValueOrDefault() <= 0)
            {
                decimal totalPricePaidToUSD = CryptoLogic.GenerateTotalPricePaidUSD(fetchedCoin, await GetAllHistoricCoinPrices(), fetchedCoin.SoldCoinCurrency);
                if (totalPricePaidToUSD == 0)
                {
                    // Getting historic priced failed. Their sold date is probably from another dimension. Screw the user, just get latest from the market.
                    totalPricePaidToUSD = CryptoLogic.GetLatestPriceOfCurrency(vm.Coin.SoldCoinCurrency, await GetAllCoinsMarketDetailsAPI());
                }
                fetchedCoin.TotalSoldPricePaidUSD = totalPricePaidToUSD;
            }

            ResultsItem result = await CryptoLogic.UpdateUserCoinAsync(fetchedCoin, CurrentUser);
            return Json(result);
        }

        public async Task<decimal> GetCurrentCoinPrice(string symbol)
        {
            Types.CoinCurrency predictedCurrency = CryptoLogic.GenerateCoinCurrencyFromSymbol(symbol);
            if (predictedCurrency == Types.CoinCurrency.Unknown) { return 0; }

            CryptoCoin coin = (await UpdateCoinsCurrentPrice(new List<CryptoCoin> { new CryptoCoin { Symbol = symbol.ToUpperInvariant() } }, false)).First();
            return CryptoLogic.GetPricePerUnitOfCoin(coin, predictedCurrency);
        }

        public async Task<JsonResult> GetGeneratedWatchOnlyDetails(string symbol)
        {
            Types.CoinCurrency predictedCurrency = CryptoLogic.GenerateCoinCurrencyFromSymbol(symbol);
            if (predictedCurrency == Types.CoinCurrency.Unknown) { return Json(ResultsItem.Error("Unknown currency type. Please use USD-, BTC-, or ETH-")); }

            CryptoCoin coin = (await UpdateCoinsCurrentPrice(new List<CryptoCoin> { new CryptoCoin { Symbol = symbol.ToUpperInvariant() } }, false)).First();

            if (coin.MarketCoin?.CurrentSymbolPriceUSD <= 0)
            {
                return Json(ResultsItem.Error("This coin/symbol was not found. Did you use the correct format? E.g. BTC-XRP"));
            }

            decimal pricePerUnit = CryptoLogic.GetPricePerUnitOfCoin(coin, predictedCurrency);
            decimal quantity = decimal.Round(100 / coin.MarketCoin.CurrentSymbolPriceUSD, 5);

            return Json(new { pricePerUnit, quantity });
        }

        public async Task<JsonResult> DeleteCoin(int coinId, int portfolioId)
        {
            CryptoCoin fetchedCoin = await CryptoLogic.GetSingleCoinByUser(coinId, CurrentUser);
            if (fetchedCoin?.PortfolioId != portfolioId) { return Json(ResultsItem.Error(Lang.PortfolioNotFound)); }
            return Json(await CryptoLogic.DeleteUserCoinsAsync(new List<CryptoCoin> { new CryptoCoin { CoinId = coinId, PortfolioId = portfolioId } }, CurrentUser));
        }

        #endregion

        #region API Management (CRUD)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateNewImportAPI(ExchangeApiInfo exchangeApiInfo)
        {
            if (!ModelState.IsValid) { return Json(ResultsItem.Error(ModelState.GetAllErrorsString())); }

            exchangeApiInfo.ApiAction = Types.ApiAction.TradesImport;
            exchangeApiInfo.ApiPrivate = exchangeApiInfo.ApiPrivate.Replace("%2B", "+");
            ResultsItem apiResult = CryptoLogic.InsertNewAPI(exchangeApiInfo, CurrentUser);

            return Json(apiResult);
        }

        [HttpPost]
        public JsonResult DeleteImportAPI(int apiId)
        {
            ResultsItem results = CryptoLogic.DeleteAPI(apiId, CurrentUser);
            if (results.IsSuccess)
            {
                CurrentUser.ExchangeApiList = CurrentUser.ExchangeApiList.Where(x => x.Id != apiId).ToList();
                SubmitCurrentUserUpdate();
            }

            return Json(results);
        }

        #endregion

        #region Trades Management (Import CSV/API, Reset Trades, Etc.)

        public PartialViewResult ImportTradeHistory() // CSV only
        {
            return PartialView("Crud/_ImportTradeHistory");
        }

        public async Task<JsonResult> ImportSyncAPI(int apiId, int portfolioId)
        {
            ExchangeApiInfo exchangeApiInfo = CurrentUser.ExchangeApiList.FirstOrDefault(x => x.Id == apiId);
            if (exchangeApiInfo == null) { return Json(ResultsItem.Error("This API Id cannot be found")); }

            if (!CurrentUser.HasPortfolio(portfolioId)) { return Json(ResultsItem.Error(Lang.PortfolioNotFound)); }

            // Test -> Move to .Tests solution
            //List<Core.Data.ServiceModels.ImportedCoin> importedCoins = FetchAPILogic.ImportCSV_Coins(exchangeApiInfo.Exchange,
            //     System.IO.File.OpenRead(@"C:\Users\Ref\Downloads\pegatrade_importFiles\api_test1\2nd-half.csv"));

            List<Core.Data.ServiceModels.ImportedCoin> importedCoins = await FetchAPILogic.ImportAPI_Coins(exchangeApiInfo, CurrentUser);
            if (importedCoins.IsNullOrEmpty()) { return Json(ResultsItem.Error(Lang.UnableToImportWrongKeyPermission)); }

            List<CryptoCoin> existingExchangeCoins = (await CryptoLogic.GetAllUserCoinsByPortfolioIdAsync(portfolioId)).Where(x => x.Exchange == exchangeApiInfo.Exchange).ToList();
            DateTime latestDate = existingExchangeCoins.IsNullOrEmpty() ? DateTime.MinValue : existingExchangeCoins.Max(x => x.OrderDate);

            // Get rid of ANY imported coins that are less than already existing date. 
            // We dont' want to mess with those, we just want to add new ones.
            importedCoins = importedCoins.Where(x => x.OrderDate > latestDate).ToList();
            if (importedCoins.IsNullOrEmpty()) { Json(ResultsItem.Error(Lang.ApiAlreadyUptoDate)); }

            List<CryptoCoin> fetchedCoins = FetchAPILogic.FormatCoinsAndGenerateTotalPricePaid(importedCoins, await GetAllHistoricCoinPrices());
            if (fetchedCoins.Count > 0)
            {
                // Get all current holding coins that will be affected by new coins. Delete them from db as well, we'll re-add them.
                existingExchangeCoins = existingExchangeCoins.Where(x => x.OrderType == Types.OrderType.Buy && fetchedCoins.Any(f => f.Symbol.EqualsTo(x.Symbol))).ToList();
                await CryptoLogic.DeleteUserCoinsAsync(existingExchangeCoins, CurrentUser);

                existingExchangeCoins.ForEach(x => x.CoinId = 0); // Reset PK so DB can generate a new one.
                existingExchangeCoins.AddRange(fetchedCoins);
                existingExchangeCoins = CryptoLogic.FormatCoinsAndBoughtSoldLogicUpdate(existingExchangeCoins);
                
                ResultsItem insertResults = await CryptoLogic.InsertCoinsToUserPortfolioAsync(existingExchangeCoins, CurrentUser, portfolioId);
                if (insertResults.IsSuccess)
                {
                    return Json(ResultsItem.Success("Successfully imported coins to your portfolio."));
                }
            }

            return Json(ResultsItem.Error(Lang.ApiNoNewTradesOrError));
        }

        // Warn, this will delete previous imported Exchange (BitTrex, Kraken, etc) trades
        public async Task<IActionResult> PostCSVTradeHistoryFile(IFormFile file, Types.Exchanges exchange, int portfolioId)
        {
            if (file == null || file.Length > 100000 || 
                (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)))
            {
                return Json(ResultsItem.Error("Please select a proper .csv/.xlsx file that is less than 100kb."));
            }

            List<Core.Data.ServiceModels.ImportedCoin> importedCoins = new List<Core.Data.ServiceModels.ImportedCoin>();
            using (var stream = file.OpenReadStream())
            {
                importedCoins = FetchAPILogic.ImportCSV_Coins(exchange, stream);
            }

            if (importedCoins.IsNullOrEmpty()) { return Json(ResultsItem.Error(Lang.ImportFailedCSV)); }

            int maxAllowedImport = SubscriptionLogic.GetMaxAllowedTradesImportPerUser(CurrentUser.PTUserInfo.SubscriptionLevel);
            if (importedCoins.Count > maxAllowedImport) { return Json(ResultsItem.Error(string.Format(Lang.CSVMaxImportAllowed, maxAllowedImport))); }

            List<CryptoCoin> fetchedCoins = FetchAPILogic.FormatCoinsAndGenerateTotalPricePaid(importedCoins, await GetAllHistoricCoinPrices());

            if (fetchedCoins.Count > 0)
            {
                fetchedCoins = CryptoLogic.FormatCoinsAndBoughtSoldLogicUpdate(fetchedCoins);

                await CryptoLogic.DeleteAllUserCoinByExchangeAsync(portfolioId, exchange, CurrentUser);

                ResultsItem insertResults = await CryptoLogic.InsertCoinsToUserPortfolioAsync(fetchedCoins, CurrentUser, portfolioId);
                if (insertResults.IsSuccess)
                {
                    return Json(ResultsItem.Success("Successfully imported coins to your portfolio."));
                }
            }

            return Json(ResultsItem.Error("An error occured when trying to import trades. Are you sure the import .csv file has correct format?"));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ImportEtherAddress(ExchangeApiInfo apiInfo)
        {
            var resultsPair = await FetchAPILogic.ApiImport_EtherAddress(await GetAllCoinsMarketDetailsAPI(), apiInfo.ApiPublic, apiInfo.PortfolioID, CurrentUser);
            if (!resultsPair.Result.IsSuccess) { return Json(resultsPair.Result); }
            
            await CryptoLogic.DeleteAllUserCoinByExchangeAsync(apiInfo.PortfolioID, Types.Exchanges.EtherAddressLookup, CurrentUser);
            ResultsItem insertResult = await CryptoLogic.InsertCoinsToUserPortfolioAsync(resultsPair.Value, CurrentUser, apiInfo.PortfolioID);
            return Json(insertResult);
        }

        public async Task<JsonResult> ResetAllTradesData()
        {
            var result = await CryptoLogic.ResetAllUserTrades(CurrentUser);
            if (result.Result.IsSuccess)
            {
                CurrentUser.Portfolios = new List<Portfolio> { result.Value };
                SubmitCurrentUserUpdate();
            }

            return Json(result.Result);
        }

        #endregion

        #region Private Methods / Tasks

        private async Task<List<CryptoCoin>> UpdateCoinsCurrentPrice(List<CryptoCoin> coins, bool useCombinedDisplay = true, Types.CoinCurrency currency = Types.CoinCurrency.USD)
        {
            // Get all API coins that has the latest price, and etc. (CoinMarketCap price get)
            List<MarketCoin> apiFetchedCoins = await GetAllCoinsMarketDetailsAPI();

            Dictionary<int, HistoricCoinPrice> historicPrices = new Dictionary<int, HistoricCoinPrice>();
            if ((currency == Types.CoinCurrency.BTC || currency == Types.CoinCurrency.ETH) && coins.Any(x => x.OrderDate > DateTime.MinValue)) { historicPrices = await GetAllHistoricCoinPrices(); }

            return CryptoLogic.UpdateCoinsCurrentPrice(coins, apiFetchedCoins, historicPrices, useCombinedDisplay, currency);
        }

        private ResultsItem CoinManagementValidation(CoinManagementVM vm, bool isSoldMode = false)
        {
            if (!ModelState.IsValid) { return ResultsItem.Error(ModelState.GetAllErrorsString()); }

            if (!isSoldMode)
            {
                vm.Coin.Symbol = vm.Coin.Symbol.ToUpperInvariant();
                if (!vm.Coin.Symbol.StartsWith("BTC-") && !vm.Coin.Symbol.StartsWith("ETH-") && !vm.Coin.Symbol.StartsWith("USD-") && !vm.Coin.Symbol.StartsWith("USDT-")) { return ResultsItem.Error("Error: The coin symbol must start with BTC-, ETH-, USD-, or USDT-"); }

                if (vm.Coin.OrderDate > DateTime.Now) { return ResultsItem.Error("Error: Order date must be today or the past."); }

                vm.Coin.CoinCurrency = CryptoLogic.GenerateCoinCurrencyFromSymbol(vm.Coin.Symbol);
                if (vm.Coin.CoinCurrency == Types.CoinCurrency.Unknown || vm.Coin.CoinCurrency == Types.CoinCurrency.EUR) { return ResultsItem.Error("Error: only BTC, ETH, and USD currency is currently supported."); }

                if (vm.Coin.PricePerUnit <= 0) { return ResultsItem.Error("Please enter Price Per Unit."); }
            }
            else
            {
                if (!CurrentUser.HasPortfolio(vm.Coin.PortfolioId)) { return ResultsItem.Error(Lang.PortfolioNotFound); }
                if (vm.Coin.SoldCoinCurrency == Types.CoinCurrency.Unknown || vm.Coin.SoldPricePerUnit.GetValueOrDefault() <= 0)
                {
                    return ResultsItem.Error("Please enter all sold details. TotalSoldPrice is optional.");
                }

                if (vm.SoldDate < vm.Coin.OrderDate) { return ResultsItem.Error("Sold date cannot be lower than Buy date. Unless you're a time traveler."); }
            }

            return ResultsItem.Success();
        }

        #endregion
    }
}
