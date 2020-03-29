using System.Threading.Tasks;
using PegaTrade.Core.Data;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Core.StaticLogic
{
    public static class AuthorizationLogic
    {
        public static async Task<ResultsPair<PegaUser>> AuthorizeUser(string username, string password)
        {
            return await AuthorizationRepository.AuthorizeUser(username, password);
        }

        public static async Task<ResultsPair<PegaUser>> CreateNewUser(PegaUser user)
        {
            return await AuthorizationRepository.CreateNewUser(user);
        }
        
        public static ResultsItem RequestPasswordReset(string email)
        {
            return AuthorizationRepository.RequestPasswordReset(email);
        }

        public static async Task<ResultsItem> AuthorizeResetPassword(string username, string authCode)
        {
            return await AuthorizationRepository.AuthorizeResetPassword(username, authCode);
        }
        
        public static async Task<ResultsItem> ChangePassword(string username, string password)
        {
            return await AuthorizationRepository.ChangePassword(username, password);
        }

        public static async Task<ResultsItem> ConfirmEmail(string username, string authCode)
        {
            return await AuthorizationRepository.ConfirmEmail(username, authCode);
        }

        public static ResultsPair<ViewUser> AuthorizeViewUser(string username, string portfolioName)
        {
            return AuthorizationRepository.AuthorizeViewUser(username, portfolioName);
        }

        public static ResultsPair<PegaUser> ViewUserProfile(string username)
        {
            return AuthorizationRepository.ViewUserProfile(username);
        }

        public static Task<ResultsItem> DeletePegaUser(string username)
        {
            return AuthorizationRepository.DeletePegaUser(username);
        }
    }
}
