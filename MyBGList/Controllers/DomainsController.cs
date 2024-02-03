using Microsoft.AspNetCore.Authorization;
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
	public class DomainsController : Controller
	{
		private readonly ApplicationDbContext _context;
		public DomainsController(ApplicationDbContext applicationDbContext)
		{
			_context = applicationDbContext;
		}
		[HttpGet(Name = "GetDomains")]
		public async Task<RestDTO<List<Domain>>> Get([FromQuery] RequestDTO<DomainDTO> input)
		{
			var domains = _context.Domains.AsQueryable();

			if (input.SortColumn != null)
				domains = domains.OrderBy($"{input.SortColumn} {input.SortOrder.ToUpper()}");
			if (!string.IsNullOrEmpty(input.FilterQuery))
				domains = domains.Where(d => d.Name.Contains(input.FilterQuery));

			domains = domains.Skip(input.PageIndex * input.PageSize).Take(input.PageSize);
			return new RestDTO<List<Domain>>()
			{
				Data = await domains.ToListAsync(),
				Links = new List<LinkDTO>()
				{
					new LinkDTO(
						Url.Action(null, "Domains", null, Request.Scheme),
						"self",
						"GET"
					)
				},
				PageIndex = input.PageIndex,
				PageSize = input.PageSize,
				RecordCount = await domains.CountAsync()

			};

		}
		[Authorize(Roles = UserRoles.Moderator)]
		[HttpPost(Name = "UpdateDomain")]
		public async Task<RestDTO<Domain>> Post(DomainDTO domainDTO)
		{
			var domain = await _context.Domains.SingleOrDefaultAsync(d => d.Id == domainDTO.Id);
			if (domain != null)
			{
				if (!string.IsNullOrEmpty(domainDTO.Name)) domain.Name = domainDTO.Name;
			}
			_context.Domains.Update(domain);
			await _context.SaveChangesAsync();
			return new RestDTO<Domain>()
			{
				Data = domain,
				Links = new List<LinkDTO>()
				{
					new LinkDTO(
						Url.Action(null, "Domains", null, Request.Scheme),
						"self",
						"POST"
					)
				}

			};
		}
		[Authorize(UserRoles.Adminstrator)]
		[HttpDelete(Name = "DeleteDomain")]
		public async Task<RestDTO<Domain>> Delete(DomainDTO domainDTO)
		{
			var domain = await _context.Domains.SingleOrDefaultAsync(d => d.Id == domainDTO.Id);
			if (domain != null)
			{
				_context.Domains.Remove(domain);
				await _context.SaveChangesAsync();
			}
			return new RestDTO<Domain>()
			{
				Data = domain,
				Links = new List<LinkDTO>()
				{
					new LinkDTO(
						Url.Action(null,"Domains",null,Request.Scheme),
						"self",
						"Delete"
						)
				}
			};
		}
	}
}
