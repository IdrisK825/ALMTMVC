using ALMTMVC.Models;

namespace ALMTMVC.Services;

public interface IEmailService
{
    Task SendCompanyNotificationAsync(
        ContactEnquiry enquiry);

    Task SendCustomerConfirmationAsync(
        ContactEnquiry enquiry);
}