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
            if (!result) return BadRequest("Friend request failed. Possible reasons: 1) The user does not exist. 2) A friend request has already been sent. 3) You are already friends with this user. 4) You cannot send a friend request to yourself.");

            return Ok("Friend request successfully sent to the target user.");
        }

        [HttpDelete("request/cancel/{targetUserId}")]
        public async Task<IActionResult> CancelFriendRequest(string targetUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.CancelFriendRequestAsync(userId, targetUserId);
            if (!result) return BadRequest("Failed to cancel the friend request. Make sure the request is still pending and it was sent by you.");

            return Ok("Your pending friend request has been successfully cancelled.");
        }

        [HttpPut("accept/{requestId}")]
        public async Task<IActionResult> AcceptFriendRequest(Guid requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.AcceptFriendRequestAsync(userId, requestId);
            if (!result) return BadRequest("Failed to accept the friend request. The request ID might be invalid, it might have already been accepted/rejected, or you are not the receiver of this request.");

            return Ok("Friend request accepted. You are now friends!");
        }

        [HttpPut("reject/{requestId}")]
        public async Task<IActionResult> RejectFriendRequest(Guid requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.RejectFriendRequestAsync(userId, requestId);
            if (!result) return BadRequest("Failed to reject the friend request. The request ID might be invalid or you are not the receiver of this request.");

            return Ok("Friend request rejected successfully.");
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _friendService.RemoveFriendAsync(userId, friendId);
            if (!result) return BadRequest("Failed to remove friend. Make sure you are actually friends with this user and the friend ID is valid.");

            return Ok("Friend successfully removed from your friends list.");
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
