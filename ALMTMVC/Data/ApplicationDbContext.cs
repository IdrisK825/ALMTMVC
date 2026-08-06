using ALMTMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ALMTMVC.Data;

public class ApplicationDbContext
    : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContactEnquiry> ContactEnquiries =>
        Set<ContactEnquiry>();

    public DbSet<EnquiryActivity> EnquiryActivities =>
        Set<EnquiryActivity>();

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EnquiryActivity>()
            .HasOne(activity => activity.ContactEnquiry)
            .WithMany(enquiry => enquiry.Activities)
            .HasForeignKey(activity =>
                activity.ContactEnquiryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}