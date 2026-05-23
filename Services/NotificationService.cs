using Megdan.Web.Data;
using Megdan.Web.Models;
using Megdan.Web.Services.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using Megdan.Web.Helpers;

namespace Megdan.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly NotificationSettings _notificationSettings;

        public NotificationService(
            IEmailService emailService,
            AppDbContext context,
            IOptions<NotificationSettings> notificationOptions)
        {
            _emailService = emailService;
            _context = context;
            _notificationSettings = notificationOptions.Value;
        }

        public async Task SendComplaintSubmittedAsync(Complaint complaint, string trackingToken)
        {
            var trackingUrl = BuildAbsoluteUrl($"/Complaints/Track?token={Uri.EscapeDataString(trackingToken)}");
            var subject = $"Complaint #{complaint.Id} received";

            var preview = "Your complaint has been submitted and is now available through your secure tracking link.";

            var content = $@"
                {BuildHero(
                    eyebrow: "Complaint received",
                    title: $"We have recorded complaint #{complaint.Id}.",
                    description: "Thank you for reporting the issue. Your complaint is now in the review workflow, and you can follow updates through your secure tracking page.")}

                {BuildSectionCard($@"
                    {BuildInfoRow("Complaint ID", complaint.Id.ToString())}
                    {BuildInfoRow("Current status", FormatValue(complaint.Status))}
                    {BuildInfoRow("Priority", FormatValue(complaint.Priority))}
                    {BuildInfoRow("Title", complaint.Title)}
                ")}

                {BuildButton(trackingUrl, "Open tracking page")}

                {BuildSupportNote("Keep this link somewhere safe. You can use it to check status updates and access your complaint later.")}
            ";

            var body = BuildEmailLayout(subject, preview, content);
            await SendAndLogAsync(complaint.Id, complaint.CitizenEmail, subject, body, "ComplaintSubmitted");
        }

        public async Task SendStatusChangedAsync(Complaint complaint)
        {
            var subject = $"Complaint #{complaint.Id} status updated";

            var preview = $"Your complaint is now marked as {FormatValue(complaint.Status)}.";

            var content = $@"
                {BuildHero(
                    eyebrow: "Status update",
                    title: $"Complaint #{complaint.Id} has been updated.",
                    description: $"Your complaint status is now {FormatValue(complaint.Status)}. You can review the latest details on the tracking page.")}

                {BuildSectionCard($@"
                    {BuildInfoRow("Complaint ID", complaint.Id.ToString())}
                    {BuildInfoRow("New status", FormatValue(complaint.Status))}
                    {BuildInfoRow("Priority", BuildPriorityBadge(FormatValue(complaint.Priority)))}
                    {BuildInfoRow("Title", complaint.Title)}
                    {(!string.IsNullOrWhiteSpace(complaint.Address) ? BuildInfoRow("Address", complaint.Address) : string.Empty)}
                ")}

                {BuildSupportNote("If you already have your tracking link, use it to see the most recent updates and any administrator comments.")}
            ";

            var body = BuildEmailLayout(subject, preview, content);
            await SendAndLogAsync(complaint.Id, complaint.CitizenEmail, subject, body, "StatusChanged");
        }

        public async Task SendUrgentAlertAsync(Complaint complaint)
        {
            if (string.IsNullOrWhiteSpace(_notificationSettings.AdminAlertEmail))
                return;

            var subject = $"Urgent complaint #{complaint.Id} requires attention";

            var preview = "An urgent complaint has been submitted and should be reviewed immediately.";

            var content = $@"
                {BuildHero(
                    eyebrow: "Urgent alert",
                    title: $"Immediate review needed for complaint #{complaint.Id}.",
                    description: "A complaint marked as urgent has entered the system. Review the details below and take action as soon as possible.",
                    accentBackground: "#fff1f2",
                    accentBorder: "#fecdd3",
                    titleColor: "#9f1239",
                    descriptionColor: "#881337")}

                {BuildSectionCard($@"
                    {BuildInfoRow("Complaint ID", complaint.Id.ToString())}
                    {BuildInfoRow("Title", complaint.Title)}
                    {BuildInfoRow("Reported by", complaint.CitizenEmail)}
                    {BuildInfoRow("Priority", BuildPriorityBadge("Urgent"))}
                    {BuildInfoRow("Address", string.IsNullOrWhiteSpace(complaint.Address) ? "N/A" : complaint.Address)}
                    {BuildInfoRow("Description", complaint.Description)}
                ")}
            ";

            var body = BuildEmailLayout(subject, preview, content);
            await SendAndLogAsync(complaint.Id, _notificationSettings.AdminAlertEmail, subject, body, "UrgentAlert");
        }

        public async Task SendComplaintAccessLinkAsync(string email, string accessUrl)
        {
            var subject = "Access your submitted complaints";

            var preview = "Use this secure link to open the list of complaints associated with your email.";

            var content = $@"
                {BuildHero(
                    eyebrow: "Email access link",
                    title: "Your complaint access link is ready.",
                    description: "Use the button below to open the page that lists complaints associated with this email address. For your security, the link expires in 30 minutes.")}

                {BuildSectionCard($@"
                    {BuildInfoRow("Email", email)}
                    {BuildInfoRow("Link validity", "30 minutes")}
                    {BuildInfoRow("Purpose", "Open your complaint list and continue to individual tracking pages")}
                ")}

                {BuildButton(accessUrl, "Open your complaints")}

                {BuildMutedText("If you did not request this email, you can safely ignore it. No changes were made to any complaint.")}
            ";

            var body = BuildEmailLayout(subject, preview, content);
            await SendAndLogAsync(
                null,
                email,
                subject,
                body,
                "ComplaintAccessLink");
        }

        private async Task SendAndLogAsync(int? complaintId, string toEmail, string subject, string body, string type)
        {
            var log = new NotificationLog
            {
                ComplaintId = complaintId,
                RecipientEmail = toEmail,
                Subject = subject,
                NotificationType = type,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, body);
                log.IsSuccess = true;
            }
            catch (Exception ex)
            {
                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;
            }

            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private string BuildAbsoluteUrl(string path)
        {
            var baseUrl = (_notificationSettings.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "https://localhost:5001";
            }

            return $"{baseUrl}{path}";
        }

        private static string BuildEmailLayout(string subject, string previewText, string innerHtml)
        {
            var safeSubject = HtmlEncode(subject);
            var safePreview = HtmlEncode(previewText);

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>{safeSubject}</title>
</head>
<body style=""margin:0; padding:0; background-color:#f7f6f2; font-family:Arial, Helvetica, sans-serif; color:#28251d;"">
    <div style=""display:none; max-height:0; overflow:hidden; opacity:0; mso-hide:all;"">
        {safePreview}
    </div>

    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""width:100%; background-color:#f7f6f2; margin:0; padding:24px 0;"">
        <tr>
            <td align=""center"" style=""padding:0 16px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""max-width:640px; width:100%;"">
                    <tr>
                        <td style=""padding-bottom:16px;"">
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""width:100%;"">
                                <tr>
                                    <td align=""left"" style=""padding:0;"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"">
                                            <tr>
                                                <td style=""width:44px; height:44px; background-color:#01696f; border-radius:14px; text-align:center; vertical-align:middle; color:#ffffff; font-size:20px; font-weight:700;"">
                                                    M
                                                </td>
                                                <td style=""padding-left:12px; vertical-align:middle;"">
                                                    <div style=""font-size:17px; font-weight:700; color:#28251d; line-height:1.2;"">Megdan</div>
                                                    <div style=""font-size:12px; letter-spacing:1.4px; text-transform:uppercase; color:#7a7974; line-height:1.4;"">Citizen complaints</div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <tr>
                        <td style=""background-color:#ffffff; border:1px solid #e7e5e4; border-radius:28px; padding:32px 24px;"">
                            {innerHtml}
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:18px 8px 0 8px; text-align:center;"">
                            <div style=""font-size:12px; line-height:1.7; color:#7a7974;"">
                                This is an automated service email from Megdan.<br/>
                                Please do not reply unless your configured mailbox supports replies.
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private static string BuildHero(
            string eyebrow,
            string title,
            string description,
            string accentBackground = "#eef7f6",
            string accentBorder = "#d9ece9",
            string titleColor = "#1c1917",
            string descriptionColor = "#57534e")
        {
            return $@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""width:100%; margin-bottom:20px;"">
    <tr>
        <td style=""background-color:{accentBackground}; border:1px solid {accentBorder}; border-radius:24px; padding:24px;"">
            <div style=""font-size:11px; font-weight:700; letter-spacing:1.6px; text-transform:uppercase; color:#0c4e54; margin-bottom:10px;"">{HtmlEncode(eyebrow)}</div>
            <div style=""font-size:30px; line-height:1.2; font-weight:800; color:{titleColor}; margin-bottom:12px;"">{HtmlEncode(title)}</div>
            <div style=""font-size:15px; line-height:1.8; color:{descriptionColor};"">{HtmlEncode(description)}</div>
        </td>
    </tr>
</table>";
        }

        private static string BuildSectionCard(string content)
        {
            return $@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""width:100%; margin-bottom:20px;"">
    <tr>
        <td style=""background-color:#fafaf9; border:1px solid #e7e5e4; border-radius:22px; padding:18px 18px 8px 18px;"">
            {content}
        </td>
    </tr>
</table>";
        }

        private static string BuildInfoRow(string label, string value)
        {
            return $@"
<div style=""padding:0 0 12px 0; margin-bottom:12px; border-bottom:1px solid #e7e5e4;"">
    <div style=""font-size:11px; font-weight:700; letter-spacing:1.3px; text-transform:uppercase; color:#78716c; margin-bottom:6px;"">{HtmlEncode(label)}</div>
    <div style=""font-size:15px; line-height:1.7; color:#292524;"">{value}</div>
</div>";
        }

        private static string BuildButton(string url, string text)
        {
            var safeUrl = HtmlAttributeEncode(url);
            var safeText = HtmlEncode(text);

            return $@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""margin:0 0 20px 0;"">
    <tr>
        <td align=""center"" bgcolor=""#01696f"" style=""border-radius:999px; background-color:#01696f;"">
            <a href=""{safeUrl}""
               target=""_blank""
               style=""display:inline-block; padding:14px 24px; font-size:15px; line-height:1.2; font-weight:700; color:#ffffff; text-decoration:none; border-radius:999px; background-color:#01696f;"">
               {safeText}
            </a>
        </td>
    </tr>
</table>";
        }

        private static string BuildSupportNote(string text)
        {
            return $@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""width:100%; margin-top:4px;"">
    <tr>
        <td style=""background-color:#1c1917; border-radius:22px; padding:18px 20px;"">
            <div style=""font-size:11px; font-weight:700; letter-spacing:1.4px; text-transform:uppercase; color:#a8a29e; margin-bottom:8px;"">Helpful note</div>
            <div style=""font-size:14px; line-height:1.8; color:#f5f5f4;"">{HtmlEncode(text)}</div>
        </td>
    </tr>
</table>";
        }

        private static string BuildMutedText(string text)
        {
            return $@"
<div style=""font-size:13px; line-height:1.8; color:#78716c; margin-top:4px;"">
    {HtmlEncode(text)}
</div>";
        }

        private static string BuildPriorityBadge(string priority)
        {
            var value = (priority ?? string.Empty).Trim().ToLowerInvariant();

            string background;
            string color;

            switch (value)
            {
                case "urgent":
                    background = "#ffe4e6";
                    color = "#be123c";
                    break;
                case "high":
                    background = "#fef3c7";
                    color = "#b45309";
                    break;
                case "medium":
                    background = "#d9ece9";
                    color = "#0c4e54";
                    break;
                case "low":
                    background = "#d1fae5";
                    color = "#065f46";
                    break;
                default:
                    background = "#e7e5e4";
                    color = "#57534e";
                    break;
            }

            return $@"<span style=""display:inline-block; padding:4px 10px; border-radius:999px; background-color:{background}; color:{color}; font-size:12px; font-weight:700;"">{HtmlEncode(priority)}</span>";
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
                return HtmlEncode("N/A");

            if (value is Enum enumValue)
                return HtmlEncode(enumValue.ToDisplayString());

            return HtmlEncode(value.ToString() ?? "N/A");
        }

        private static string HtmlEncode(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string HtmlAttributeEncode(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}

//using Megdan.Web.Data;
//using Megdan.Web.Models;
//using Megdan.Web.Services.Configuration;
//using Microsoft.Extensions.Options;

//namespace Megdan.Web.Services
//{
//    public class NotificationService : INotificationService
//    {
//        private readonly IEmailService _emailService;
//        private readonly AppDbContext _context;
//        private readonly NotificationSettings _notificationSettings;

//        public NotificationService(
//            IEmailService emailService,
//            AppDbContext context,
//            IOptions<NotificationSettings> notificationOptions)
//        {
//            _emailService = emailService;
//            _context = context;
//            _notificationSettings = notificationOptions.Value;
//        }

//        public async Task SendComplaintSubmittedAsync(Complaint complaint, string trackingToken)
//        {
//            var trackingUrl = $"https://localhost:5001/Complaints/Track?token={trackingToken}";
//            var subject = $"Complaint #{complaint.Id} submitted";
//            var body = $@"
//                <p>Your complaint has been submitted successfully.</p>
//                <p><strong>Complaint ID:</strong> {complaint.Id}</p>
//                <p><strong>Status:</strong> {complaint.Status}</p>
//                <p><a href='{trackingUrl}'>Open your tracking page</a></p>";

//            await SendAndLogAsync(complaint.Id, complaint.CitizenEmail, subject, body, "ComplaintSubmitted");
//        }

//        public async Task SendStatusChangedAsync(Complaint complaint)
//        {
//            var subject = $"Complaint #{complaint.Id} status updated";
//            var body = $@"
//                <p>Your complaint status has changed.</p>
//                <p><strong>Complaint ID:</strong> {complaint.Id}</p>
//                <p><strong>New Status:</strong> {complaint.Status}</p>
//                <p><strong>Priority:</strong> {complaint.Priority}</p>";

//            await SendAndLogAsync(complaint.Id, complaint.CitizenEmail, subject, body, "StatusChanged");
//        }

//        public async Task SendUrgentAlertAsync(Complaint complaint)
//        {
//            if (string.IsNullOrWhiteSpace(_notificationSettings.AdminAlertEmail))
//                return;

//            var subject = $"URGENT complaint #{complaint.Id}";
//            var body = $@"
//                <p>An urgent complaint requires immediate attention.</p>
//                <p><strong>Complaint ID:</strong> {complaint.Id}</p>
//                <p><strong>Title:</strong> {complaint.Title}</p>
//                <p><strong>Email:</strong> {complaint.CitizenEmail}</p>
//                <p><strong>Address:</strong> {complaint.Address}</p>
//                <p><strong>Description:</strong> {complaint.Description}</p>";

//            await SendAndLogAsync(complaint.Id, _notificationSettings.AdminAlertEmail, subject, body, "UrgentAlert");
//        }

//        public async Task SendComplaintAccessLinkAsync(string email, string accessUrl)
//        {
//            var subject = "Access your submitted complaints";

//            var body = $@"
//        <p>You requested access to your submitted complaints.</p>
//        <p>Use the link below to view your complaint list:</p>
//        <p><a href=""{accessUrl}"">{accessUrl}</a></p>
//        <p>This link expires in 30 minutes.</p>";

//            await SendAndLogAsync(
//                null,
//                email,
//                subject,
//                body,
//                "ComplaintAccessLink");
//        }

//        private async Task SendAndLogAsync(int? complaintId, string toEmail, string subject, string body, string type)
//        {
//            var log = new NotificationLog
//            {
//                ComplaintId = complaintId,
//                RecipientEmail = toEmail,
//                Subject = subject,
//                NotificationType = type,
//                CreatedAtUtc = DateTime.UtcNow
//            };

//            try
//            {
//                await _emailService.SendEmailAsync(toEmail, subject, body);
//                log.IsSuccess = true;
//            }
//            catch (Exception ex)
//            {
//                log.IsSuccess = false;
//                log.ErrorMessage = ex.Message;
//            }

//            _context.NotificationLogs.Add(log);
//            await _context.SaveChangesAsync();
//        }
//    }
//}