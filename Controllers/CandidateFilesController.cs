using api.cernahomecare.com.Data;
using CernaHomeCare.AdminApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CernaHomeCare.AdminApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidateFilesController : ControllerBase
{
    private readonly CernaHomeCareDbContext _context;

    public CandidateFilesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.CandidateFiles.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.CandidateFiles.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CandidateFile file)
    {
        file.UploadedUtc = DateTime.UtcNow;

        _context.CandidateFiles.Add(file);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = file.CandidateFileId }, file);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CandidateFile file)
    {
        if (id != file.CandidateFileId) return BadRequest();

        _context.Entry(file).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.CandidateFiles.FindAsync(id);
        if (item == null) return NotFound();

        _context.CandidateFiles.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}