// SmartPlanner.API/Controllers/LegacyController.cs
using Microsoft.AspNetCore.Mvc;

namespace SmartPlanner.API.Controllers
{
    [ApiController]
    public class LegacyController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult GetHome()
        {
            return Redirect("/swagger");
        }

        [HttpGet("/status")]
        public IActionResult GetStatus()
        {
            return Ok(new { 
                status = "API is running", 
                timestamp = DateTime.UtcNow,
                message = "Legacy endpoint - use /api endpoints instead"
            });
        }

        [HttpPost("/action")]
        public IActionResult HandleAction()
        {
            return Ok(new { 
                message = "Legacy endpoint deprecated", 
                new_endpoint = "POST /api/goals",
                timestamp = DateTime.UtcNow
            });
        }
    }
}