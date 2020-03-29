using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PegaTrade.Core.EntityFramework;
using LocalAccount = PegaTrade.Layer.Models.Account;
using LocalCommunity = PegaTrade.Layer.Models.Community;
using PegaTrade.Layer.Models;
using Mapster;
using PegaTrade.Layer.Models.Helpers;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PegaTrade.Core.StaticLogic.Helper;

namespace PegaTrade.Core.Data
{
    public static class CommunityRepository
    {
        // CurrentUser can be null
        public static async Task<List<LocalCommunity.BBThread>> GetBBThreads(int take, List<Types.ConvThreadCategory> categories, LocalAccount.PegaUser currentUser, int officialCoinId = 0)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    List<LocalCommunity.BBThread> threads = await GetBBThreadsFromDB(db, currentUser, take: take, categories: categories, officialCoindId: officialCoinId);
                    return threads;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"Category:{string.Join(",", categories)}" }, ex);
                return new List<LocalCommunity.BBThread>();
            }
        }

        public static async Task<LocalCommunity.BBThread> GetBBThreadWithComments(int threadId, LocalAccount.PegaUser currentUser)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var getThreadTask = GetBBThreadsFromDB(db, currentUser, threadId: threadId);
                    var getCommentsTask = GetBBThreadCommentsFromDB(db, threadId, currentUser);

                    LocalCommunity.BBThread thread = (await getThreadTask).FirstOrDefault();
                    List<LocalCommunity.BBComment> comments = await getCommentsTask;

                    if (thread == null) { return null; }

                    thread.ThreadComments = comments;
                    thread.ThreadName = "thread_" + thread.ThreadId;

                    return thread;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"ThreadId:{threadId}" }, ex);
                return new LocalCommunity.BBThread();
            }
        }

        public static async Task<ResultsItem> CreateBBThread(LocalCommunity.BBThread thread, LocalAccount.PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    thread.Message = thread.Message.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });
                    thread.UserId = user.UserId;

                    BBThreads serviceMessageThread = thread.Adapt<BBThreads>();
                    serviceMessageThread.CreateDate = DateTime.Now;
                    serviceMessageThread.UserId = user.UserId;
                    serviceMessageThread.Message = serviceMessageThread.Message.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });
                    serviceMessageThread.Title = serviceMessageThread.Title.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });

                    db.BBThreads.Add(serviceMessageThread);
                    await db.SaveChangesAsync();

                    return ResultsItem.Success("Successfully created a new thread");
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}" }, ex);
                return ResultsItem.Error($"Unable to create a new thread. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> DeleteBBThread(int threadId, LocalAccount.PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    BBThreads serviceThread = db.BBThreads.FirstOrDefault(x => x.ThreadId == threadId);
                    if (serviceThread == null) { return ResultsItem.Error("This thread was not found or has already been deleted."); }
                    if (serviceThread.UserId != user.UserId) { return ResultsItem.Error("This thread belongs to another user."); }

                    db.Remove(serviceThread);
                    await db.SaveChangesAsync();

                    return ResultsItem.Success("Successfully deleted this thread. Please refresh the page to see changes.");
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}" }, ex);
                return ResultsItem.Error($"Unable to delete this thread. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> CreateBBComment(LocalCommunity.BBComment comment, LocalAccount.PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    comment.Message.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });

                    BBComments serviceComment = comment.Adapt<BBComments>();
                    serviceComment.CreateDate = DateTime.Now;
                    serviceComment.UserId = user.UserId;
                    serviceComment.Message = serviceComment.Message.Clean(new[] { Types.CleanInputType.AZ09CommonCharsSM });
                    
                    db.BBComments.Add(serviceComment);
                    await db.SaveChangesAsync();

                    return ResultsItem.Success("Successfully posted a new comment.");
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}" }, ex);
                return ResultsItem.Error($"Unable to create a comment. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> DeleteBBComment(int commentId, LocalAccount.PegaUser user)
        {
            try
            {
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    BBComments serviceComment = db.BBComments.FirstOrDefault(x => x.CommentId == commentId);
                    if (serviceComment == null) { return ResultsItem.Error("This comment was not found or has already been deleted."); }
                    if (serviceComment.UserId != user.UserId) { return ResultsItem.Error("This comment belongs to another user."); }

                    db.Remove(serviceComment);
                    await db.SaveChangesAsync();

                    return ResultsItem.Success("Successfully deleted this comment. Please refresh the page to see changes.");
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}" }, ex);
                return ResultsItem.Error($"Unable to delete this comment. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> VoteBBThread(int threadId, bool isUpvote, LocalAccount.PegaUser user)
        {
            try
            {
                string message = "Successfully updated your vote. It will be reflected shortly.";
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var getServiceThreadTask = db.BBThreads.FirstOrDefaultAsync(x => x.ThreadId == threadId);
                    var getExistingVoteTask = db.BBThreadVotes.FirstOrDefaultAsync(x => x.ThreadId == threadId && x.UserId == user.UserId);

                    var serviceThread = await getServiceThreadTask;
                    var existingVote = await getExistingVoteTask;

                    if (serviceThread == null) { return ResultsItem.Error("This thread does not exist."); }
                    if (existingVote != null)
                    {
                        if (existingVote.IsUpvote == isUpvote) { db.Remove(existingVote); message = "Successfully removed your vote. It will be reflected shortly."; } // Same vote (e.g. clicked on upvote twice), which means they are re-tracing their vote.
                        else { existingVote.IsUpvote = isUpvote; }
                    }
                    else
                    {
                        var newServiceVote = new BBThreadVotes
                        {
                            ThreadId = threadId,
                            IsUpvote = isUpvote,
                            UserId = user.UserId
                        };
                        db.BBThreadVotes.Add(newServiceVote);
                    }

                    db.SaveChanges();
                    return ResultsItem.Success(message);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}", $"ThreadId:{threadId}" }, ex);
                return ResultsItem.Error($"Unable to submit your vote. {ex.Message}");
            }
        }

        public static async Task<ResultsItem> VoteBBComment(long commentId, bool isUpvote, LocalAccount.PegaUser user)
        {
            try
            {
                string message = "Successfully updated your vote. It will be reflected shortly.";
                using (PegasunDBContext db = new PegasunDBContext())
                {
                    var getServiceCommentTask = db.BBComments.FirstOrDefaultAsync(x => x.CommentId == commentId);
                    var getExistingVoteTask = db.BBCommentVotes.FirstOrDefaultAsync(x => x.CommentId == commentId && x.UserId == user.UserId);

                    var serviceComment = await getServiceCommentTask;
                    var existingVote = await getExistingVoteTask;

                    if (serviceComment == null) { return ResultsItem.Error("This comment does not exist."); }
                    if (existingVote != null)
                    {
                        if (existingVote.IsUpvote == isUpvote) { db.Remove(existingVote); message = "Successfully removed your vote. It will be reflected shortly."; } // Same vote (e.g. clicked on upvote twice), which means they are re-tracing their vote.
                        else { existingVote.IsUpvote = isUpvote; }
                    }
                    else
                    {
                        var newServiceVote = new BBCommentVotes
                        {
                            CommentId = commentId,
                            IsUpvote = isUpvote,
                            UserId = user.UserId
                        };
                        db.BBCommentVotes.Add(newServiceVote);
                    }

                    db.SaveChanges();
                    return ResultsItem.Success(message);
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { $"User:{user.Username}", $"CommentId:{commentId}" }, ex);
                return ResultsItem.Error($"Unable to submit your vote. {ex.Message}");
            }
        }

        private static async Task<List<LocalCommunity.BBThread>> GetBBThreadsFromDB(PegasunDBContext db, LocalAccount.PegaUser currentUser, int? take = null, int? threadId = null, 
                    List<Types.ConvThreadCategory> categories = null, int officialCoindId = 0)
        {
            if ((take == null || categories.IsNullOrEmpty()) && threadId == null) { throw new Exception("GetMessageThread: Not all parameters were passed in correctly."); }

            var query = db.BBThreads.OrderByDescending(x => x.CreateDate).Select(x => new
            {
                BBThread = x,
                User = new { x.User.UserId, x.User.Username, x.User.Email },
                VoteResult = new { TotalVotes = x.BBThreadVotes.Count, TotalUpvotes = x.BBThreadVotes.Count(v => v.IsUpvote) },
                TotalComments = x.BBComments.Count
            });

            var result = threadId.GetValueOrDefault() > 0 ? await query.Where(x => x.BBThread.ThreadId == threadId).ToListAsync()
                                                          : officialCoindId > 0 
                                                               ? await query.Where(x => x.BBThread.OfficialCoinId == officialCoindId).Take(take.GetValueOrDefault()).ToListAsync()
                                                               : await query.Where(x => categories.Any(c => (short)c == x.BBThread.CategoryCode)).Take(take.GetValueOrDefault()).ToListAsync();

            List<LocalCommunity.BBThread> threads = new List<LocalCommunity.BBThread>();
            result.ForEach(r =>
            {
                LocalCommunity.BBThread thread = r.BBThread.Adapt<LocalCommunity.BBThread>();
                thread.User = r.User.Adapt<LocalAccount.PegaUser>();
                thread.VoteResult = r.VoteResult.Adapt<LocalCommunity.VoteResult>();
                thread.TotalComments = r.TotalComments;
                thread.CurrentLoggedInUserID = (currentUser?.UserId).GetValueOrDefault();

                threads.Add(thread);
            });

            return threads;
        }
        
        private static async Task<List<LocalCommunity.BBComment>> GetBBThreadCommentsFromDB(PegasunDBContext db, int threadId, LocalAccount.PegaUser currentUser)
        {
            var query = db.BBComments.Select(x => new
            {
                BBComment = x,
                User = new { x.User.UserId, x.User.Username, x.User.Email },
                VoteResult = new { TotalVotes = x.BBCommentVotes.Count, TotalUpvotes = x.BBCommentVotes.Count(v => v.IsUpvote) }
            });

            var result = await query.Where(x => x.BBComment.ThreadId == threadId).ToListAsync();

            List<LocalCommunity.BBComment> comments = new List<LocalCommunity.BBComment>();
            result.ForEach(r =>
            {
                LocalCommunity.BBComment comment = r.BBComment.Adapt<LocalCommunity.BBComment>();
                comment.User = r.User.Adapt<LocalAccount.PegaUser>();
                comment.VoteResult = r.VoteResult.Adapt<LocalCommunity.VoteResult>();
                comment.CurrentLoggedInUserID = (currentUser?.UserId).GetValueOrDefault();

                comments.Add(comment);
            });

            return comments;
        }
    }
}
