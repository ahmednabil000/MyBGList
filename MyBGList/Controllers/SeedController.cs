using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;
using MyBGList.Models.Csv;
using System.Globalization;

namespace MyBGList.Controllers
{
	[Controller]
	[Route("[Controller]")]
	public class SeedController : Controller
	{
		private readonly ILogger<SeedController> _logger;
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public SeedController(ILogger<SeedController> logger, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
		{
			_logger = logger;
			_context = context;
			_webHostEnvironment = webHostEnvironment;
		}
		[HttpPut]
		[ResponseCache(NoStore = true)]
		public async Task<IActionResult> Put()
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
	}
}
