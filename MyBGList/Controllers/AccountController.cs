using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyBGList.DTO;
using MyBGList.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MyBGList.Controllers
{
	[Route("[controller]/[action]")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<AccountController> _logger;
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApiUser> _userManager;
		private readonly SignInManager<ApiUser> _signInManager;
		public AccountController(IConfiguration configuration, ILogger<AccountController> logger, ApplicationDbContext context, UserManager<ApiUser> userManager, SignInManager<ApiUser> signInManager)
		{
			_configuration = configuration;
			_logger = logger;
			_context = context;
			_userManager = userManager;
			_signInManager = signInManager;
		}
		[HttpPost]
		[ResponseCache(NoStore = true)]
		public async Task<IActionResult> Register(RegisterDTO input)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var details = new ValidationProblemDetails(ModelState);
					details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
					details.Status = StatusCodes.Status400BadRequest;
					return BadRequest(details);
				}
				var newUser = new ApiUser()
				{
					UserName = input.UserName,
					Email = input.Email
				};

				var result = await _userManager.CreateAsync(newUser, input.Password);
				if (result.Succeeded)
				{
					_logger.LogInformation("User {UserName} {Email} has been succesfully created", input.UserName, input.Email);
					return StatusCode(201, $"User {input.UserName} has been created");
				}
				else
				{
					throw new Exception(string.Format("Error{0}: ", string.Join(" ", result.Errors.Select(e => e.Description))));
				}
			}
			catch (Exception ex)
			{
				var exceptionDetails = new ProblemDetails();
				exceptionDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
				exceptionDetails.Detail = ex.Message;
				exceptionDetails.Status = StatusCodes.Status500InternalServerError;

				return StatusCode(StatusCodes.Status500InternalServerError, exceptionDetails);
			}


		}
		[HttpPost]
		[ResponseCache(NoStore = true)]
		public async Task<IActionResult> Login(LoginDTO input)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var details = new ValidationProblemDetails(ModelState);
					details.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
					details.Status = StatusCodes.Status400BadRequest;
					return StatusCode(StatusCodes.Status400BadRequest, details);
				}
				else
				{
					var user = await _userManager.FindByEmailAsync(input.Email);
					if (user == null || !await _userManager.CheckPasswordAsync(user, input.Password))
					{
						throw new Exception("Invalid login attempt");
					}
					else
					{

						var signingCredentials = new SigningCredentials(
							new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"]))
						 , SecurityAlgorithms.HmacSha256);

						var claims = new List<Claim>()
						{
							new Claim(ClaimTypes.Name,input.Email)
						};

						var userRoles = await _userManager.GetRolesAsync(user);
						var userRolesClaims = userRoles.Select(r => new Claim(ClaimTypes.Role, r));
						claims.AddRange(userRolesClaims);

						var jwtObject = new JwtSecurityToken(
							issuer: _configuration["JWT:Issuer"],
							audience: _configuration["JWT:Audience"],
							claims: claims,
							expires: DateTime.UtcNow.AddSeconds(300),
							signingCredentials: signingCredentials
							);

						var jwtString = new JwtSecurityTokenHandler().WriteToken(jwtObject);
						HttpContext.Response.Headers["Authorization"] = jwtString;

						return StatusCode(StatusCodes.Status200OK, jwtString);
					}
				}
			}
			catch (Exception ex)
			{
				var exceptionDetails = new ProblemDetails();
				exceptionDetails.Status = StatusCodes.Status401Unauthorized;
				exceptionDetails.Detail = ex.Message;
				exceptionDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

				return StatusCode(StatusCodes.Status401Unauthorized, exceptionDetails);
			}
		}
	}
}
