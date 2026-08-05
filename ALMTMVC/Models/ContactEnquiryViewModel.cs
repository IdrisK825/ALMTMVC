using System.ComponentModel.DataAnnotations;

namespace ALMTMVC.Models;

public class ContactEnquiryViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(
        100,
        ErrorMessage = "Your name cannot exceed 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(30)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    [Display(Name = "Company or building name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Please select a service.")]
    [Display(Name = "Service required")]
    public string ServiceRequired { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Building address or project location")]
    public string? ProjectLocation { get; set; }

    [Required(ErrorMessage = "Please tell us how we can assist.")]
    [StringLength(
        2000,
        MinimumLength = 10,
        ErrorMessage = "Your message must contain between 10 and 2000 characters.")]
    public string Message { get; set; } = string.Empty;

    [Range(
        typeof(bool),
        "true",
        "true",
        ErrorMessage = "Please confirm that we may contact you.")]
    [Display(Name = "I agree that Almighty Lift Consultants may contact me about this enquiry.")]
    public bool ConsentToContact { get; set; }

    // Hidden field used later as a simple spam trap.
    public string? Website { get; set; }
}