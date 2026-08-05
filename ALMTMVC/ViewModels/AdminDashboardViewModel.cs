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

    public int QuotedEnquiries { get; set; }

    public int ClosedEnquiries { get; set; }

    public int SpamEnquiries { get; set; }

    public int EnquiriesLast7Days { get; set; }

    public int FilteredEnquiries { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    public string StatusFilter { get; set; } = string.Empty;

    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalPages { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public int FirstItemNumber =>
        FilteredEnquiries == 0
            ? 0
            : ((CurrentPage - 1) * PageSize) + 1;

    public int LastItemNumber =>
        Math.Min(CurrentPage * PageSize, FilteredEnquiries);
}