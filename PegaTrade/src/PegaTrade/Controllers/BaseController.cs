using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Coins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models.Community;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Controllers
{
    public abstract class BaseController : Controller
    {
        protected IMemoryCache _memoryCache;
        protected IHostingEnvironment _hostingEnvironment;

        internal BaseController() { }
        internal BaseController(IMemoryCache cache, IHostingEnvironment environment)
        {
            _memoryCache = cache;
            _hostingEnvironment = environment;
        }

        #region Session/Cache

        protected T GetSession<T>(string key)
        {
            string sessionStr = HttpContext.Session.GetString(key);
            return string.IsNullOrEmpty(sessionStr) ? default(T) : Utilities.Deserialize<T>(sessionStr);
        }

        protected void SetSession(string key, object value)
        {
            HttpContext.Session.SetString(key, Utilities.Serialize(value));
        }

        private PegaUser _currentUser;
        protected PegaUser CurrentUser => _currentUser ?? (_currentUser = GetSession<PegaUser>(Constant.Session.SessionCurrentUser));
        protected void SubmitCurrentUserUpdate() => SetSession(Constant.Session.SessionCurrentUser, CurrentUser);

        public async Task<List<MarketCoin>> GetAllCoinsMarketDetailsAPI()
        {
            if (_memoryCache.TryGetValue(Constant.Session.GlobalAllCoinsAPIData, out MarketFetchData marketData))
            {
                if (marketData.LastUpdated.AddSeconds(45) < DateTime.Now) // Update/Refresh Data
                {
                    if (marketData.CurrentlyUpdating && marketData.LastUpdated.AddMinutes(3) > DateTime.Now) // It's already updating & less than 3 minutes has passed since API call
                    {
                        // Just wait for it to get automatically get updated in the threadpool. Return outdated data for now.
                        return marketData.MarketCoins;
                    }

                    // Data is outdated & It's not updating yet || It's trying to update, it's been over 3 minutes, still nothing. Retry again. API Server is slow/down.
                    if (!marketData.CurrentlyUpdating)
                    {
                        marketData.CurrentlyUpdating = true;
                        _memoryCache.Set(Constant.Session.GlobalAllCoinsAPIData, marketData, TimeSpan.FromHours(1));
                    }

                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        // Start an update process in the background.
                        MarketFetchData data = new MarketFetchData
                        {
                            MarketCoins = FetchAPILogic.GetAllCoinsFromApiAsync().Result,
                            LastUpdated = DateTime.Now
                        };
                        if (!data.MarketCoins.IsNullOrEmpty())
                        {
                            _memoryCache.Set(Constant.Session.GlobalAllCoinsAPIData, data, TimeSpan.FromHours(1));
                        }
                    });
                }

                // Fresh data
                return marketData.MarketCoins;
            }
            
            List<MarketCoin> fetchedCoins = await FetchAPILogic.GetAllCoinsFromApiAsync();
            MarketFetchData newData = new MarketFetchData
            {
                MarketCoins = fetchedCoins,
                LastUpdated = DateTime.Now
            };
            _memoryCache.Set(Constant.Session.GlobalAllCoinsAPIData, newData, TimeSpan.FromHours(1));
            return newData.MarketCoins;
        }

        // identifier -> symbol or name
        public async Task<OfficialCoin> GetOfficialCoin(string identifier)
        {
            async Task<OfficialCoinFetchData> UpdateAndStoreOfficialCoinData(OfficialCoinFetchData officialCoinFetchData)
            {
                List<MarketCoin> marketCoins = await GetAllCoinsMarketDetailsAPI();
                officialCoinFetchData.OfficialCoins.ForEach(x => { x.MarketCoin = marketCoins.FirstOrDefault(m => m.CoinMarketCapID == x.Name); });
                officialCoinFetchData.LastMarketUpdated = DateTime.Now;
                _memoryCache.Set(Constant.Session.GlobalAllOfficialCoins, officialCoinFetchData, TimeSpan.FromDays(7));
                return officialCoinFetchData;
            }

            if (_memoryCache.TryGetValue(Constant.Session.GlobalAllOfficialCoins, out OfficialCoinFetchData fetchData))
            {
                if (fetchData.LastMarketUpdated.AddMinutes(1) < DateTime.Now) { fetchData = await UpdateAndStoreOfficialCoinData(fetchData); } // Update market result
                return CryptoLogic.FindOfficialCoinFromIdentifier(identifier, fetchData.OfficialCoins);
            }

            // Data does not exist yet.
            fetchData = new OfficialCoinFetchData { OfficialCoins = CryptoLogic.GetAllOfficialCoins() };
            fetchData = await UpdateAndStoreOfficialCoinData(fetchData);

            return CryptoLogic.FindOfficialCoinFromIdentifier(identifier, fetchData.OfficialCoins);
        }

        public async Task<Dictionary<int, HistoricCoinPrice>> GetAllHistoricCoinPrices()
        {
            if (!_memoryCache.TryGetValue(Constant.Session.GlobalHistoricCoinsPrice, out Dictionary<int, HistoricCoinPrice> historicPrices))
            {
                string serverMapPath = System.IO.Path.Combine(_hostingEnvironment.WebRootPath, "files\\prices");
                var results = await FetchAPILogic.GetAllHistoricPrice_BTC_ETH(serverMapPath: serverMapPath);
                _memoryCache.Set(Constant.Session.GlobalHistoricCoinsPrice, results, TimeSpan.FromHours(24));

                return results;
            }

            return historicPrices;
        }

        public async Task<List<BBThread>> GetThreadsByCategory(int take, Types.ConvThreadCategory category, int officialCoinId = 0)
        {
            if (_memoryCache.TryGetValue(Constant.Session.GlobalThreadsByCategory, out List<ConversationFetchData> fetchData))
            {
                if (!fetchData.IsNullOrEmpty() && fetchData.Any(x => x.Take == take && x.Category == category && x.OfficialCoinId == officialCoinId))
                {
                    // This results already exist. 
                    ConversationFetchData desiredResult = fetchData.First(x => x.Take == take && x.Category == category && x.OfficialCoinId == officialCoinId);
                    return desiredResult.Results;
                }
            }

            // Results does not exist, or fetchData itself is non-existant. 
            if (fetchData.IsNullOrEmpty()) { fetchData = new List<ConversationFetchData>(); }

            List<BBThread> results = await CommunityLogic.GetMessageThreads(50, Types.ConvThreadCategory.MainDashboard, CurrentUser, officialCoinId: officialCoinId);
            fetchData.Add(new ConversationFetchData
            {
                Take = take,
                Category = category,
                OfficialCoinId = officialCoinId,
                Results = results
            });
            _memoryCache.Set(Constant.Session.GlobalThreadsByCategory, fetchData, TimeSpan.FromHours(24));

            return results;
        }

        public async Task<BBThread> GetThreadWithComments(int threadId)
        {
            if (_memoryCache.TryGetValue(Constant.Session.GlobalThreadsWithComments, out List<BBThread> fetchData))
            {
                if (!fetchData.IsNullOrEmpty() && fetchData.Any(x => x.ThreadId == threadId))
                {
                    return fetchData.First(x => x.ThreadId == threadId);
                }
            }

            // Results does not exist, or fetchData itself is non-existant. 
            if (fetchData.IsNullOrEmpty()) { fetchData = new List<BBThread>(); }

            BBThread result = await CommunityLogic.GetThreadWithComments(threadId, CurrentUser);
            fetchData.Add(result);
            _memoryCache.Set(Constant.Session.GlobalThreadsWithComments, fetchData, TimeSpan.FromHours(24));

            return result;
        }

        // Remove the updated threads from session, so a fresh version can be fetched again.
        public void OnThreadUpdatedCacheHandler(Types.ConvThreadCategory category = Types.ConvThreadCategory.Unknown, int officialCoinId = 0, int threadId = 0)
        {
            if (_memoryCache.TryGetValue(Constant.Session.GlobalThreadsByCategory, out List<ConversationFetchData> fetchData))
            {
                if (fetchData.IsNullOrEmpty()) { return; }

                if (threadId > 0)
                {
                    if (fetchData.Any(x => x.Results.Any(t => t.ThreadId == threadId)))
                    {
                        fetchData.RemoveAll(x => x.Results.Any(t => t.ThreadId == threadId));
                        _memoryCache.Set(Constant.Session.GlobalThreadsByCategory, fetchData, TimeSpan.FromHours(24));
                    }
                }
                else if (fetchData.Any(x => x.Category == category && x.OfficialCoinId == officialCoinId))
                {
                    ConversationFetchData desiredResult = fetchData.First(x => x.Category == category && x.OfficialCoinId == officialCoinId);
                    fetchData.Remove(desiredResult);

                    _memoryCache.Set(Constant.Session.GlobalThreadsByCategory, fetchData, TimeSpan.FromHours(24));
                }
            }
        }

        // Remove the updated thread with comments from session, so a fresh version can be fetched again.
        public void OnCommentsUpdatedCacheHandler(int threadId = 0, long commentId = 0)
        {
            if (_memoryCache.TryGetValue(Constant.Session.GlobalThreadsWithComments, out List<BBThread> fetchData))
            {
                if (fetchData.IsNullOrEmpty()) { return; }

                if (commentId > 0)
                {
                    if (fetchData.Any(x => x.ThreadComments.Any(c => c.CommentId == commentId)))
                    {
                        fetchData.RemoveAll(x => x.ThreadComments.Any(c => c.CommentId == commentId));
                        _memoryCache.Set(Constant.Session.GlobalThreadsWithComments, fetchData, TimeSpan.FromHours(24));
                    }
                }
                else if (fetchData.Any(x => x.ThreadId == threadId))
                {
                    BBThread desiredResult = fetchData.First(x => x.ThreadId == threadId);
                    fetchData.Remove(desiredResult);

                    _memoryCache.Set(Constant.Session.GlobalThreadsWithComments, fetchData, TimeSpan.FromHours(24));
                }
            }
        }

        #endregion

        public PartialViewResult GeneratePartialViewError(string errorMessage)
        {
            ResultsItem result = new ResultsItem
            {
                MessageColor = "text-danger font-strong",
                Message = errorMessage
            };
            return PartialView("~/Views/Shared/_Messages.cshtml", result);
        }
    }
}
