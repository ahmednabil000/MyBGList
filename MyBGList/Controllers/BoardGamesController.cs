using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.Constants;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
	[ApiController]
	[Route("[Controller]")]
	[EnableCors("AnyOrigin")]
	[ResponseCache(CacheProfileName = "Any-60")]

	public class BoardGamesController : ControllerBase
	{
		private readonly ILogger<BoardGamesController> _logger;
		private readonly ApplicationDbContext _context;

		public BoardGamesController(ILogger<BoardGamesController> logger, ApplicationDbContext context)
		{
			_logger = logger;
			_context = context;

		}
		[HttpGet(Name = "GetBoardGames")]
		[Authorize]
		//[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
		public async Task<RestDTO<List<BoardGame>>> Get([FromQuery] RequestDTO<BoardGameDTO> requestDTO)
		{
			_logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "Get method started [{MachineName}] [{ThreadId}].", Environment.MachineName, Environment.CurrentManagedThreadId);

			IQueryable<BoardGame> query = _context.BoardGames.AsQueryable();



			if (requestDTO.SortColumn == null || requestDTO.SortColumn == "")
			{
				query = _context.BoardGames.Skip(requestDTO.PageIndex * requestDTO.PageSize).Take(requestDTO.PageSize);
			}
			else
			{
				if (requestDTO.SortOrder.ToLower() != "desc" && requestDTO.SortOrder.ToLower() != "asc") requestDTO.SortOrder = "asc";
				requestDTO.SortOrder = requestDTO.SortOrder.ToUpper();
				query = _context.BoardGames.Skip(requestDTO.PageIndex * requestDTO.PageSize).Take(requestDTO.PageSize).OrderBy($"{requestDTO.SortColumn} {requestDTO.SortOrder}");
			}
			if (!string.IsNullOrEmpty(requestDTO.FilterQuery))
				query = query.Where(bg => bg.Name.Contains(requestDTO.FilterQuery));


			var recordCount = await query.CountAsync();

			var restDTO = new RestDTO<List<BoardGame>>();

			return new RestDTO<List<BoardGame>>()
			{
				Data = await query.ToListAsync(),
				Links = new List<LinkDTO>
				{
					new LinkDTO(
						Url.Action(null, "BoardGames", null, Request.Scheme),
						"self",
						"GET"
					)
				},
				PageSize = requestDTO.PageSize,
				PageIndex = requestDTO.PageIndex,
				RecordCount = recordCount
			};
		}
		[Authorize(Roles = UserRoles.Moderator)]
		[HttpPost(Name = "UpdateBoardGame")]
		[ResponseCache(NoStore = true)]
		public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO boardGameDTO)
		{
			var boardGame = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == boardGameDTO.Id);
			if (boardGame != null)
			{
				if (!string.IsNullOrEmpty(boardGameDTO.Name))
					boardGame.Name = boardGameDTO.Name;
				if (boardGameDTO.Year.HasValue && boardGameDTO.Year > 0)
					boardGame.Year = boardGameDTO.Year.Value;
				boardGame.LastModifiedDate = DateTime.Now;
				_context.Update(boardGame);
				await _context.SaveChangesAsync();
			}
			return new RestDTO<BoardGame>()
			{
				Data = boardGame,
				Links = new List<LinkDTO>()
				{
					new LinkDTO(
						Url.Action(null, "BoardGames", boardGameDTO, Request.Scheme),
					"self",
					"POST"
					)
				}

			}!;
		}
		[Authorize(Roles = UserRoles.Adminstrator)]
		[HttpDelete(Name = "DeleteBoardGame")]
		[ResponseCache(NoStore = true)]
		public async Task<RestDTO<BoardGame?>> Deleate(int id)
		{
			var boardGame = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
			if (boardGame != null)
				_context.BoardGames.Remove(boardGame);
			await _context.SaveChangesAsync();
			return new RestDTO<BoardGame?>
			{
				Data = boardGame,
				Links = new List<LinkDTO>()
				{
					new LinkDTO(Url.Action(null,"BoardGames","id",Request.Scheme)!,
					"self",
					"DELETE"
					)
				}
			};
		}
	}
}

