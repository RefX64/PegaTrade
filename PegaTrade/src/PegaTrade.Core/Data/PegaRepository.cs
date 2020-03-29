using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Helpers;
using System.Collections.Generic;
using System.Net.Http;

namespace PegaTrade.Core.Data
{
    internal static class PegaRepository
    {
        private static readonly string baseUrl = StaticLogic.AppSettingsProvider.PegasunAPIUrl;

        public static ResultsItem SubscribeEmail(string email, Types.EmailPreferences emailPreferenceCode)
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("emailAddress", email),
                    new KeyValuePair<string, string>("emailPreferenceCode", ((int)emailPreferenceCode).ToString()),
                    new KeyValuePair<string, string>("platform", ((int)Types.Platform.PegaTrade).ToString())
                });

                HttpResponseMessage response = client.PostAsync($"{baseUrl}/v1/Account/SubscribeEmail", content).Result;
                return RepositoryHelper.GetSimpleHttpResponseResult(response, string.Empty);
            }
        }
    }
}
