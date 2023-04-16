# Payment_services
## What is Stripe?
Stripe is a payment service provider that allows merchants to accept credit cards online. Stripe is used by all sizes of businesses including companies like Shopify and Amazon.
## Prerequisites
In order to follow along in this tutorial about accepting Stripe payments in .NET you need to have the following:
- [A Stripe Account](https://dashboard.stripe.com/test/dashboard).
- [Visual Studio IDE](https://visualstudio.microsoft.com/vs/?ref=blog.christian-schou.dk).
- Basic [C#](https://learn.microsoft.com/en-us/dotnet/csharp/?ref=blog.christian-schou.dk) knowledge.
- Basic [.NET](https://dotnet.microsoft.com/en-us/?ref=blog.christian-schou.dk) knowledge.

## Install Dependencies
### Install through Package Manager Console:
```C#
Install-Package Stripe.net
```
Create a new folder named Models and a child folder named Stripe. Inside Stripe, create 5 new files like the ones below. The final project structure should now look like this:
**AddStripeCard.cs**
```C#
using System;
namespace Payment_services.Models
{
    public record AddStripePayment(
        string CustomerId,
        string ReceiptEmail,
        string Description,
        string Currency,
        long Amount);
}
```
**User**
```C#
using Payment_services.Models;
using System;
namespace Payment_services.Models
{
    public record AddStripeCustomer(
        string Email,
        string Name,
        AddStripeCard CreditCard);
}
```

**Card_Info.cs**
```C#
using System;
namespace Payment_services.Models
{
    public record AddStripeCard(
        string Name,
        string CardNumber,
        string ExpirationYear,
        string ExpirationMonth,
        string Cvc);
}
```

**StripeCustomer.cs**
```C#
using System;
namespace Payment_services.Models
{
    public record StripeCustomer(
        string Name,
        string Email,
        string CustomerId);
}
```

**StripePayment**
```C#
using System;
namespace Payment_services.Models
{
    public record StripePayment(
        string CustomerId,
        string ReceiptEmail,
        string Description,
        string Currency,
        long Amount,
        string PaymentId);
}
```

## Add Services to work with Stripe
Create a new folder named `Contracts` and `Application` in the root of your project. Inside `Contracts` add a new interface named `IStripeAppService` with the following code inside:

```C#
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

```
Create a new file named `StripeAppService` inside the `Application.` Let's split this service class up into two sections. One for our Customer creation and a second one for adding a new payment to Stripe.
Dependency Injection in `StripeAppService`
```C#
using System;
using Payment_services.Contracts;
using Payment_services.Models;
using Stripe;


namespace Payment_services.Application
{
    public class StripeAppService : IStripeAppService
    {
        private readonly ChargeService _chargeService;
        private readonly CustomerService _customerService;
        private readonly TokenService _tokenService;

        public StripeAppService(
            ChargeService chargeService,
            CustomerService customerService,
            TokenService tokenService)
        {
            _chargeService = chargeService;
            _customerService = customerService;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Create a new customer at Stripe through API using customer and card details from records.
        /// </summary>
        /// <param name="customer">Stripe Customer</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>Stripe Customer</returns>
      
            public async Task<StripeCustomer> AddStripeCustomerAsync(AddStripeCustomer customer, CancellationToken ct)
            {
                // Set Stripe Token options based on customer data
                TokenCreateOptions tokenOptions = new TokenCreateOptions
                {
                    Card = new TokenCardOptions
                    {
                        Name = customer.Name,
                        Number = customer.CreditCard.CardNumber,
                        ExpYear = customer.CreditCard.ExpirationYear,
                        ExpMonth = customer.CreditCard.ExpirationMonth,
                        Cvc = customer.CreditCard.Cvc
                    }
                };

                // Create new Stripe Token
                Token stripeToken = await _tokenService.CreateAsync(tokenOptions, null, ct);

                // Set Customer options using
                CustomerCreateOptions customerOptions = new CustomerCreateOptions
                {
                    Name = customer.Name,
                    Email = customer.Email,
                    Source = stripeToken.Id
                };

                // Create customer at Stripe
                Customer createdCustomer = await _customerService.CreateAsync(customerOptions, null, ct);

                // Return the created customer at stripe
                return new StripeCustomer(createdCustomer.Name, createdCustomer.Email, createdCustomer.Id);
            }

        

        /// <summary>
        /// Add a new payment at Stripe using Customer and Payment details.
        /// Customer has to exist at Stripe already.
        /// </summary>
        /// <param name="payment">Stripe Payment</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns><Stripe Payment/returns>
       
            public async Task<StripePayment> AddStripePaymentAsync(AddStripePayment payment, CancellationToken ct)
            {
                // Set the options for the payment we would like to create at Stripe
                ChargeCreateOptions paymentOptions = new ChargeCreateOptions
                {
                    Customer = payment.CustomerId,
                    ReceiptEmail = payment.ReceiptEmail,
                    Description = payment.Description,
                    Currency = payment.Currency,
                    Amount = payment.Amount
                };

                // Create the payment
                var createdPayment = await _chargeService.CreateAsync(paymentOptions, null, ct);

                // Return the payment to requesting method
                return new StripePayment(
                  createdPayment.CustomerId,
                  createdPayment.ReceiptEmail,
                  createdPayment.Description,
                  createdPayment.Currency,
                  createdPayment.Amount,
                  createdPayment.Id);
            }


        
    }
}
```

## Add Controllers
Create a new controller named StripeController and add the following code inside - I will explain below.
```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Payment_services.Contracts;
using Payment_services.Models;

namespace Payment_services.Controllers
{
    [Route("api/[controller]")]
    public class StripeController : Controller
    {
        private readonly IStripeAppService _stripeService;

        public StripeController(IStripeAppService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpPost("customer/add")]
        public async Task<ActionResult<StripeCustomer>> AddStripeCustomer([FromBody] AddStripeCustomer customer,CancellationToken ct)
        {
            StripeCustomer createdCustomer = await _stripeService.AddStripeCustomerAsync(customer,ct);

            return StatusCode(StatusCodes.Status200OK, createdCustomer);
        }

        [HttpPost("payment/add")]
        public async Task<ActionResult<StripePayment>> AddStripePayment([FromBody] AddStripePayment payment,CancellationToken ct)
        {
            StripePayment createdPayment = await _stripeService.AddStripePaymentAsync(payment,ct);

            return StatusCode(StatusCodes.Status200OK, createdPayment);
        }
    }
}
```
### Let's divide the controller into three sections.
1. First, we create a constructor with dependency injection for our `IStripeAppService`.
1. Then we add a new controller action `AddStripeCustomer()` that requests the service for Stripe in our backend and created the customer async.
1. The second controller action `AddStripePayment()` takes in a payment record in the body and requests a new charge through the Stripe service we injected in our constructor.
1. Finally, we return the created resource (the response from the app service) in both actions.

## Register Services
I have separated the service registrations into a separate file named `StripeInfrastructure` located at the root of the project. Inside it I have added the following code:

```C#
using System;
using Payment_services.Application;
using Payment_services.Contracts;
using Stripe;

namespace Payment_services
{
    public static class StripeInfrastructure
    {
        public static IServiceCollection AddStripeInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration.GetValue<string>("StripeSettings:SecretKey");

            return services
                .AddScoped<CustomerService>()
                .AddScoped<ChargeService>()
                .AddScoped<TokenService>()
                .AddScoped<IStripeAppService, StripeAppService>();
        }
    }
}
```
Let's update `Program.cs` to the following instead of the default from Microsoft. This will load the `StripeInfrastructure` at every startup.
```C#
using Payment_services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Stripe Infrastructure
builder.Services.AddStripeInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

```

