using System.ComponentModel.DataAnnotations;

namespace ALMTMVC.ViewModels;

public class ReplyEnquiryViewModel
{
    public int EnquiryId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string ServiceRequired { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email subject.")]
    [StringLength(
        200,
        ErrorMessage = "The subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a reply message.")]
    [StringLength(
        5000,
        MinimumLength = 10,
        ErrorMessage =
            "The reply must be between 10 and 5000 characters.")]
    public string Message { get; set; } = string.Empty;
}