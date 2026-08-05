using System.Text;
using ALMTMVC.Data;
using ALMTMVC.Models;
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

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
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
    // ENQUIRY DETAILS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var enquiry = await _context.ContactEnquiries
            .AsNoTracking()
            .FirstOrDefaultAsync(enquiry => enquiry.Id == id);

        if (enquiry is null)
        {
            return NotFound();
        }

        return View(enquiry);
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

        enquiry.Status = status;

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