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
        string? statusFilter)
    {
        searchTerm = searchTerm?.Trim() ?? string.Empty;
        statusFilter = statusFilter?.Trim() ?? string.Empty;

        IQueryable<ContactEnquiry> filteredQuery =
            _context.ContactEnquiries.AsNoTracking();

        // Search by customer details, service or location.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredQuery = filteredQuery.Where(enquiry =>
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

        // Apply a status filter only when it is one of the approved values.
        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            AllowedStatuses.Contains(statusFilter))
        {
            filteredQuery = filteredQuery.Where(
                enquiry => enquiry.Status == statusFilter);
        }

        var enquiries = await filteredQuery
            .OrderByDescending(enquiry => enquiry.SubmittedAtUtc)
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

            FilteredEnquiries = enquiries.Count,

            SearchTerm = searchTerm,

            StatusFilter = statusFilter
        };

        return View(viewModel);
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
}