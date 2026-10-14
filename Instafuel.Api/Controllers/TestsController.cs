using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly PgDbContext _context;

    public TestsController(PgDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Test>>> GetTests()
    {
        var tests = await _context.Tests.ToListAsync();
        return Ok(tests);
    }

    [HttpPost]
    public async Task<ActionResult<Test>> CreateTest(Test test)
    {
        _context.Tests.Add(test);
        await _context.SaveChangesAsync();
        return Ok(test);
    }
}