using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBGList.DTO;
using MyBGList.Models;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
	[ApiController]
	[Route("[Controller]")]
	public class MechanicsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public MechanicsController(ApplicationDbContext context)
		{
			_context = context;
		}
		[HttpGet(Name = "GetMechanics")]
		public async Task<RestDTO<List<Mechanic>>> Get([FromQuery] RequestDTO<MechanicDTO> input)
		{
			var mechanics = _context.Mechanics.AsQueryable();

			if (input.SortColumn != null)
			{
				mechanics = mechanics.OrderBy($"{input.SortColumn} {input.SortOrder.ToUpper()}");
			}
			if (input.FilterQuery != null)
			{
				mechanics = mechanics.Where(m => m.Name.Contains(input.FilterQuery));
			}
			mechanics = mechanics.Skip(input.PageIndex * input.PageSize).Take(input.PageSize);

			return new RestDTO<List<Mechanic>>
			{
				Data = await mechanics.ToListAsync(),
				Links = new List<LinkDTO>()
				{
					new LinkDTO(
						Url.Action(null,"Mechanics",null,Request.Scheme),
						"self",
						"GET")
				},
				PageIndex = input.PageIndex,
				PageSize = input.PageSize
			};
		}
	}
}
