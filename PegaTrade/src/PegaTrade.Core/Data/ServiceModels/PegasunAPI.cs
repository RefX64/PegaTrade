using System;
using System.Collections.Generic;
using System.Text;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Core.Data.ServiceModels
{
    public class PegasunAPIResult
    {
        public int resultType { get; set; }
        public string message { get; set; }

        public ResultsItem ConvertToResultsItem()
        {
            return ResultsItem.Create((Layer.Models.Types.ResultsType) resultType, message);
        }
    }

    public class PegasunAPIAuthorization
    {
        public int userId { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public bool subscribeNewsLetter { get; set; }
        public string fullName { get; set; }
        public bool emailConfirmed { get; set; }
        public PegasunAPIResult result { get; set; }
    }
}
