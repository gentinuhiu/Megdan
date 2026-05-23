using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Megdan.Web.Data;
using Megdan.Web.Helpers;
using Megdan.Web.Models;
using Megdan.Web.Models.Enums;
using Megdan.Web.Services;
using Megdan.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Megdan.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
    string? category,
    ComplaintPriority? priority,
    ComplaintStatus? status,
    string? citizenEmail,
    DateTime? fromDate,
    DateTime? toDate,
    string? sortBy,
    string? sortDirection)
        {
            var query = BuildFilteredComplaintQuery(category, priority, status, citizenEmail, fromDate, toDate);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => c.Category != null && c.Category.Contains(category));

            if (priority.HasValue)
                query = query.Where(c => c.Priority == priority.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(citizenEmail))
                query = query.Where(c => c.CitizenEmail.Contains(citizenEmail));

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(c => c.CreatedAtUtc >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(c => c.CreatedAtUtc < to);
            }

            // SORTING

            var direction = (sortDirection ?? "desc").ToLower();

            query = sortBy switch
            {
                "Priority" => direction == "asc"
                    ? query.OrderBy(c => c.Priority)
                    : query.OrderByDescending(c => c.Priority),

                "Status" => direction == "asc"
                    ? query.OrderBy(c => c.Status)
                    : query.OrderByDescending(c => c.Status),

                "Category" => direction == "asc"
                    ? query.OrderBy(c => c.Category)
                    : query.OrderByDescending(c => c.Category),

                "Comments" => direction == "asc"
                    ? query.OrderBy(c => c.Comments.Count)
                    : query.OrderByDescending(c => c.Comments.Count),

                _ => direction == "asc"
                    ? query.OrderBy(c => c.CreatedAtUtc)
                    : query.OrderByDescending(c => c.CreatedAtUtc)
            };

            var allComplaints = _context.Complaints.AsQueryable();

            var vm = new AdminComplaintListViewModel
            {
                Category = category,
                Priority = priority,
                Status = status,
                CitizenEmail = citizenEmail,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortDirection = sortDirection,

                TotalCount = await allComplaints.CountAsync(),
                PendingCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.Pending),
                InProgressCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.InProgress),
                ResolvedCount = await allComplaints.CountAsync(c => c.Status == ComplaintStatus.Resolved),
                UrgentCount = await allComplaints.CountAsync(c => c.Priority == ComplaintPriority.Urgent),

                Complaints = await query
                    .Select(c => new AdminComplaintListItemViewModel
                    {
                        Id = c.Id,
                        Title = c.Title,
                        CitizenEmail = c.CitizenEmail,
                        Category = c.Category,
                        Priority = c.Priority,
                        Status = c.Status,
                        CreatedAtUtc = c.CreatedAtUtc,
                        CommentCount = c.Comments.Count,
                        HasImages = c.Images.Any()
                    })
                    .ToListAsync()
            };

            var priorityGroups = await allComplaints
                .GroupBy(c => c.Priority)
                .Select(g => new { Name = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            vm.StatusLabelsJson = JsonSerializer.Serialize(new[] { "Pending", "In Progress", "Resolved" });
            vm.StatusValuesJson = JsonSerializer.Serialize(new[] { vm.PendingCount, vm.InProgressCount, vm.ResolvedCount });

            vm.PriorityLabelsJson = JsonSerializer.Serialize(priorityGroups.Select(x => x.Name));
            vm.PriorityValuesJson = JsonSerializer.Serialize(priorityGroups.Select(x => x.Count));

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Images)
                .Include(c => c.Comments)
                .Include(c => c.Feedback)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
                return NotFound();

            var vm = new AdminUpdateComplaintViewModel
            {
                Id = complaint.Id,
                Category = complaint.Category,
                Priority = complaint.Priority,
                Status = complaint.Status,
                Complaint = complaint
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View(new Megdan.Web.ViewModels.ImportComplaintsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(ImportComplaintsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var imported = 0;
            var errors = new List<string>();

            try
            {
                using var stream = model.CsvFile.OpenReadStream();
                using var reader = new StreamReader(stream);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                    IgnoreBlankLines = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                };

                using var csv = new CsvReader(reader, config);
                csv.Context.RegisterClassMap<ImportComplaintCsvRowMap>();

                var records = csv.GetRecords<ImportComplaintCsvRow>().ToList();

                var rowNumber = 1;

                foreach (var row in records)
                {
                    rowNumber++;

                    if (string.IsNullOrWhiteSpace(row.Title))
                    {
                        errors.Add($"Row {rowNumber}: Title is required.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(row.Description))
                    {
                        errors.Add($"Row {rowNumber}: Description is required.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(row.CitizenEmail))
                    {
                        errors.Add($"Row {rowNumber}: CitizenEmail is required.");
                        continue;
                    }

                    decimal? lat = null;
                    decimal? lng = null;

                    if (!string.IsNullOrWhiteSpace(row.Latitude))
                    {
                        if (decimal.TryParse(row.Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedLat))
                            lat = parsedLat;
                        else
                        {
                            errors.Add($"Row {rowNumber}: Latitude is invalid.");
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(row.Longitude))
                    {
                        if (decimal.TryParse(row.Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedLng))
                            lng = parsedLng;
                        else
                        {
                            errors.Add($"Row {rowNumber}: Longitude is invalid.");
                            continue;
                        }
                    }

                    var priority = Megdan.Web.Models.Enums.ComplaintPriority.Medium;
                    if (!string.IsNullOrWhiteSpace(row.Priority) &&
                        Enum.TryParse<Megdan.Web.Models.Enums.ComplaintPriority>(row.Priority, true, out var parsedPriority))
                    {
                        priority = parsedPriority;
                    }

                    var status = Megdan.Web.Models.Enums.ComplaintStatus.Pending;
                    if (!string.IsNullOrWhiteSpace(row.Status))
                    {
                        var normalizedStatus = row.Status.Replace(" ", "", StringComparison.OrdinalIgnoreCase);

                        if (Enum.TryParse<Megdan.Web.Models.Enums.ComplaintStatus>(normalizedStatus, true, out var parsedStatus))
                        {
                            status = parsedStatus;
                        }
                    }

                    var complaint = new Complaint
                    {
                        Title = row.Title.Trim(),
                        Description = row.Description.Trim(),
                        CitizenEmail = row.CitizenEmail.Trim(),
                        Address = string.IsNullOrWhiteSpace(row.Address) ? null : row.Address.Trim(),
                        Latitude = lat,
                        Longitude = lng,
                        Category = string.IsNullOrWhiteSpace(row.Category) ? null : row.Category.Trim(),
                        Priority = priority,
                        Status = status,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsAiProcessed = false
                    };

                    _context.Complaints.Add(complaint);
                    imported++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Import failed: {ex.Message}");
                return View(model);
            }

            if (errors.Any())
            {
                TempData["ImportMessage"] = $"Imported {imported} complaints. Some rows were skipped.";
                TempData["ImportErrors"] = string.Join(" || ", errors);
            }
            else
            {
                TempData["ImportMessage"] = $"Imported {imported} complaints successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(
    string? category,
    Megdan.Web.Models.Enums.ComplaintPriority? priority,
    Megdan.Web.Models.Enums.ComplaintStatus? status,
    string? citizenEmail,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var items = await BuildFilteredComplaintQuery(category, priority, status, citizenEmail, fromDate, toDate)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Title,Description,CitizenEmail,Category,Priority,Status,Address,Latitude,Longitude,CreatedAtUtc");

            foreach (var item in items)
            {
                sb.AppendLine($"\"{item.Id}\",\"{item.Title.Replace("\"", "\"\"")}\",\"{item.Description.Replace("\"", "\"\"")}\",\"{item.CitizenEmail}\",\"{item.Category}\",\"{item.Priority}\",\"{item.Status}\",\"{item.Address}\",\"{item.Latitude}\",\"{item.Longitude}\",\"{item.CreatedAtUtc:o}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"complaints-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(
    string? category,
    Megdan.Web.Models.Enums.ComplaintPriority? priority,
    Megdan.Web.Models.Enums.ComplaintStatus? status,
    string? citizenEmail,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var items = await BuildFilteredComplaintQuery(category, priority, status, citizenEmail, fromDate, toDate)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.CitizenEmail,
                    c.Category,
                    Priority = c.Priority.ToString(),
                    Status = c.Status.ToString(),
                    c.Address,
                    c.Latitude,
                    c.Longitude,
                    c.CreatedAtUtc
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Complaints");

            worksheet.Cell(1, 1).InsertTable(items);
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"complaints-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(AdminUpdateComplaintViewModel model)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (complaint == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.Complaint = await _context.Complaints
                    .Include(c => c.Images)
                    .Include(c => c.Comments)
                    .Include(c => c.Feedback)
                    .FirstAsync(c => c.Id == model.Id);

                return View("Details", model);
            }

            var previousPriority = complaint.Priority;
            var previousStatus = complaint.Status;

            complaint.Category = model.Category;
            complaint.Priority = model.Priority;
            complaint.Status = model.Status;
            complaint.UpdatedAtUtc = DateTime.UtcNow;

            if (previousStatus != ComplaintStatus.Resolved && model.Status == ComplaintStatus.Resolved)
                complaint.ResolvedAtUtc = DateTime.UtcNow;

            if (previousStatus == ComplaintStatus.Resolved && model.Status != ComplaintStatus.Resolved)
                complaint.ResolvedAtUtc = null;

            if (!string.IsNullOrWhiteSpace(model.AdminComment))
            {
                _context.ComplaintComments.Add(new ComplaintComment
                {
                    ComplaintId = complaint.Id,
                    Content = model.AdminComment.Trim(),
                    AuthorType = "Admin",
                    AuthorName = "Admin",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            if (previousStatus != complaint.Status)
            {
                await _notificationService.SendStatusChangedAsync(complaint);
            }

            if (previousPriority != ComplaintPriority.Urgent && complaint.Priority == ComplaintPriority.Urgent)
            {
                await _notificationService.SendUrgentAlertAsync(complaint);
            }

            return RedirectToAction(nameof(Details), new { id = complaint.Id });
        }

        /*NEW*/

        private static readonly TimeZoneInfo AppTimeZone =
    TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        private static DateTime ToUtcStart(DateTime localDate)
        {
            var local = DateTime.SpecifyKind(localDate.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, AppTimeZone);
        }

        private static DateTime ToUtcEndExclusive(DateTime localDate)
        {
            var localNextDay = DateTime.SpecifyKind(localDate.Date.AddDays(1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localNextDay, AppTimeZone);
        }

        private IQueryable<Complaint> BuildFilteredComplaintQuery(
            string? category,
            ComplaintPriority? priority,
            ComplaintStatus? status,
            string? citizenEmail,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.Complaints.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => c.Category != null && c.Category.Contains(category));

            if (priority.HasValue)
                query = query.Where(c => c.Priority == priority.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(citizenEmail))
                query = query.Where(c => c.CitizenEmail.Contains(citizenEmail));

            if (fromDate.HasValue)
            {
                var fromUtc = ToUtcStart(fromDate.Value);
                query = query.Where(c => c.CreatedAtUtc >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtcExclusive = ToUtcEndExclusive(toDate.Value);
                query = query.Where(c => c.CreatedAtUtc < toUtcExclusive);
            }

            return query;
        }
    }
}