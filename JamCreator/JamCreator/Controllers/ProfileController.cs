using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamCreator.Data;                      // your AppDbContext namespace
using JamCreator.Shared.Models;
using JamCreator.Shared.Models.DTOs;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProfileController(AppDbContext db) => _db = db;

    // GET: api/profile/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfileDto>> Get(string id, CancellationToken ct)
    {
        var p = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();

        return new UserProfileDto {
            Id = p.Id, Username = p.Username, FavoriteGenre = p.FavoriteGenre,
            Avatar = p.Avatar, UpdatedAtUtc = p.UpdatedAtUtc
        };
    }

    // PUT: api/profile/{id} (upsert)
    [HttpPut("{id}")]
    public async Task<ActionResult<UserProfileDto>> Upsert(string id, [FromBody] UserProfileDto dto, CancellationToken ct)
    {
        var entity = await _db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            entity = new UserProfile {
                Id = id,
                Username = dto.Username?.Trim() ?? "",
                FavoriteGenre = dto.FavoriteGenre?.Trim() ?? "",
                Avatar = string.IsNullOrWhiteSpace(dto.Avatar) ? "🎸" : dto.Avatar.Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.UserProfiles.Add(entity);
        }
        else
        {
            entity.Username = dto.Username?.Trim() ?? "";
            entity.FavoriteGenre = dto.FavoriteGenre?.Trim() ?? "";
            entity.Avatar = string.IsNullOrWhiteSpace(dto.Avatar) ? "🎸" : dto.Avatar.Trim();
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new UserProfileDto {
            Id = entity.Id, Username = entity.Username, FavoriteGenre = entity.FavoriteGenre,
            Avatar = entity.Avatar, UpdatedAtUtc = entity.UpdatedAtUtc
        });
    }

    // DELETE: api/profile/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var entity = await _db.UserProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        _db.UserProfiles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
