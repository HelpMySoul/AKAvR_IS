using AKAVER_Server.Classes.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AKAVER_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthTestController : ControllerBase
    {
        [HttpGet("check-token")]
        [Authorize]
        public IActionResult CheckToken()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Username = User.FindFirst(ClaimTypes.Name)?.Value,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        [HttpGet("debug")]
        [AllowAnonymous]
        public IActionResult DebugInfo()
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            return Ok(new
            {
                AuthHeader = authHeader,
                HasBearer = authHeader.StartsWith("Bearer "),
                TokenLength = authHeader.Replace("Bearer ", "").Length
            });
        }
    }
}
