using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Post;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IFriendshipRepository _friendshipRepository;

        public PostService(
            IPostRepository postRepository,
            IUserRepository userRepository,
            IEventRepository eventRepository,
            IFriendshipRepository friendshipRepository)
        {
            _postRepository = postRepository;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _friendshipRepository = friendshipRepository;
        }

        public async Task<PostDto?> CreatePostAsync(CreatePostDto model, string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return null;

            var user = await _userRepository.GetByIdAsync(uid);
            if (user == null) return null;

            var evt = await _eventRepository.GetByIdAsync(model.EventId);
            if (evt == null) return null;
            
            var participant = await _eventRepository.GetParticipantAsync(model.EventId, uid);
            if (participant == null && evt.OwnerId != uid) return null; 

            var post = new Post
            {
                UserId = uid,
                EventId = model.EventId,
                Content = model.Content,
                PhotoUrl = model.PhotoUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _postRepository.AddAsync(post);

            user.InteractionScore += 5; 
            await _userRepository.UpdateAsync(user);

            return MapToDto(post, userId);
        }

        public async Task<bool> DeletePostAsync(Guid postId, string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return false;

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) return false;

            if (post.UserId != uid) return false;

            await _postRepository.DeleteAsync(post);
            return true;
        }

        public async Task<IEnumerable<PostDto>> GetPostsByEventIdAsync(Guid eventId, string currentUserId)
        {
            var posts = await _postRepository.GetByEventIdAsync(eventId);
            return posts.Select(p => MapToDto(p, currentUserId));
        }

        public async Task<IEnumerable<PostDto>> GetMyPostsAsync(string userId)
        {
             if (!Guid.TryParse(userId, out var uid)) return Enumerable.Empty<PostDto>();
             var posts = await _postRepository.GetByUserIdAsync(uid);
             return posts.Select(p => MapToDto(p, userId));
        }
        
        public async Task<IEnumerable<PostDto>> GetUserPostsAsync(string userId, string currentUserId)
        {
             if (!Guid.TryParse(userId, out var uid)) return Enumerable.Empty<PostDto>();
             var posts = await _postRepository.GetByUserIdAsync(uid);
             return posts.Select(p => MapToDto(p, currentUserId));
        }

        public async Task<IEnumerable<PostDto>> GetFeedAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return Enumerable.Empty<PostDto>();

            var friends = await _friendshipRepository.GetFriendsAsync(uid);
            var friendIds = friends.Select(f => f.Id).ToList();
            
            if (!friendIds.Any()) return Enumerable.Empty<PostDto>();

            var posts = await _postRepository.GetFeedAsync(friendIds);
            return posts.Select(p => MapToDto(p, userId));
        }

        public async Task<bool> ToggleLikeAsync(Guid postId, string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return false;

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) return false;

            var existingLike = await _postRepository.GetLikeAsync(postId, uid);
            if (existingLike != null)
            {
                await _postRepository.RemoveLikeAsync(existingLike);
                return false; // unliked
            }
            else
            {
                var newLike = new PostLike
                {
                    PostId = postId,
                    UserId = uid
                };
                await _postRepository.AddLikeAsync(newLike);
                
                // Gamification: Give points to the post owner for getting a like
                if (post.UserId != uid)
                {
                    var postOwner = await _userRepository.GetByIdAsync(post.UserId);
                    if (postOwner != null)
                    {
                        postOwner.InteractionScore += 2;
                        await _userRepository.UpdateAsync(postOwner);
                    }
                }

                return true; // liked
            }
        }

        public async Task<PostCommentDto?> AddCommentAsync(Guid postId, CreatePostCommentDto model, string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return null;

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) return null;

            var user = await _userRepository.GetByIdAsync(uid);
            if (user == null) return null;

            var comment = new PostComment
            {
                PostId = postId,
                UserId = uid,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            await _postRepository.AddCommentAsync(comment);

            // Gamification: Give points
            user.InteractionScore += 3;
            await _userRepository.UpdateAsync(user);
            
            if (post.UserId != uid)
            {
                var postOwner = await _userRepository.GetByIdAsync(post.UserId);
                if (postOwner != null)
                {
                    postOwner.InteractionScore += 3;
                    await _userRepository.UpdateAsync(postOwner);
                }
            }

            return new PostCommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                UserId = comment.UserId,
                UserName = user.FullName,
                UserProfilePhoto = user.ProfilePhotoUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId, string userId)
        {
            if (!Guid.TryParse(userId, out var uid)) return false;

            var comment = await _postRepository.GetCommentByIdAsync(commentId);
            if (comment == null) return false;

            if (comment.UserId != uid) return false; // Only comment owner can delete

            await _postRepository.RemoveCommentAsync(comment);
            return true;
        }

        public async Task<IEnumerable<PostCommentDto>> GetPostCommentsAsync(Guid postId)
        {
            var comments = await _postRepository.GetCommentsByPostIdAsync(postId);
            return comments.Select(c => new PostCommentDto
            {
                Id = c.Id,
                PostId = c.PostId,
                UserId = c.UserId,
                UserName = c.User != null ? c.User.FullName : "Unknown",
                UserProfilePhoto = c.User != null ? c.User.ProfilePhotoUrl : null,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            });
        }
        
        private PostDto MapToDto(Post post, string currentUserId)
        {
            bool isLiked = false;
            if (Guid.TryParse(currentUserId, out var uid))
            {
                isLiked = post.Likes != null && post.Likes.Any(l => l.UserId == uid);
            }

            return new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                UserName = post.User != null ? post.User.FullName : "Unknown",
                UserProfilePhoto = post.User != null ? post.User.ProfilePhotoUrl : null,
                EventId = post.EventId,
                EventTitle = post.Event != null ? post.Event.Title : "Unknown Event",
                Content = post.Content,
                PhotoUrl = post.PhotoUrl,
                CreatedAt = post.CreatedAt,
                LikeCount = post.Likes?.Count ?? 0,
                CommentCount = post.Comments?.Count ?? 0,
                IsLiked = isLiked
            };
        }
    }
}
