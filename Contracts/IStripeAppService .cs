using System;
using Payment_services.Models;


namespace Payment_services.Contracts
{
    public interface IStripeAppService
    {
        Task<StripeCustomer> AddStripeCustomerAsync(AddStripeCustomer customer, CancellationToken ct);
       
        Task<StripePayment> AddStripePaymentAsync(AddStripePayment payment, CancellationToken ct);
      
    }
}

