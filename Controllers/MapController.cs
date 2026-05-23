using Megdan.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Megdan.Web.Controllers
{
    public class MapController : Controller
    {
        private readonly AppDbContext _context;

        public MapController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetComplaintMarkers()
        {
            var data = await _context.Complaints
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Category,
                    c.Status,
                    c.Priority,
                    c.Latitude,
                    c.Longitude,
                    c.Address
                })
                .ToListAsync();

            return Json(data);
        }
    }
}