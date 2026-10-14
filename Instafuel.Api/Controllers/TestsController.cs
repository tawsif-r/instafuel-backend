using Instafuel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly PgDbContext _context;
    private readonly ILogger<TestsController> _logger;

    public TestsController(
        PgDbContext context,
        ILogger<TestsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Test>>> GetTests()
    {
        try
        {
            var tests = await _context.Tests.ToListAsync();
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                // ex,
                "Failed to retrieve test records from the database. Operation: {Operation}",
                nameof(GetTests));

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving test records." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Test>> CreateTest(Test test)
    {
        try
        {
            _context.Tests.Add(test);
            await _context.SaveChangesAsync();
            return Ok(test);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                // ex,
                "Failed to create a test record in the database. Operation: {Operation}",
                nameof(CreateTest));

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the test record." });
        }
    }
}