using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Helpers;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Layer.Models.Community;
using PegaTrade.Layer.Models.Helpers;
using PegaTrade.ViewModel.Community;

namespace PegaTrade.Controllers
{
    [TypeFilter(typeof(ValidWriteUserOnly))]
    public class CommunityController : BaseController
    {
        public CommunityController(IMemoryCache cache, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _memoryCache = cache;
            _hostingEnvironment = env;
        }

        public async Task<PartialViewResult> GetConversationThreads(string threadName, Types.ConvThreadCategory category, int officialCoinId = 0, int take = 50)
        { 
            ConversationsVM vm = new ConversationsVM
            {
                CurrentThread = new BBThread
                {
                    ThreadName = threadName,
                    CategoryCode = category
                },
                Threads = await GetThreadsByCategory(take, category, officialCoinId),
                CurrentUser = CurrentUser
            };

            return PartialView("_Conversations", vm);
        }

        public async Task<PartialViewResult> GetAllConversationThreads(int take = 50)
        {
            ConversationsVM vm = new ConversationsVM
            {
                Threads = await CommunityLogic.GetMessageThreads(take, new List<Types.ConvThreadCategory> { Types.ConvThreadCategory.MainDashboard,
                                Types.ConvThreadCategory.OfficialCoins, Types.ConvThreadCategory.FeatureRequests  }, CurrentUser),
                CurrentUser = CurrentUser,
                HideCreateNewPost = true
            };

            List<OfficialCoin> officialCoins = CryptoLogic.GetAllOfficialCoins();
            foreach (var x in vm.Threads.Where(x => x.OfficialCoinId > 0))
            {
                OfficialCoin officialCoin = officialCoins.FirstOrDefault(o => o.OfficialCoinId == x.OfficialCoinId);
                if (officialCoin == null) { continue; }

                x.OfficialCoin = officialCoin;
                x.ShowOfficialCoinNameOnThread = true;
            }

            return PartialView("_Conversations", vm);
        }

        public async Task<PartialViewResult> GetThreadsWithComments(int threadId)
        {
            // Aka: View a single thread with comments. 
            ConversationsVM vm = new ConversationsVM
            {
                CurrentThread = await GetThreadWithComments(threadId),
                CurrentUser = CurrentUser,
                IsCreateCommentMode = true
            };

            return PartialView("_ThreadCommentsView", vm);
        }

        #region CRUD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ResultsItem> CreateBBThread(BBThread thread)
        {
            if (!ModelState.IsValid) { return ResultsItem.Error(ModelState.GetAllErrorsString()); }

            OnThreadUpdatedCacheHandler(thread.CategoryCode, thread.OfficialCoinId.GetValueOrDefault());
            return await CommunityLogic.CreateBBThread(thread, CurrentUser);
        }

        [HttpPost]
        public async Task<ResultsItem> DeleteBBThread(int threadId)
        {
            OnThreadUpdatedCacheHandler(threadId: threadId);
            return await CommunityLogic.DeleteBBThread(threadId, CurrentUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ResultsItem> CreateBBComment(BBComment comment)
        {
            if (!ModelState.IsValid) { return ResultsItem.Error(ModelState.GetAllErrorsString()); }

            OnThreadUpdatedCacheHandler(threadId: comment.ThreadId);
            OnCommentsUpdatedCacheHandler(comment.ThreadId);
            return await CommunityLogic.CreateBBComment(comment, CurrentUser);
        }

        [HttpPost]
        public async Task<ResultsItem> DeleteBBComment(int commentId)
        {
            OnCommentsUpdatedCacheHandler(commentId: commentId);
            return await CommunityLogic.DeleteBBComment(commentId, CurrentUser);
        }

        [HttpPost]
        public async Task<ResultsItem> VoteBBThread(int threadId, bool isUpvote)
        {
            OnThreadUpdatedCacheHandler(threadId: threadId);
            return await CommunityLogic.VoteBBThread(threadId, isUpvote, CurrentUser);
        }

        [HttpPost]
        public async Task<ResultsItem> VoteBBComment(int commentId, bool isUpvote)
        {
            OnCommentsUpdatedCacheHandler(commentId: commentId);
            return await CommunityLogic.VoteBBComment(commentId, isUpvote, CurrentUser);
        }
        #endregion

        public PartialViewResult ConnectWithUs()
        {
            return PartialView("_ConnectWithUs");
        }
    }
}