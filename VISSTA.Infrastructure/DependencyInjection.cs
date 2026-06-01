using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VISSTA.Application.Interfaces;
using VISSTA.Domain.Entities;
using VISSTA.Infrastructure.Handlers;
using VISSTA.Infrastructure.Identity;
using VISSTA.Infrastructure.Persistence;
using VISSTA.Infrastructure.Repositories;
using VISSTA.Infrastructure.Services;
using VISSTA.Infrastructure.Settings;

namespace VISSTA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<SiteSettings>(configuration.GetSection("Site"));
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.Configure<PaymentSettings>(configuration.GetSection("Payment"));

        services.AddDbContext<VISSTADbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<VISSTADbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IUserAccountLookupService, IdentityAccountLookupService>();
        services.AddScoped<IPaymentService, MockPaymentService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IPasswordResetOtpRepository, PasswordResetOtpRepository>();
        services.AddScoped<INewsletterRepository, NewsletterRepository>();
        services.AddSingleton<ICurrencyFormatter, CurrencyFormatter>();
        services.AddHostedService<NewsletterCampaignSchedulerService>();

        // Register MediatR handlers from this assembly (e.g. ResetPasswordOtpCommandHandler
        // which needs ApplicationUser and cannot live in the Application layer)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ResetPasswordOtpCommandHandler).Assembly));

        return services;
    }
}
