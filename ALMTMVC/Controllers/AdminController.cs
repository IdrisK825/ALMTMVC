using System.Text;
using ALMTMVC.Data;
using ALMTMVC.Models;
using ALMTMVC.Services;
using ALMTMVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ALMTMVC.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const int DefaultPageSize = 10;

    private static readonly string[] AllowedStatuses =
    {
        "New",
        "Contacted",
        "Quoted",
        "Completed",
        "Closed",
        "Spam"
    };

    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // ==========================================
    // ADMIN DASHBOARD
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? statusFilter,
        int page = 1)
    {
        searchTerm = searchTerm?.Trim() ?? string.Empty;
        statusFilter = statusFilter?.Trim() ?? string.Empty;
        page = Math.Max(page, 1);

        IQueryable<ContactEnquiry> filteredQuery =
            BuildFilteredQuery(searchTerm, statusFilter);

        int filteredCount = await filteredQuery.CountAsync();

        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                filteredCount / (double)DefaultPageSize));

        if (page > totalPages)
        {
            page = totalPages;
        }

        var enquiries = await filteredQuery
            .OrderByDescending(enquiry => enquiry.SubmittedAtUtc)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            Enquiries = enquiries,

            TotalEnquiries =
                await _context.ContactEnquiries.CountAsync(),

            NewEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "New"),

            ContactedEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "Contacted"),

            CompletedEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "Completed"),

            QuotedEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "Quoted"),

            ClosedEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "Closed"),

            SpamEnquiries =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.Status == "Spam"),

            EnquiriesLast7Days =
                await _context.ContactEnquiries.CountAsync(
                    enquiry => enquiry.SubmittedAtUtc >=
                        DateTime.UtcNow.AddDays(-7)),

            FilteredEnquiries = filteredCount,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            CurrentPage = page,
            PageSize = DefaultPageSize,
            TotalPages = totalPages
        };

        return View(viewModel);
    }

    // ==========================================
    // EXPORT ENQUIRIES TO CSV
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Export(
        string? searchTerm,
        string? statusFilter)
    {
        searchTerm = searchTerm?.Trim() ?? string.Empty;
        statusFilter = statusFilter?.Trim() ?? string.Empty;

        IQueryable<ContactEnquiry> filteredQuery =
            BuildFilteredQuery(searchTerm, statusFilter);

        var enquiries = await filteredQuery
            .OrderByDescending(enquiry => enquiry.SubmittedAtUtc)
            .ToListAsync();

        var csv = new StringBuilder();

        csv.Append('\uFEFF');

        csv.AppendLine(
            "ID,Full Name,Email,Phone Number,Company," +
            "Service Required,Project Location,Message," +
            "Consent to Contact,Submitted Date,Status");

        foreach (var enquiry in enquiries)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(enquiry.Id.ToString()),
                EscapeCsv(enquiry.FullName),
                EscapeCsv(enquiry.Email),
                EscapeCsv(enquiry.PhoneNumber),
                EscapeCsv(enquiry.CompanyName),
                EscapeCsv(enquiry.ServiceRequired),
                EscapeCsv(enquiry.ProjectLocation),
                EscapeCsv(enquiry.Message),
                EscapeCsv(
                    enquiry.ConsentToContact
                        ? "Yes"
                        : "No"),
                EscapeCsv(
                    enquiry.SubmittedAtUtc
                        .ToLocalTime()
                        .ToString("yyyy-MM-dd HH:mm")),
                EscapeCsv(enquiry.Status)));
        }

        byte[] fileContents =
            Encoding.UTF8.GetBytes(csv.ToString());

        string fileName =
            $"ALMT-Enquiries-{DateTime.Now:yyyy-MM-dd-HHmm}.csv";

        return File(
            fileContents,
            "text/csv; charset=utf-8",
            fileName);
    }

    // ==========================================
    // ENQUIRY DETAILS AND TIMELINE
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var enquiry = await _context.ContactEnquiries
            .AsNoTracking()
            .Include(enquiry => enquiry.Activities)
            .FirstOrDefaultAsync(enquiry => enquiry.Id == id);

        if (enquiry is null)
        {
            return NotFound();
        }

        enquiry.Activities = enquiry.Activities
            .OrderByDescending(activity => activity.CreatedAtUtc)
            .ToList();

        return View(enquiry);
    }

    // ==========================================
    // REPLY TO CUSTOMER
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Reply(int id)
    {
        var enquiry = await _context.ContactEnquiries
            .AsNoTracking()
            .FirstOrDefaultAsync(enquiry => enquiry.Id == id);

        if (enquiry is null)
        {
            return NotFound();
        }

        var viewModel = new ReplyEnquiryViewModel
        {
            EnquiryId = enquiry.Id,
            CustomerName = enquiry.FullName,
            CustomerEmail = enquiry.Email,
            ServiceRequired = enquiry.ServiceRequired,

            Subject =
                $"Regarding your {enquiry.ServiceRequired} enquiry",

            Message = $"""
                Hi {enquiry.FullName},

                Thank you for contacting Almighty Lift Consultants regarding your {enquiry.ServiceRequired} enquiry.

                We have reviewed your request and would be happy to assist you.

                Kind regards,
                Almighty Lift Consultants
                """
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(
        ReplyEnquiryViewModel model)
    {
        var enquiry = await _context.ContactEnquiries
            .FirstOrDefaultAsync(
                enquiry => enquiry.Id == model.EnquiryId);

        if (enquiry is null)
        {
            return NotFound();
        }

        model.CustomerName = enquiry.FullName;
        model.CustomerEmail = enquiry.Email;
        model.ServiceRequired = enquiry.ServiceRequired;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _emailService.SendAdminReplyAsync(
                enquiry,
                model.Subject,
                model.Message);

            string previousStatus = enquiry.Status;

            if (enquiry.Status == "New")
            {
                enquiry.Status = "Contacted";
            }

            var replyActivity = new EnquiryActivity
            {
                ContactEnquiryId = enquiry.Id,
                ActivityType = "ReplySent",
                Title = "Reply sent to customer",
                Description =
                    $"Subject: {model.Subject}\n\n{model.Message}",
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.EnquiryActivities.Add(replyActivity);

            if (previousStatus != enquiry.Status)
            {
                var statusActivity = new EnquiryActivity
                {
                    ContactEnquiryId = enquiry.Id,
                    ActivityType = "StatusChanged",
                    Title = "Enquiry status changed",
                    Description =
                        $"{previousStatus} → {enquiry.Status}",
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.EnquiryActivities.Add(statusActivity);
            }

            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                $"Your reply was sent successfully to {enquiry.Email}.";

            return RedirectToAction(
                nameof(Details),
                new { id = enquiry.Id });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Admin reply failed for enquiry {EnquiryId}.",
                enquiry.Id);

            ModelState.AddModelError(
                string.Empty,
                "The reply could not be sent. Please check the email configuration and try again.");

            return View(model);
        }
    }

    // ==========================================
    // UPDATE STATUS
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string status)
    {
        if (!AllowedStatuses.Contains(status))
        {
            TempData["AdminError"] =
                "The selected enquiry status is invalid.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var enquiry = await _context.ContactEnquiries
            .FirstOrDefaultAsync(enquiry => enquiry.Id == id);

        if (enquiry is null)
        {
            return NotFound();
        }

        string previousStatus = enquiry.Status;

        if (previousStatus == status)
        {
            TempData["AdminSuccess"] =
                "The enquiry status is already set to that value.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        enquiry.Status = status;

        var activity = new EnquiryActivity
        {
            ContactEnquiryId = enquiry.Id,
            ActivityType = "StatusChanged",
            Title = "Enquiry status changed",
            Description = $"{previousStatus} → {status}",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.EnquiryActivities.Add(activity);

        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] =
            "The enquiry status was updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    // ==========================================
    // DELETE ENQUIRY
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var enquiry = await _context.ContactEnquiries
            .FirstOrDefaultAsync(enquiry => enquiry.Id == id);

        if (enquiry is null)
        {
            return NotFound();
        }

        _context.ContactEnquiries.Remove(enquiry);

        await _context.SaveChangesAsync();

        TempData["AdminSuccess"] =
            "The enquiry was deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // SHARED FILTER QUERY
    // ==========================================

    private IQueryable<ContactEnquiry> BuildFilteredQuery(
        string searchTerm,
        string statusFilter)
    {
        IQueryable<ContactEnquiry> query =
            _context.ContactEnquiries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(enquiry =>
                enquiry.FullName.Contains(searchTerm) ||
                enquiry.Email.Contains(searchTerm) ||
                enquiry.ServiceRequired.Contains(searchTerm) ||
                (enquiry.CompanyName != null &&
                 enquiry.CompanyName.Contains(searchTerm)) ||
                (enquiry.PhoneNumber != null &&
                 enquiry.PhoneNumber.Contains(searchTerm)) ||
                (enquiry.ProjectLocation != null &&
                 enquiry.ProjectLocation.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            AllowedStatuses.Contains(statusFilter))
        {
            query = query.Where(
                enquiry => enquiry.Status == statusFilter);
        }

        return query;
    }

    // ==========================================
    // CSV SAFETY
    // ==========================================

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;

        if (value.StartsWith('=') ||
            value.StartsWith('+') ||
            value.StartsWith('-') ||
            value.StartsWith('@'))
        {
            value = "'" + value;
        }

        value = value.Replace("\"", "\"\"");

        return $"\"{value}\"";
    }
}