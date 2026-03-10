using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using peeposredemption.Domain.Interfaces;
using System.Security.Claims;

namespace peeposredemption.API.Pages.App
{
    public class DirectMessageModel : PageModel
    {
        private readonly IUnitOfWork _uow;
        public DirectMessageModel(IUnitOfWork uow) => _uow = uow;

        public Guid RecipientId { get; set; }
        public string RecipientName { get; set; }
        public List<DmViewModel> Messages { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid recipientId)
        {
            var currentUserId = Guid.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            RecipientId = recipientId;

            var recipient = await _uow.Users.GetByIdAsync(recipientId);
            if (recipient == null) return NotFound();
            RecipientName = recipient.Username;

            var dms = await _uow.DirectMessages
                .GetConversationAsync(currentUserId, recipientId, 1, 50);

            Messages = dms.Select(dm => new DmViewModel
            {
                Content = dm.Content,
                SentAt = dm.SentAt,
                IsMine = dm.SenderId == currentUserId
            }).ToList();

            return Page();
        }
    }

    public class DmViewModel
    {
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }

}
