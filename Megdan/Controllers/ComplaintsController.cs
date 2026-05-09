using Megdan.Domain.Entity;
using Megdan.Domain.Enum;
using Megdan.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Megdan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        // Public: List all for the map
        [HttpGet]
        public IActionResult Get() => Ok(_complaintService.GetAllComplaints());

        // Public: Submit (Guest Mode)
        [HttpPost]
        public IActionResult Post([FromBody] Complaint complaint)
        {
            if (string.IsNullOrEmpty(complaint.UserEmail)) return BadRequest("Email is required.");

            var result = _complaintService.CreateComplaint(complaint);
            return CreatedAtAction(nameof(GetByToken), new { token = result.TrackingToken }, result);
        }

        // Public: Tracking via Token (No login required)
        [HttpGet("track/{token}")]
        public IActionResult GetByToken(string token)
        {
            var complaint = _complaintService.GetComplaintByTrackingToken(token);
            return complaint == null ? NotFound() : Ok(complaint);
        }

        // Admin/Staff: Update Status
        [HttpPatch("{id}/status")]
        public IActionResult UpdateStatus(Guid id, [FromBody] ComplaintStatus status)
        {
            _complaintService.UpdateStatus(id, status);
            return NoContent();
        }
    }
}
