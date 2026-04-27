using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniCliqueBackend.Application.DTOs.Post;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost("events/{eventId}/posts")]
        public async Task<IActionResult> CreatePost(Guid eventId, [FromBody] CreatePostDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (model.EventId == Guid.Empty) model.EventId = eventId;
            if (model.EventId != eventId) return BadRequest("Event ID mismatch.");

            var post = await _postService.CreatePostAsync(model, userId);
            if (post == null) return BadRequest("Failed to create post. User must be participant or owner.");

            return CreatedAtAction(nameof(GetPostsByEvent), new { eventId = eventId }, post);
        }

        [HttpDelete("posts/{id}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _postService.DeletePostAsync(id, userId);
            if (!result) return BadRequest("Failed to delete post or unauthorized.");

            return Ok("Post deleted successfully.");
        }

        [HttpGet("events/{eventId}/posts")]
        public async Task<IActionResult> GetPostsByEvent(Guid eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var posts = await _postService.GetPostsByEventIdAsync(eventId, userId);
            return Ok(posts);
        }

        [HttpGet("users/me/posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var posts = await _postService.GetMyPostsAsync(userId);
            return Ok(posts);
        }

        [HttpGet("users/{targetUserId}/posts")]
        public async Task<IActionResult> GetUserPosts(string targetUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var posts = await _postService.GetUserPostsAsync(targetUserId, currentUserId);
            return Ok(posts);
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var feed = await _postService.GetFeedAsync(userId);
            return Ok(feed);
        }

        [HttpPost("posts/{id}/like")]
        public async Task<IActionResult> ToggleLike(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _postService.ToggleLikeAsync(id, userId);
            return Ok(new { liked = result });
        }

        [HttpPost("posts/{id}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CreatePostCommentDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var comment = await _postService.AddCommentAsync(id, model, userId);
            if (comment == null) return BadRequest("Failed to add comment.");

            return Ok(comment);
        }

        [HttpGet("posts/{id}/comments")]
        public async Task<IActionResult> GetPostComments(Guid id)
        {
            var comments = await _postService.GetPostCommentsAsync(id);
            return Ok(comments);
        }

        [HttpDelete("posts/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _postService.DeleteCommentAsync(commentId, userId);
            if (!result) return BadRequest("Failed to delete comment or unauthorized.");

            return Ok("Comment deleted successfully.");
        }
    }
}
