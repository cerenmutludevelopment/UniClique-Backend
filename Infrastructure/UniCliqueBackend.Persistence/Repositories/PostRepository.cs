using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Persistence.Contexts;

namespace UniCliqueBackend.Persistence.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly AppDbContext _context;

        public PostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(Guid id)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Event)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<IEnumerable<Post>> GetAllAsync()
        {
             return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Event)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => p.EventId == eventId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Posts
                .Include(p => p.Event)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Post post)
        {
            // Soft delete logic can be handled in Service or implicitly here if we set IsDeleted
            // Based on requirements, "Post delete" exists.
            post.IsDeleted = true; // Force soft delete here or just update
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Post>> GetFeedAsync(IEnumerable<Guid> friendIds)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Event)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => friendIds.Contains(p.UserId) && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PostLike?> GetLikeAsync(Guid postId, Guid userId)
        {
            return await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task AddLikeAsync(PostLike like)
        {
            await _context.PostLikes.AddAsync(like);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveLikeAsync(PostLike like)
        {
            _context.PostLikes.Remove(like);
            await _context.SaveChangesAsync();
        }

        public async Task AddCommentAsync(PostComment comment)
        {
            await _context.PostComments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCommentAsync(PostComment comment)
        {
            _context.PostComments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<PostComment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _context.PostComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        }

        public async Task<IEnumerable<PostComment>> GetCommentsByPostIdAsync(Guid postId)
        {
            return await _context.PostComments
                .Include(c => c.User)
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
