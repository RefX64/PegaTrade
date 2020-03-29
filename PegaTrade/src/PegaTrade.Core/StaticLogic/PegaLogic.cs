using PegaTrade.Core.Data;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Core.StaticLogic
{
    public static class PegaLogic
    {
        public static ResultsItem SubscribeEmail(string email, Types.EmailPreferences emailPreferenceCode)
        {
            return PegaRepository.SubscribeEmail(email, emailPreferenceCode);
        }
    }
}
