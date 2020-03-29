using PegaTrade.Core.Data;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Community;
using PegaTrade.Layer.Models.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Core.StaticLogic
{
    public static class CommunityLogic
    {
        public static async Task<List<BBThread>> GetMessageThreads(int take, Types.ConvThreadCategory category, PegaUser user, int officialCoinId = 0)
        {
            return await GetMessageThreads(take, new List<Types.ConvThreadCategory> { category }, user, officialCoinId);
        }
        public static async Task<List<BBThread>> GetMessageThreads(int take, List<Types.ConvThreadCategory> categories, PegaUser currentUser, int officialCoinId = 0)
        {
            List<BBThread> threads = await CommunityRepository.GetBBThreads(take, categories, currentUser, officialCoinId);
            return threads;
        }

        public static async Task<BBThread> GetThreadWithComments(int threadId, PegaUser user)
        {
            BBThread thread = await CommunityRepository.GetBBThreadWithComments(threadId, user);
            return thread;
        }

        public static async Task<ResultsItem> CreateBBThread(BBThread thread, PegaUser user)
        {
            return await CommunityRepository.CreateBBThread(thread, user);
        }

        public static async Task<ResultsItem> DeleteBBThread(int threadId, PegaUser user)
        {
            return await CommunityRepository.DeleteBBThread(threadId, user);
        }

        public static async Task<ResultsItem> CreateBBComment(BBComment comment, PegaUser user)
        {
            return await CommunityRepository.CreateBBComment(comment, user);
        }

        public static async Task<ResultsItem> DeleteBBComment(int commentId, PegaUser user)
        {
            return await CommunityRepository.DeleteBBComment(commentId, user);
        }

        public static async Task<ResultsItem> VoteBBThread(int threadId, bool isUpvote, PegaUser user)
        {
            return await CommunityRepository.VoteBBThread(threadId, isUpvote, user);
        }

        public static async Task<ResultsItem> VoteBBComment(int commentId, bool isUpvote, PegaUser user)
        {
            return await CommunityRepository.VoteBBComment(commentId, isUpvote, user);
        }
    }
}
