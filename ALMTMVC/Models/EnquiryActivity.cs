using System.ComponentModel.DataAnnotations;

namespace ALMTMVC.Models;

public class EnquiryActivity
{
    public int Id { get; set; }

    public int ContactEnquiryId { get; set; }

    public ContactEnquiry ContactEnquiry { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000)]
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}