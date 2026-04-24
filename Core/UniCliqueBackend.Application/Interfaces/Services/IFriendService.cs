using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Friendship;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IFriendService
    {
        Task<bool> SendFriendRequestAsync(string userId, string targetUserId);
        Task<bool> AcceptFriendRequestAsync(string userId, Guid requestId);
        Task<bool> RejectFriendRequestAsync(string userId, Guid requestId);
        Task<bool> CancelFriendRequestAsync(string userId, string targetUserId);
        Task<bool> RemoveFriendAsync(string userId, string friendId);
        
        Task<IEnumerable<FriendDto>> GetFriendsAsync(string userId);
        Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(string userId);
        Task<IEnumerable<FriendDto>> SearchUsersAsync(string query, string currentUserId);
    }
}
