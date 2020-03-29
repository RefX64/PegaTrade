using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using PegaTrade.Core.Data.ServiceModels;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Layer.Language;
using PegaTrade.Layer.Models.Helpers;
using LocalCoins = PegaTrade.Layer.Models.Coins;
using LocalAccount = PegaTrade.Layer.Models.Account;
using static PegaTrade.Layer.Models.Types;
using PegaTrade.Core.EntityFramework;
using Mapster;
using PegaTrade.Core.StaticLogic.Helper;

namespace PegaTrade.Core.Data
{
    public static class AuthorizationRepository
    {
        private static readonly string baseUrl = AppSettingsProvider.PegasunAPIUrl;

        public static async Task<ResultsPair<LocalAccount.PegaUser>> AuthorizeUser(string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{baseUrl}/v1/Account/Authenticate?username={username}&password={password}");
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    PegasunAPIAuthorization apiUser = NetJSON.NetJSON.Deserialize<PegasunAPIAuthorization>(jsonString);

                    if (apiUser != null && apiUser.result.resultType == 0)
                    {
                        // Todo: Move this out? make less calls
                        var portfolioTask = CryptoLogic.GetAllUserPortfolioAsync(new LocalAccount.PegaUser {  UserId = apiUser.userId, Username = apiUser.username });
                        var apiResultsTask = CryptoLogic.GetAllUserExchangeAPIAsync(apiUser.userId);
                        var ptUserInfoTask = GetPTUserInfo(apiUser.userId);
                        var portfolios = await portfolioTask;
                        var apiList = await apiResultsTask;
                        var ptUserInfo = await ptUserInfoTask;

                        return ResultsPair.CreateSuccess(new LocalAccount.PegaUser
                        {
                            UserId = apiUser.userId,
                            Username = apiUser.username,
                            Email = apiUser.email,
                            FullName = apiUser.fullName,
                            Portfolios = portfolios,
                            ExchangeApiList = apiList,
                            PTUserInfo = ptUserInfo,
                            EmailConfirmed = apiUser.emailConfirmed
                        });
                    }

                    return ResultsPair.CreateError<LocalAccount.PegaUser>(apiUser?.result.message ?? "An unknown error occured.");
                }
            }

            return  ResultsPair.CreateError<LocalAccount.PegaUser>(Lang.ServerConnectionError);
        }

        // Creates a new user and then redirects them to the login page.
        public static async Task<ResultsPair<LocalAccount.PegaUser>> CreateNewUser(LocalAccount.PegaUser user)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", user.Username),
                    new KeyValuePair<string, string>("Password", user.Password),
                    new KeyValuePair<string, string>("Email", user.Email),
                    new KeyValuePair<string, string>("EmailSubscribePreferenceCode", ((int)EmailPreferences.Default).ToString()),
                    new KeyValuePair<string, string>("platform", ((int)Platform.PegaTrade).ToString())
                });

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/v1/Account/Create", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    ResultsItem createUserResult = NetJSON.NetJSON.Deserialize<PegasunAPIResult>(jsonString).ConvertToResultsItem();
                    if (createUserResult.ResultType == ResultsType.Successful)
                    {
                        var getUserResult = await AuthorizeUser(user.Username, user.Password);
                        if (!getUserResult.Result.IsSuccess) { return getUserResult; }

                        return ResultsPair.Create(createUserResult, getUserResult.Value);
                    }

                    return ResultsPair.CreateError<LocalAccount.PegaUser>(createUserResult.Message);
                }
            }

            return ResultsPair.CreateError<LocalAccount.PegaUser>(Lang.ServerConnectionError);
        }

        public static ResultsItem RequestPasswordReset(string email)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("email", email),
                    new KeyValuePair<string, string>("platform", ((int)Platform.PegaTrade).ToString())
                });

                HttpResponseMessage response = client.PostAsync($"{baseUrl}/v1/Account/RequestResetPassword", content).Result;
                return RepositoryHelper.GetSimpleHttpResponseResult(response, Lang.PasswordResetEmailSent);
            }
        }

        public static async Task<ResultsItem> AuthorizeResetPassword(string username, string authCode)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", username),
                    new KeyValuePair<string, string>("AuthCode", authCode),
                    new KeyValuePair<string, string>("AuthType", ((int)ConfirmationAuthType.PasswordChange).ToString())
                });

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/v1/Account/ConfirmPendingAuth", content);
                return RepositoryHelper.GetSimpleHttpResponseResult(response, string.Empty);
            }
        }

        public static async Task<ResultsItem> ChangePassword(string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", username),
                    new KeyValuePair<string, string>("Password", password)
                });

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/v1/Account/ChangePassword", content);
                return RepositoryHelper.GetSimpleHttpResponseResult(response, string.Empty);
            }
        }

        public static async Task<ResultsItem> ConfirmEmail(string username, string authCode)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", username),
                    new KeyValuePair<string, string>("AuthCode", authCode),
                    new KeyValuePair<string, string>("AuthType", ((int)ConfirmationAuthType.EmailConfirm).ToString())
                });

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/v1/Account/ConfirmPendingAuth", content);
                return RepositoryHelper.GetSimpleHttpResponseResult(response, "Your email has been confirmed. Please login again.");
            }
        }

        public static ResultsPair<LocalAccount.ViewUser> AuthorizeViewUser(string username, string portfolioName)
        {
            using (PegasunDBContext db = new PegasunDBContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Username == username);
                if (user == null) { return ResultsPair.CreateError<LocalAccount.ViewUser>($"Could not find the user {username}."); }

                var portfolios = db.Portfolios.Where(x => x.UserId == user.UserId && x.DisplayType == 3).ToList();
                if (portfolios.IsNullOrEmpty() || !portfolios.Any(x => Utilities.FormatPortfolioName(x.Name).EqualsTo(portfolioName)))
                {
                    return ResultsPair.CreateError<LocalAccount.ViewUser>($"Could not find the portfolio {portfolioName}");
                }

                return ResultsPair.CreateSuccess(new LocalAccount.ViewUser
                {
                    PortfolioName = portfolioName,
                    SelectedPortfolioID = portfolios.First(x => Utilities.FormatPortfolioName(x.Name).EqualsTo(portfolioName)).PortfolioId,
                    Portfolios = portfolios.Adapt<List<LocalCoins.Portfolio>>(),
                    Username = user.Username
                });
            }
        }

        public static ResultsPair<LocalAccount.PegaUser> ViewUserProfile(string username)
        {
            using (PegasunDBContext db = new PegasunDBContext())
            {
                var user = db.Users.FirstOrDefault(x => x.Username == username);
                if (user == null) { return ResultsPair.CreateError<LocalAccount.PegaUser>($"Could not find the user {username}."); }

                var portfolios = db.Portfolios.Where(x => x.UserId == user.UserId && x.DisplayType == 3).ToList();

                LocalAccount.PegaUser localUser = user.Adapt<LocalAccount.PegaUser>();
                localUser.Portfolios = portfolios.Adapt<List<LocalCoins.Portfolio>>();
                localUser.TotalCreatedThreads = db.BBThreads.Count(x => x.UserId == user.UserId);
                localUser.TotalCreatedComments = db.BBComments.Count(x => x.UserId == user.UserId);
                
                return ResultsPair.CreateSuccess(localUser);
            }
        }

        public static async Task<ResultsItem> DeletePegaUser(string username)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", username)
                });

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/v1/Account/DeleteUser", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    return NetJSON.NetJSON.Deserialize<PegasunAPIResult>(jsonString).ConvertToResultsItem();
                }
            }

            return ResultsItem.Error(Lang.ServerConnectionError);
        }

        private static async Task<LocalAccount.PTUserInfo> GetPTUserInfo(int userId)
        {
            try
            {
                TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    PTUserInfo ptUserInfo = db.PTUserInfo.FirstOrDefault(x => x.UserId == userId);
                    if (ptUserInfo == null) { return (await CreateNewPTUserInfo(new LocalAccount.PegaUser { UserId = userId })).Value; }
                    return ptUserInfo.Adapt<LocalAccount.PTUserInfo>();
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task<ResultsPair<LocalAccount.PTUserInfo>> CreateNewPTUserInfo(LocalAccount.PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    PTUserInfo ptUserInfo = new PTUserInfo
                    {
                        UserId = user.UserId,
                        SubscriptionLevel = (byte)SubscriptionLevel.Free,
                        SubscriptionExpireDate = null
                    };

                    db.PTUserInfo.Add(ptUserInfo);
                    await db.SaveChangesAsync();

                    LocalAccount.PTUserInfo localSubscription = ptUserInfo.Adapt<LocalAccount.PTUserInfo>();

                    return ResultsPair.Create(ResultsItem.Success("Successfully created a new subscription."), localSubscription);
                }
            }
            catch (Exception ex)
            {
                return ResultsPair.CreateError<LocalAccount.PTUserInfo>($"Unable to create a new PTUserInfo: {ex.Message}");
            }
        }
    }
}
