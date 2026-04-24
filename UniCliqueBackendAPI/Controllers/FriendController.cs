using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UniCliqueBackend.Application.Interfaces.Services;

namespace UniCliqueBackendAPI.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;

        public FriendController(IFriendService friendService)
        {
            _friendService = friendService;
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendFriendRequest(string targetUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.SendFriendRequestAsync(userId, targetUserId);
            if (!result) return BadRequest("Failed to send friend request. User might not exist or request already sent.");

            return Ok("Friend request sent successfully.");
        }

        [HttpDelete("request/cancel/{targetUserId}")]
        public async Task<IActionResult> CancelFriendRequest(string targetUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.CancelFriendRequestAsync(userId, targetUserId);
            if (!result) return BadRequest("Failed to cancel friend request.");

            return Ok("Friend request cancelled.");
        }

        [HttpPut("accept/{requestId}")]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.AcceptFriendRequestAsync(userId, requestId);
            if (!result) return BadRequest("Failed to accept request.");

            return Ok("Friend request accepted.");
        }

        [HttpPut("reject/{requestId}")]
        public async Task<IActionResult> RejectFriendRequest(Guid requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.RejectFriendRequestAsync(userId, requestId);
            if (!result) return BadRequest("Failed to reject request.");

            return Ok("Friend request rejected.");
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.RemoveFriendAsync(userId, friendId);
            if (!result) return BadRequest("Failed to remove friend.");

            return Ok("Friend removed successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var friends = await _friendService.GetFriendsAsync(userId);
            return Ok(friends);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var requests = await _friendService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query cannot be empty.");

            var results = await _friendService.SearchUsersAsync(query, userId);
            return Ok(results);
        }
    }
}
