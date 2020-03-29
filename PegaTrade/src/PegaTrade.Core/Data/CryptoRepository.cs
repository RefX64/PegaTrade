using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PegaTrade.Core.EntityFramework;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Helpers;
using LocalCoins = PegaTrade.Layer.Models.Coins;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models.Account;

namespace PegaTrade.Core.Data
{
    // EF Core-CRUD help: https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/crud
    internal static class CryptoRepository
    {
        #region Portfolio
        
        public static async Task<List<LocalCoins.Portfolio>> GetAllUserPortfolioAsync(PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<Portfolios> servicePortfolios = await db.Portfolios.Where(x => x.UserId == user.UserId).ToListAsync();

                    if (servicePortfolios.IsNullOrEmpty())
                    {
                        var result = await InsertNewPortfolio(user.UserId, "Default", Types.PortfolioDisplayType.Private, isDefault: true);
                        result.Value.OwnerUsername = user.Username;
                        return new List<LocalCoins.Portfolio> { result.Value };
                    }

                    List<LocalCoins.Portfolio> localPortfolios = servicePortfolios.Adapt<List<LocalCoins.Portfolio>>();
                    localPortfolios.ForEach(x => x.OwnerUsername = user.Username);
                    return localPortfolios;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "GetAllUserPortfolio", $"UserId:{user.UserId}" }, ex);
                return new List<LocalCoins.Portfolio>();
            }
        }

        public static async Task<ResultsPair<LocalCoins.Portfolio>> InsertNewPortfolio(int userId, string portfolioName, Types.PortfolioDisplayType displayType, bool isDefault)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    Portfolios portfolio = new Portfolios
                    {
                        UserId = userId,
                        Name = portfolioName.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM }),
                        DisplayType = (short)displayType,
                        IsDefault = isDefault
                    };

                    db.Portfolios.Add(portfolio);
                    await db.SaveChangesAsync();

                    return ResultsPair.Create(ResultsItem.Success("Successfully created a new portfolio."), portfolio.Adapt<LocalCoins.Portfolio>());
                }
            }
            catch (Exception ex)
            {
                return ResultsPair.CreateError<LocalCoins.Portfolio>($"Unable to create a new portfolio. {ex.Message}");
            }
        }

        public static ResultsPair<LocalCoins.Portfolio> UpdatePortfolio(int portfolioId, string portfolioName, Types.PortfolioDisplayType displayType, bool isDefault)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    Portfolios portfolio = db.Portfolios.FirstOrDefault(x => x.PortfolioId == portfolioId);
                    if (portfolio != null)
                    {
                        portfolio.Name = portfolioName.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });
                        portfolio.DisplayType = (short)displayType;
                        portfolio.IsDefault = isDefault;

                        db.Update(portfolio);
                        db.SaveChanges();
                    }

                    return ResultsPair.Create(ResultsItem.Success("Successfully updated portfolio name."), portfolio.Adapt<LocalCoins.Portfolio>());
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "UpdatePortfolio", $"portfolioId:{portfolioId},portfolioName:{portfolioName}" }, ex);
                return ResultsPair.CreateError<LocalCoins.Portfolio>($"Unable to update portfolio. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> DeletePortfolio(int portfolioId)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    Portfolios portfolio = db.Portfolios.FirstOrDefault(x => x.PortfolioId == portfolioId);
                    if (portfolio != null)
                    {
                        db.Portfolios.Remove(portfolio);
                        await db.SaveChangesAsync();
                    }
                }

                return ResultsItem.Success("Successfully deleted portfolio.");
            }
            catch (Exception ex)
            {
                return ResultsItem.Error($"Unable to delete portfolio. {ex.Message}");
            }
        }

        #endregion

        #region Coins
        // Todo: Implement coin security. Logged in User's portfolio must contain the ID coins are being CRUD'ed to. 

        public static async Task<LocalCoins.CryptoCoin> GetSingleCoinByUser(long coinId)
        {
            try
            {
                TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var serviceCoin = await db.Coins.FirstOrDefaultAsync(x => x.CoinId == coinId);
                    if (serviceCoin != null) { return serviceCoin.Adapt<LocalCoins.CryptoCoin>(); }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "GetAllUserCoinsByPortfolioIdAsync", $"coinId:{coinId}" }, ex);
            }
            return null;
        }

        public static async Task<List<LocalCoins.CryptoCoin>> GetAllUserCoinsByPortfolioIdAsync(int portfolioId)
        {
            try
            {
                TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<Coins> serviceCoins = await db.Coins.Where(x => x.PortfolioId == portfolioId).ToListAsync();
                    List<LocalCoins.CryptoCoin> localPortfolios = serviceCoins.Adapt<List<LocalCoins.CryptoCoin>>();
                    return localPortfolios;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "GetAllUserCoinsByPortfolioIdAsync", $"portfolioId:{portfolioId}" }, ex);
                return new List<LocalCoins.CryptoCoin>();
            }
        }

        public static async Task<ResultsItem> InsertCoinsToUserPortfolioAsync(List<LocalCoins.CryptoCoin> coins)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<Coins> serviceCoins = coins.Where(x => x.Symbol.Length < 10).Select(x => new Coins
                    {
                        CoinId = x.CoinId,
                        Symbol = x.Symbol.Clean(new [] { Types.CleanInputType.Letters, Types.CleanInputType.Digits, Types.CleanInputType.Dash }),
                        Shares = x.Shares,
                        OrderDate = x.OrderDate,
                        CoinCurrency = (short)x.CoinCurrency,
                        PricePerUnit = x.PricePerUnit,
                        TotalPricePaidUsd = x.TotalPricePaidUSD,
                        OrderType = (short)x.OrderType,
                        //Notes = x.Notes,
                        Exchange = (short)x.Exchange,
                        PortfolioId = x.PortfolioId,
                        SoldCoinCurrency = (short)x.SoldCoinCurrency,
                        SoldPricePerUnit = x.SoldPricePerUnit,
                        TotalSoldPricePaidUsd = x.TotalSoldPricePaidUSD,
                    }).ToList();
                    
                    db.AddRange(serviceCoins);
                    await db.SaveChangesAsync();
                }

                return ResultsItem.Success("Successfully added all the coins to your portfolio.");
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "InsertCoinsToUserPortfolioAsync", $"portfolioId:{coins.First().PortfolioId}" }, ex);
                return ResultsItem.Error($"Unable to add all the coins to your portfolio. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> UpdateUserCoinAsync(LocalCoins.CryptoCoin coin)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    Coins serviceCoin = db.Coins.FirstOrDefault(x => x.CoinId == coin.CoinId);
                    if (serviceCoin == null) { return ResultsItem.Error("This coin was not found."); }

                    serviceCoin.Shares = coin.Shares;
                    serviceCoin.CoinCurrency = (short)coin.CoinCurrency;
                    serviceCoin.PricePerUnit = coin.PricePerUnit;
                    serviceCoin.TotalPricePaidUsd = coin.TotalPricePaidUSD;
                    serviceCoin.Exchange = (short)coin.Exchange;
                    serviceCoin.OrderType = (short)coin.OrderType;
                    serviceCoin.OrderDate = coin.OrderDate;
                    serviceCoin.SoldCoinCurrency = (short)coin.SoldCoinCurrency;
                    serviceCoin.SoldPricePerUnit = coin.SoldPricePerUnit;
                    serviceCoin.TotalSoldPricePaidUsd = coin.TotalSoldPricePaidUSD;

                    db.Update(serviceCoin);
                    await db.SaveChangesAsync();
                }

                return ResultsItem.Success("Successfully updated coin.");
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "UpdateUserCoinAsync", $"coinId:{coin.CoinId},portfolioId:{coin.PortfolioId}" }, ex);
                return ResultsItem.Error($"Unable to updated coin. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> DeleteUserCoinAsync(List<LocalCoins.CryptoCoin> coins)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<Coins> serviceCoins = coins.Select(x => new Coins { CoinId = x.CoinId }).ToList();

                    db.RemoveRange(serviceCoins);
                    await db.SaveChangesAsync();
                }

                return ResultsItem.Success("Successfully deleted coin.");
            }
            catch (Exception ex)
            {
                return ResultsItem.Error($"Unable to delete coin. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> DeleteAllUserCoinByExchangeAsync(int portfolioId, Types.Exchanges exchange)
        {
            try
            {
                int exchangeId = (int)exchange;
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var coins = db.Coins.Where(x => x.PortfolioId == portfolioId && x.Exchange == exchangeId);
                    db.Coins.RemoveRange(coins);

                    await db.SaveChangesAsync();
                }

                return ResultsItem.Success("Successfully deleted all coins by exchange.");
            }
            catch (Exception ex)
            {
                return ResultsItem.Error($"Unable to delete all coins. {ex.Message}");
            }
        }

        public static async Task<ResultsPair<LocalCoins.Portfolio>> ResetAllUserTrades(PegaUser user)
        {
            ResultsPair<LocalCoins.Portfolio> generateError(string error) { return ResultsPair.CreateError<LocalCoins.Portfolio>(error); }
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var portfolios = db.Portfolios.Where(x => user.Portfolios.Any(p => p.PortfolioId == x.PortfolioId));
                    db.Portfolios.RemoveRange(portfolios);

                    await db.SaveChangesAsync();

                    var insertPortfolioResult = await InsertNewPortfolio(user.UserId, "Default", Types.PortfolioDisplayType.Public, true);
                    if (insertPortfolioResult.Result.IsSuccess)
                    {
                        insertPortfolioResult.Result.Message = "Successfully reset all of your trades.";
                        return insertPortfolioResult;
                    }

                    return generateError("An error occured during the reset process. Please re-login and try again.");
                }
            }
            catch (Exception ex)
            {
                return generateError($"An error occured during the reset process. Please re-login and try again. Error: {ex.Message}");
            }
        }

        #endregion

        #region Official Coins
        
        public static List<LocalCoins.OfficialCoin> GetAllOfficialCoins()
        {
            using (PegasunDBContext db = new PegasunDBContext())
            {
                List<OfficialCoins> coins = db.OfficialCoins.ToList();
                return coins.Adapt<List<LocalCoins.OfficialCoin>>();
            }
        }
        
        #endregion

        #region API

        public static async Task<List<LocalCoins.ExchangeApiInfo>> GetAllUserExchangeAPIAsync(int userId)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<ApiDetails> apiDetails = await db.ApiDetails.Where(x => x.UserId == userId).ToListAsync();
                    List<LocalCoins.ExchangeApiInfo> localApis = apiDetails.Adapt<List<LocalCoins.ExchangeApiInfo>>();

                    return localApis;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "GetAllUserExchangeAPIAsync", $"UserId:{userId}" }, ex);
                return new List<LocalCoins.ExchangeApiInfo>();
            }
        }

        public static ResultsItem InsertNewAPI(LocalCoins.ExchangeApiInfo exchangeApiDetails, PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    ApiDetails serviceApi = new ApiDetails
                    {
                        Name = exchangeApiDetails.Name.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM }),
                        Exchange = (short)exchangeApiDetails.Exchange,
                        ApiAction = (short)exchangeApiDetails.ApiAction,
                        ApiPublic = Cryptography.Encrypt(exchangeApiDetails.ApiPublic, Types.EncryptionType.ApiKey_Public, user),
                        ApiPrivate = Cryptography.Encrypt(exchangeApiDetails.ApiPrivate, Types.EncryptionType.ApiKey_Private, user),
                        ApiThirdKey = exchangeApiDetails.ApiThirdKey.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM }),
                        DateAdded = DateTime.Now,
                        UserId = user.UserId,
                    };

                    db.ApiDetails.Add(serviceApi);
                    db.SaveChanges();
                }

                return ResultsItem.Success("Successfully created a new API.");
            }
            catch (Exception ex)
            {
                return ResultsItem.Error($"Unable to create a new API. {ex.Message}");
            }
        }

        public static ResultsItem DeleteAPI(int apiId)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    ApiDetails apiDetail = db.ApiDetails.FirstOrDefault(x => x.Id == apiId);
                    if (apiDetail != null)
                    {
                        db.ApiDetails.Remove(apiDetail);
                        db.SaveChanges();
                    }
                }

                return ResultsItem.Success("Successfully updated deleted API.");
            }
            catch (Exception ex)
            {
                return ResultsItem.Error($"Unable to delete API. {ex.Message}");
            }
        }

        #endregion
    }
}
