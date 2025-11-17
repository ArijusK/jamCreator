using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JamCreator.Shared.Interfaces;
using JamCreator.Shared.Models;

namespace JamCreator.Controllers
{
    [ApiController]
    [Route("api/participants")]
    public class ParticipantController : ControllerBase
    {
        private readonly IRepository<SessionParticipant, int> _participants;

        public ParticipantController(IRepository<SessionParticipant, int> participants)
        {
            _participants = participants;
        }

        // DELETE: api/participants/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _participants.DeleteByIdAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
