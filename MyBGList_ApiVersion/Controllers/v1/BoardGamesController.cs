using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MyBGList;
using MyBGList_ApiVersion.DTO.v1;

namespace MyBGList_ApiVersion.Controllers.v1
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("v/{version:apiversion}/[Controller]")]
	[EnableCors("AnyOrigin")]
	public class BoardGamesController : ControllerBase
	{
		private readonly ILogger<BoardGamesController> _logger;
		public BoardGamesController(ILogger<BoardGamesController> logger)
		{
			_logger = logger;
		}
		[HttpGet(Name = "GetBoardGames")]
		[ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
		public RestDTO<List<BoardGame>> Get()
		{
			var restDTO = new RestDTO<List<BoardGame>>();
			restDTO.Data = new List<BoardGame>
			{
				new BoardGame()
				{
					Id = 1,
					Name = "Axis & Allies",
					Year = 1981
				},
				new BoardGame()
				{
					Id = 2,
					Name = "Citadels",
					Year = 2000
				},
				new BoardGame()
				{
					Id = 3,
					Name = "Terraforming Mars",
					Year = 2016
				}
			};
			restDTO.Links = new List<LinkDTO>
			{
				new LinkDTO(Url.Action(null, "BoardGames", null, Request.Scheme), "self", "GET")
			};
			return restDTO;
		}
	}
}

