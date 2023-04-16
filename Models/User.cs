using Payment_services.Models;
using System;
namespace Payment_services.Models
{
    public record AddStripeCustomer(
        string Email,
        string Name,
        AddStripeCard CreditCard);
}

