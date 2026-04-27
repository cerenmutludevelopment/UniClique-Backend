using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(Guid id);
        Task<IEnumerable<Post>> GetAllAsync();
        Task<IEnumerable<Post>> GetByEventIdAsync(Guid eventId);
        Task<IEnumerable<Post>> GetByUserIdAsync(Guid userId);
        
        Task AddAsync(Post post);
        Task UpdateAsync(Post post);
        Task DeleteAsync(Post post);
        
        Task<IEnumerable<Post>> GetFeedAsync(IEnumerable<Guid> friendIds);

        // Interactions
        Task<PostLike?> GetLikeAsync(Guid postId, Guid userId);
        Task AddLikeAsync(PostLike like);
        Task RemoveLikeAsync(PostLike like);

        Task AddCommentAsync(PostComment comment);
        Task RemoveCommentAsync(PostComment comment);
        Task<PostComment?> GetCommentByIdAsync(Guid commentId);
        Task<IEnumerable<PostComment>> GetCommentsByPostIdAsync(Guid postId);
    }
}
