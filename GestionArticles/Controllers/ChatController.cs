using System.Security.Claims;
using GestionArticles.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionArticles.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chat;             // bot client
        private readonly IAdminChatService _adminChat;   // bot admin / manager

        public ChatController(IChatService chat, IAdminChatService adminChat)
        {
            _chat = chat;
            _adminChat = adminChat;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("chat")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            // Le rôle est lu CÔTÉ SERVEUR : un client ne peut jamais atteindre le bot admin,
            // même en modifiant le JavaScript de la page.
            bool isAdmin = User.IsInRole("Admin");
            bool isManager = User.IsInRole("Manager");

            if (isAdmin || isManager)
                return Json(await _adminChat.AskAsync(req, isAdmin));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Json(await _chat.AskAsync(req, userId));
        }
    }
}
