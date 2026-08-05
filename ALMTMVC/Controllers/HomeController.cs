using ALMTMVC.Data;
using ALMTMVC.Models;
using ALMTMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace ALMTMVC.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<HomeController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Services()
    {
        return View();
    }

    public IActionResult Projects()
    {
        return View();
    }

    public IActionResult Gallery()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactEnquiryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(
        ContactEnquiryViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Website))
        {
            return RedirectToAction(nameof(ContactSuccess));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var enquiry = new ContactEnquiry
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            CompanyName = model.CompanyName,
            ServiceRequired = model.ServiceRequired,
            ProjectLocation = model.ProjectLocation,
            Message = model.Message,
            ConsentToContact = model.ConsentToContact,
            SubmittedAtUtc = DateTime.UtcNow,
            Status = "New"
        };

        _context.ContactEnquiries.Add(enquiry);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService
                .SendCompanyNotificationAsync(enquiry);

            await _emailService
                .SendCustomerConfirmationAsync(enquiry);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Enquiry {EnquiryId} was saved, but email sending failed.",
                enquiry.Id);
        }

        TempData["CustomerName"] = model.FullName;

        return RedirectToAction(nameof(ContactSuccess));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ContactSuccess()
    {
        return View();
    }
}