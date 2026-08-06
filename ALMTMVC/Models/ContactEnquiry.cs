using System.ComponentModel.DataAnnotations;

namespace ALMTMVC.Models;

public class ContactEnquiry
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    [StringLength(100)]
    public string ServiceRequired { get; set; } = string.Empty;

    [StringLength(250)]
    public string? ProjectLocation { get; set; }

    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    public bool ConsentToContact { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "New";

    public ICollection<EnquiryActivity> Activities { get; set; }
        = new List<EnquiryActivity>();
}