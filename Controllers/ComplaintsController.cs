using Megdan.Web.Data;
using Megdan.Web.Models;
using Megdan.Web.Models.Enums;
using Megdan.Web.Models.ViewModels;
using Megdan.Web.Services;
using Megdan.Web.Services.Configuration;
using Megdan.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Megdan.Web.Controllers
{
    public class ComplaintsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;
        private readonly IAiAnalysisService _aiAnalysisService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly RecaptchaSettings _recaptchaSettings;
        private readonly IEmailAccessService _emailAccessService;
        private readonly IComplaintSimilarityService _complaintSimilarityService;

        public ComplaintsController(
    AppDbContext context,
    IFileStorageService fileStorageService,
    INotificationService notificationService,
    IAiAnalysisService aiAnalysisService,
    IRecaptchaService recaptchaService,
    IOptions<RecaptchaSettings> recaptchaOptions,
    IEmailAccessService emailAccessService,
       IComplaintSimilarityService complaintSimilarityService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
            _aiAnalysisService = aiAnalysisService;
            _recaptchaService = recaptchaService;
            _recaptchaSettings = recaptchaOptions.Value;
            _emailAccessService = emailAccessService;
            _complaintSimilarityService = complaintSimilarityService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SubmitComplaintViewModel
            {
                RecaptchaSiteKey = _recaptchaSettings.SiteKey
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmitComplaintViewModel model)
        {
            model.Images ??= new List<IFormFile>();

            if (!ModelState.IsValid)
                return View(model);

            if (model.Images.Count > 5)
            {
                ModelState.AddModelError(nameof(model.Images), "You can upload up to 5 images.");
                return View(model);
            }

            foreach (var image in model.Images)
            {
                if (image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.Images), "Each image must be 5 MB or smaller.");
                    return View(model);
                }
            }

            var recaptchaResult = await _recaptchaService.VerifyAsync(model.RecaptchaToken, "submit_complaint");
            if (!recaptchaResult.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, recaptchaResult.ErrorMessage);
                model.RecaptchaSiteKey = _recaptchaSettings.SiteKey;
                return View(model);
            }

            try
            {
                var complaint = new Complaint
                {
                    Title = model.Title,
                    Description = model.Description,
                    CitizenEmail = model.CitizenEmail,
                    Address = model.Address,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Status = ComplaintStatus.Pending,
                    Priority = ComplaintPriority.Medium,
                    IsAiProcessed = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                var savedImages = await _fileStorageService.SaveComplaintImagesAsync(model.Images);
                foreach (var file in savedImages)
                {
                    _context.ComplaintImages.Add(new ComplaintImage
                    {
                        ComplaintId = complaint.Id,
                        FileName = file.FileName,
                        FilePath = file.FilePath,
                        ContentType = file.ContentType,
                        FileSizeBytes = file.FileSizeBytes
                    });
                }

                var trackingToken = new TrackingToken
                {
                    ComplaintId = complaint.Id,
                    Token = Guid.NewGuid().ToString("N"),
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
                };
                _context.TrackingTokens.Add(trackingToken);

                var aiResult = await _aiAnalysisService.AnalyzeComplaintAsync(model.Title, model.Description);
                complaint.Category = aiResult.Category;
                complaint.FrustrationLevel = aiResult.FrustrationLevel;
                complaint.IsAiProcessed = aiResult.Success;

                if (Enum.TryParse<ComplaintPriority>(aiResult.Priority, true, out var parsedPriority))
                    complaint.Priority = parsedPriority;

                await _context.SaveChangesAsync();

                await _notificationService.SendComplaintSubmittedAsync(complaint, trackingToken.Token);

                return RedirectToAction(nameof(Submitted), new { id = complaint.Id });
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Something went wrong while submitting the complaint.");
                model.RecaptchaSiteKey = _recaptchaSettings.SiteKey;
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> Submitted(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.TrackingTokens)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
                return NotFound();

            return View(complaint);
        }

        [HttpGet]
        public async Task<IActionResult> Track(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View("TrackEntry", new RequestComplaintAccessViewModel());
            }

            var trackingToken = await _context.TrackingTokens
                .Include(t => t.Complaint)
                    .ThenInclude(c => c.Images)
                .Include(t => t.Complaint)
                    .ThenInclude(c => c.Comments)
                .Include(t => t.Complaint)
                    .ThenInclude(c => c.Feedback)
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

            if (trackingToken == null || trackingToken.ExpiresAtUtc < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Tracking link is invalid or expired.";

                return View("TrackEntry", new RequestComplaintAccessViewModel());
            }

            trackingToken.LastUsedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var vm = new TrackComplaintViewModel
            {
                Complaint = trackingToken.Complaint,
                Token = trackingToken.Token,

                NewComment = new AddCitizenCommentViewModel
                {
                    Token = trackingToken.Token
                },

                NewFeedback = new AddComplaintFeedbackViewModel
                {
                    Token = trackingToken.Token
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(
    [Bind(Prefix = "NewFeedback")] AddComplaintFeedbackViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Track), new { token = model.Token });

            var trackingToken = await _context.TrackingTokens
                .Include(t => t.Complaint)
                    .ThenInclude(c => c.Feedback)
                .FirstOrDefaultAsync(t => t.Token == model.Token && t.IsActive);

            if (trackingToken == null)
                return NotFound();

            var complaint = trackingToken.Complaint;

            if (complaint.Status != ComplaintStatus.Resolved)
                return BadRequest();

            if (complaint.Feedback != null)
                return RedirectToAction(nameof(Track), new { token = model.Token });

            var feedback = new ComplaintFeedback
            {
                ComplaintId = complaint.Id,
                Rating = model.Rating,
                Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
                SubmittedAtUtc = DateTime.UtcNow
            };

            _context.ComplaintFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Track), new { token = model.Token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestComplaintAccess(Megdan.Web.ViewModels.RequestComplaintAccessViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("TrackEntry", model);
            }

            var normalizedEmail = model.Email.Trim().ToLowerInvariant();

            var hasComplaints = await _context.Complaints
                .AnyAsync(c => c.CitizenEmail != null && c.CitizenEmail.ToLower() == normalizedEmail);

            if (hasComplaints)
            {
                var accessToken = await _emailAccessService.CreateTokenAsync(normalizedEmail);

                var accessUrl = Url.Action(
                    "UserComplaints",
                    "Complaints",
                    new { accessToken = accessToken.Token },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(accessUrl))
                {
                    await _notificationService.SendComplaintAccessLinkAsync(normalizedEmail, accessUrl);
                }
            }

            TempData["SuccessMessage"] = "If complaints exist for that email, an access link has been sent.";
            return RedirectToAction(nameof(Track));
        }

        [HttpGet]
        public async Task<IActionResult> UserComplaints(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                TempData["ErrorMessage"] = "Access link is missing.";
                return RedirectToAction(nameof(Track));
            }

            var validToken = await _emailAccessService.GetValidTokenAsync(accessToken);

            if (validToken == null)
            {
                TempData["ErrorMessage"] = "Access link is invalid or expired.";
                return RedirectToAction(nameof(Track));
            }

            validToken.UsedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var complaints = await _context.Complaints
                .Where(c => c.CitizenEmail != null && c.CitizenEmail.ToLower() == validToken.Email)
                .Include(c => c.TrackingTokens)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new Megdan.Web.ViewModels.UserComplaintItemViewModel
                {
                    ComplaintId = c.Id,
                    Title = c.Title,
                    Status = c.Status.ToString(),
                    Priority = c.Priority.ToString(),
                    Category = c.Category,
                    CreatedAtUtc = c.CreatedAtUtc,
                    Address = c.Address,
                    TrackingToken = c.TrackingTokens
                        .Where(t => t.IsActive)
                        .OrderByDescending(t => t.CreatedAtUtc)
                        .Select(t => t.Token)
                        .FirstOrDefault() ?? string.Empty
                })
                .ToListAsync();

            var vm = new Megdan.Web.ViewModels.UserComplaintsViewModel
            {
                Email = validToken.Email,
                Complaints = complaints
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckDuplicates(
    [FromBody] ComplaintDuplicateCheckRequest request,
    CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest();
            }

            var matches = await _complaintSimilarityService.FindSimilarComplaintsAsync(
                request.Title,
                request.Description,
                request.Address,
                request.Latitude,
                request.Longitude,
                cancellationToken);

            var response = new ComplaintDuplicateCheckResponse
            {
                HasMatches = matches.Count > 0,
                Matches = matches.Select(x => new ComplaintDuplicateSuggestionViewModel
                {
                    ComplaintId = x.ComplaintId,
                    Title = x.Title,
                    Category = x.Category,
                    Status = x.Status,
                    Priority = x.Priority,
                    LocationLabel = x.LocationLabel,
                    CreatedAt = x.CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy"),
                    SimilarityPercent = (int)Math.Round(x.SimilarityScore * 100),
                    MatchReason = x.MatchReason
                }).ToList()
            };

            return Json(response);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCitizenComment(
    [Bind(Prefix = "NewComment")] AddCitizenCommentViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Track), new { token = model.Token });

            var trackingToken = await _context.TrackingTokens
                .Include(t => t.Complaint)
                .FirstOrDefaultAsync(t => t.Token == model.Token && t.IsActive);

            if (trackingToken == null)
                return NotFound();

            var comment = new ComplaintComment
            {
                ComplaintId = trackingToken.ComplaintId,
                Content = model.Content,
                AuthorType = "Citizen",
                AuthorName = trackingToken.Complaint.CitizenEmail,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.ComplaintComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Track), new { token = model.Token });
        }
    }
}