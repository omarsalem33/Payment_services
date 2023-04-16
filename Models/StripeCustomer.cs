using System;
namespace Payment_services.Models
{
    public record StripeCustomer(
        string Name,
        string Email,
        string CustomerId);
}

