using Ai_Fitness_Coach.Data;
using Ai_Fitness_Coach.DTOs;
using Ai_Fitness_Coach.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class MealGeneratorController : ControllerBase
{
    private readonly IMealService _aiService;
    private readonly ApplicationDbContext _context;

    public MealGeneratorController(IMealService aiService, ApplicationDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }
    [Authorize]
    [HttpGet("generate-plan")]
    public async Task<IActionResult> GetPlan()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return NotFound("User not found in database.");

            var plan = await _aiService.GenerateFullDayMealPlanAsync(user);

            return Ok(plan);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}