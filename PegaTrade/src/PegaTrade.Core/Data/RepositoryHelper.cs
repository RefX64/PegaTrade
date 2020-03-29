using PegaTrade.Core.Data.ServiceModels;
using PegaTrade.Layer.Language;
using PegaTrade.Layer.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using static PegaTrade.Layer.Models.Types;

namespace PegaTrade.Core.Data
{
    internal static class RepositoryHelper
    {
        /// <summary>
        /// Returns the API result from Pegasun API
        /// </summary>
        /// <param name="response">the HttpResponseMessage</param>
        /// <param name="successMessage">You can send a custom success message or leave it empty. If empty, will use APIResult's default message.</param>
        public static ResultsItem GetSimpleHttpResponseResult(HttpResponseMessage response, string successMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                string jsonString = response.Content.ReadAsStringAsync().Result;
                ResultsItem apiResult = NetJSON.NetJSON.Deserialize<PegasunAPIResult>(jsonString).ConvertToResultsItem();
                if (apiResult.ResultType == ResultsType.Successful)
                {
                    return ResultsItem.Success(string.IsNullOrEmpty(successMessage) ? apiResult.Message : successMessage);
                }

                return apiResult; // Error
            }

            return ResultsItem.Error(Lang.ServerConnectionError);
        }
    }
}
