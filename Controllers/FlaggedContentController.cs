using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlaggedContentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FlaggedContentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/flaggedcontent
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var flags = await _context.FlaggedContents
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return Ok(flags);
        }

        // POST: api/flaggedcontent
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlaggedContent flag)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            flag.CreatedAt = DateTime.UtcNow;
            flag.UpdatedAt = DateTime.UtcNow;

            _context.FlaggedContents.Add(flag);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = flag.Id }, flag);
        }

        // PATCH: api/flaggedcontent/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var flag = await _context.FlaggedContents.FindAsync(id);
            if (flag == null)
            {
                return NotFound();
            }

            flag.Status = dto.Status;
            flag.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(flag);
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
