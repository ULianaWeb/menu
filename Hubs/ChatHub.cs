using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using menu.Data;
using menu.Models;
using menu.Areas.Identity.Data;

namespace menu.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<menuUser> _userManager;
        private readonly ILogger<ChatHub> _logger;

        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _connections =
            new ConcurrentDictionary<string, ConcurrentBag<string>>();

        public ChatHub(ApplicationDbContext db, UserManager<menuUser> userManager, ILogger<ChatHub> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        private string? GetCurrentUserId()
        {
            var userId = Context.UserIdentifier ?? Context.User?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            return userId;
        }

        public override Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var bag = _connections.GetOrAdd(userId, _ => new ConcurrentBag<string>());
                bag.Add(Context.ConnectionId);
                _logger.LogInformation("User connected: {UserId}, connection: {ConnectionId}", userId, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("OnConnectedAsync: could not determine user id for connection {ConnectionId}", Context.ConnectionId);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var bag))
            {
                var newBag = new ConcurrentBag<string>();
                foreach (var id in bag)
                {
                    if (id != Context.ConnectionId) newBag.Add(id);
                }
                _connections[userId] = newBag;
                _logger.LogInformation("User disconnected: {UserId}, connection: {ConnectionId}", userId, Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToAll(string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            var senderId = user?.Id;
            var senderEmail = user?.Email;

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                SenderEmail = senderEmail,
                ReceiverId = null,
                ReceiverEmail = null,
                MessageType = ChatMessageType.Text,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            var payload = new
            {
                chatMessage.Id,
                chatMessage.SenderId,
                chatMessage.SenderEmail,
                chatMessage.ReceiverId,
                chatMessage.ReceiverEmail,
                chatMessage.Message,
                chatMessage.MessageType,
                chatMessage.FileUrl,
                chatMessage.FileName,
                Timestamp = new DateTimeOffset(chatMessage.Timestamp).ToUnixTimeMilliseconds()
            };

            await Clients.All.SendAsync("ReceiveMessage", payload);
            _logger.LogInformation("Broadcast message from {SenderEmail} (id:{SenderId})", senderEmail, senderId);
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            var senderId = user?.Id;
            var senderEmail = user?.Email;

            string? receiverEmail = null;
            if (!string.IsNullOrEmpty(receiverId))
            {
                var receiverUser = await _userManager.FindByIdAsync(receiverId);
                receiverEmail = receiverUser?.Email;
            }

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                SenderEmail = senderEmail,
                ReceiverId = receiverId,
                ReceiverEmail = receiverEmail,
                MessageType = ChatMessageType.Text,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            var payload = new
            {
                chatMessage.Id,
                chatMessage.SenderId,
                chatMessage.SenderEmail,
                chatMessage.ReceiverId,
                chatMessage.ReceiverEmail,
                chatMessage.Message,
                chatMessage.MessageType,
                chatMessage.FileUrl,
                chatMessage.FileName,
                Timestamp = new DateTimeOffset(chatMessage.Timestamp).ToUnixTimeMilliseconds()
            };

            // Якщо надсилають собі — надсилаємо лише один раз (відправнику)
            if (!string.IsNullOrEmpty(receiverId) && receiverId == senderId)
            {
                if (!string.IsNullOrEmpty(senderId))
                {
                    await Clients.User(senderId).SendAsync("ReceivePrivateMessage", payload);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(receiverId))
                {
                    await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", payload);
                }

                if (!string.IsNullOrEmpty(senderId))
                {
                    await Clients.User(senderId).SendAsync("ReceivePrivateMessage", payload);
                }
            }

            _logger.LogInformation("Private message from {SenderEmail} to {ReceiverEmail}", senderEmail, receiverEmail ?? "(null)");
        }

        public async Task SendFileMessage(string? receiverId, string fileName, string fileUrl)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            var senderId = user?.Id;
            var senderEmail = user?.Email;

            string? receiverEmail = null;
            if (!string.IsNullOrEmpty(receiverId))
            {
                var receiverUser = await _userManager.FindByIdAsync(receiverId);
                receiverEmail = receiverUser?.Email;
            }

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                SenderEmail = senderEmail,
                ReceiverId = receiverId,
                ReceiverEmail = receiverEmail,
                MessageType = ChatMessageType.File,
                Message = null,
                FileName = fileName,
                FileUrl = fileUrl,
                Timestamp = DateTime.UtcNow
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            var payload = new
            {
                chatMessage.Id,
                chatMessage.SenderId,
                chatMessage.SenderEmail,
                chatMessage.ReceiverId,
                chatMessage.ReceiverEmail,
                chatMessage.Message,
                chatMessage.MessageType,
                chatMessage.FileUrl,
                chatMessage.FileName,
                Timestamp = new DateTimeOffset(chatMessage.Timestamp).ToUnixTimeMilliseconds()
            };

            if (string.IsNullOrEmpty(receiverId))
            {
                await Clients.All.SendAsync("ReceiveFileMessage", payload);
            }
            else
            {
                if (receiverId == senderId)
                {
                    if (!string.IsNullOrEmpty(senderId))
                        await Clients.User(senderId).SendAsync("ReceiveFileMessage", payload);
                }
                else
                {
                    await Clients.User(receiverId).SendAsync("ReceiveFileMessage", payload);
                    if (!string.IsNullOrEmpty(senderId))
                        await Clients.User(senderId).SendAsync("ReceiveFileMessage", payload);
                }
            }

            _logger.LogInformation("File message from {SenderEmail} (file: {FileName}) to {ReceiverEmail}", senderEmail, fileName, receiverEmail ?? "(all)");
        }

        public async Task GetRecentMessages(int count = 50)
        {
            var messages = await _db.ChatMessages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.SenderEmail,
                    m.ReceiverId,
                    m.ReceiverEmail,
                    m.Message,
                    m.MessageType,
                    m.FileUrl,
                    m.FileName,
                    Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                }).ToListAsync();

            await Clients.Caller.SendAsync("LoadHistory", messages);
            _logger.LogInformation("Loaded {Count} recent messages for connection {ConnectionId}", messages.Count, Context.ConnectionId);
        }

        // Повертає історію для конкретної розмови
        public async Task GetRecentMessagesForConversation(string? otherUserId, int count = 200)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            var userId = GetCurrentUserId();
            var isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            if (string.IsNullOrEmpty(otherUserId))
            {
                // public chat
                var publicMessages = await _db.ChatMessages
                    .Where(m => m.ReceiverId == null)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(count)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        m.SenderEmail,
                        m.ReceiverId,
                        m.ReceiverEmail,
                        m.Message,
                        m.MessageType,
                        m.FileUrl,
                        m.FileName,
                        Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                    }).ToListAsync();

                await Clients.Caller.SendAsync("LoadHistory", publicMessages);
                return;
            }

            if (isAdmin)
            {
                // Адмін бачить всі повідомлення, де otherUserId був відправником або отримувачем
                var adminMessages = await _db.ChatMessages
                    .Where(m => m.SenderId == otherUserId || m.ReceiverId == otherUserId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(count)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderId,
                        m.SenderEmail,
                        m.ReceiverId,
                        m.ReceiverEmail,
                        m.Message,
                        m.MessageType,
                        m.FileUrl,
                        m.FileName,
                        Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                    }).ToListAsync();

                await Clients.Caller.SendAsync("LoadHistory", adminMessages);
                _logger.LogInformation("Admin {User} loaded messages for {Other}", userId, otherUserId);
                return;
            }

            // Звичайний користувач: тільки приватні повідомлення між userId і otherUserId (обидві сторони)
            var convMessages = await _db.ChatMessages
                .Where(m =>
                    (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                    (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.SenderEmail,
                    m.ReceiverId,
                    m.ReceiverEmail,
                    m.Message,
                    m.MessageType,
                    m.FileUrl,
                    m.FileName,
                    Timestamp = new DateTimeOffset(m.Timestamp).ToUnixTimeMilliseconds()
                }).ToListAsync();

            await Clients.Caller.SendAsync("LoadHistory", convMessages);
            _logger.LogInformation("Loaded {Count} conversation messages for {UserId} with {OtherId}", convMessages.Count, userId, otherUserId);
        }
    }
}
