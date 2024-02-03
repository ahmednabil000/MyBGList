using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.Models;
using MyBGList.Models.Csv;
using System.Globalization;

namespace MyBGList.Controllers
{
	[Authorize(Roles = UserRoles.Adminstrator)]
	[Controller]
	[Route("[Controller]/[action]")]
	public class SeedController : Controller
	{
		private readonly ILogger<SeedController> _logger;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly UserManager<ApiUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		public SeedController(ILogger<SeedController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, RoleManager<IdentityRole> roleManager, UserManager<ApiUser> userManager)
		{
			_logger = logger;
			_context = context;
			_webHostEnvironment = webHostEnvironment;
			_roleManager = roleManager;
			_userManager = userManager;
		}

		[HttpPut]
		[ResponseCache(NoStore = true)]
		public async Task<IActionResult> BoardGames()
		{
			var config = new CsvConfiguration(CultureInfo.GetCultureInfo("pt-BR"))
			{
				HasHeaderRecord = true,
				Delimiter = ";",
			};
			using (var streamReader = new StreamReader(System.IO.Path.Combine(_webHostEnvironment.ContentRootPath, "Data/bgg_dataset.csv")))
			{
				using (var csvReader = new CsvReader(streamReader, config))
				{
					var existingBoardGames = await _context.BoardGames.ToDictionaryAsync(bg => bg.Id);
					var existingDomains = await _context.Domains.ToDictionaryAsync(d => d.Name);
					var existingMechanics = await _context.Mechanics.ToDictionaryAsync(m => m.Name);
					var now = DateTime.Now;

					var skippedRows = 0;
					var records = csvReader.GetRecords<BggRecord>();
					foreach (var record in records)
					{
						if (!record.ID.HasValue || string.IsNullOrEmpty(record.Name) || existingBoardGames.ContainsKey(record.ID.Value))
						{
							skippedRows++;
							continue;
						}
						var boardgame = new BoardGame()
						{
							Id = record.ID.Value,
							Name = record.Name,
							BGGRank = record.BGGRank ?? 0,
							ComplexityAverage = record.ComplexityAverage ?? 0,
							MaxPlayers = record.MaxPlayers ?? 0,
							MinAge = record.MinAge ?? 0,
							MinPlayers = record.MinPlayers ?? 0,
							OwnedUsers = record.OwnedUsers ?? 0,
							PlayTime = record.PlayTime ?? 0,
							RatingAverage = record.RatingAverage ?? 0,
							UsersRated = record.UsersRated ?? 0,
							Year = record.YearPublished ?? 0,
							CreatedDate = now,
							LastModifiedDate = now,
						};
						await _context.AddAsync(boardgame);

						if (!string.IsNullOrEmpty(record.Domains))
						{
							foreach (var domainName in record.Domains
								.Split(',', StringSplitOptions.TrimEntries)
								.Distinct(StringComparer.InvariantCultureIgnoreCase))
							{
								var domain = existingDomains.GetValueOrDefault(domainName);
								if (domain is null)
								{
									domain = new Domain()
									{
										Name = domainName,
										CreatedDate = now,
										LastModifiedDate = now
									};
									existingDomains.Add(domainName, domain);
									await _context.Domains.AddAsync(domain);
								}
								var boardGames_Domain = new BoardGames_Domains()
								{
									BoardGame = boardgame,
									Domain = domain,
									CreatedDate = now,
								};
								await _context.BoardGames_Domains.AddAsync(boardGames_Domain);
							}
						}
						if (!string.IsNullOrEmpty(record.Mechanics))
						{
							foreach (var mechannicName in record.Mechanics
								.Split(',', StringSplitOptions.TrimEntries)
								.Distinct(StringComparer.InvariantCultureIgnoreCase))
							{
								var mechanic = existingMechanics.GetValueOrDefault(mechannicName);
								if (mechanic is null)
								{
									mechanic = new Mechanic()
									{
										Name = mechannicName,
										CreatedDate = now,
										LastModifiedDate = now
									};
									await _context.Mechanics.AddAsync(mechanic);
									existingMechanics.Add(mechannicName, mechanic);
								}
								await _context.BoardGames_Mechanics.AddAsync(new BoardGames_Mechanics()
								{
									BoardGame = boardgame,
									Mechanic = mechanic,
									CreatedDate = now,
								});
							}
						}
					}
					// SAVE
					using var transaction = _context.Database.BeginTransaction();
					_context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT BoardGames ON");
					await _context.SaveChangesAsync();
					_context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT BoardGames OFF");
					transaction.Commit();
					return new JsonResult(new
					{
						BoardGames = _context.BoardGames.Count(),
						Domains = _context.Domains.Count(),
						Mechanics = _context.Mechanics.Count(),
						SkippedRows = skippedRows
					});
				}
			}
		}
		[HttpPost]
		public async Task<IActionResult> AuthData()
		{
			int rolesCreated = 0;
			int userAddedToRoles = 0;

			if (!await _roleManager.RoleExistsAsync(UserRoles.Adminstrator))
			{
				var resault = await _roleManager.CreateAsync(new IdentityRole(UserRoles.Adminstrator));
				if (resault.Succeeded)
				{
					_logger.LogInformation("{role} role has been created", UserRoles.Adminstrator);
					rolesCreated++;
				}
			}

			if (!await _roleManager.RoleExistsAsync(UserRoles.Moderator))
			{
				var resault = await _roleManager.CreateAsync(new IdentityRole(UserRoles.Moderator));
				if (resault.Succeeded)
				{
					_logger.LogInformation("{role} role has been created", UserRoles.Moderator);
					rolesCreated++;
				}
			}

			var testAdmin = await _userManager.FindByNameAsync("TestAdminstrator");
			var testModerator = await _userManager.FindByNameAsync("TestModerator");

			if (testAdmin != null && !await _userManager.IsInRoleAsync(testAdmin, UserRoles.Adminstrator))
			{
				var resault = await _userManager.AddToRoleAsync(testAdmin, UserRoles.Adminstrator);
				var result2 = await _userManager.AddToRoleAsync(testAdmin, UserRoles.Moderator);
				if (resault.Succeeded)
				{
					_logger.LogInformation("Role {role} is added succeessfully to user {user}", UserRoles.Adminstrator, testAdmin.UserName);
					userAddedToRoles++;
				}
				if (result2.Succeeded)
				{
					_logger.LogInformation("Role {role} is added succeessfully to user {user}", UserRoles.Moderator, testAdmin.UserName);
				}
			}
			if (testModerator != null && !await _userManager.IsInRoleAsync(testModerator, UserRoles.Moderator))
			{
				var resault = await _userManager.AddToRoleAsync(testModerator, UserRoles.Moderator);
				if (resault.Succeeded)
				{
					_logger.LogInformation("Role {role} is added succeessfully to user {user}", UserRoles.Moderator, testModerator.UserName);
					userAddedToRoles++;
				}
			}
			return new JsonResult(new
			{
				CreatedRules = rolesCreated,
				UserAddedToRoles = userAddedToRoles
			});
		}
	}
}
