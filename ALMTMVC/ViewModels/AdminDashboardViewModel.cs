using ALMTMVC.Models;

namespace ALMTMVC.ViewModels;

public class AdminDashboardViewModel
{
    public IReadOnlyList<ContactEnquiry> Enquiries { get; set; }
        = Array.Empty<ContactEnquiry>();

    public int TotalEnquiries { get; set; }

    public int NewEnquiries { get; set; }

    public int ContactedEnquiries { get; set; }

    public int CompletedEnquiries { get; set; }

    public int FilteredEnquiries { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public string StatusFilter { get; set; } = string.Empty;
}