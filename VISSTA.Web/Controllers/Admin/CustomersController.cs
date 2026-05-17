using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VISSTA.Infrastructure.Identity;
using VISSTA.Web.Models;

namespace VISSTA.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/customers")]
public sealed class CustomersController(UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var customers = userManager.Users
            .OrderBy(x => x.FullName)
            .Select(x => new ProfileViewModel
            {
                FullName = x.FullName,
                Email = x.Email ?? string.Empty,
                PhoneNumber = x.PhoneNumber ?? string.Empty
            })
            .ToList();

        return View("~/Views/Admin/Customers/Index.cshtml", new AdminCustomersViewModel(customers));
    }
}
