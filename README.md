# VISSTA — ASP.NET Core MVC E-Commerce

VISSTA has been migrated from a React/Vite/Tailwind landing page into a Clean Architecture ASP.NET Core 8 MVC e-commerce application. The MVC home page preserves the original quiet luxury visual system: navy, gold, cream, glassmorphism, editorial imagery, and Framer Motion-style reveal animations implemented with CSS and `IntersectionObserver`.

## Solution Structure

```text
VISSTA.sln
VISSTA.Domain/            Entities, value objects, enums, domain events
VISSTA.Application/       CQRS requests, handlers, DTOs, validators, ports
VISSTA.Infrastructure/    EF Core, Identity, repositories, services, seed data
VISSTA.Web/               MVC controllers, views, view models, CSS, JS, assets
tests/
  VISSTA.UnitTests/
  VISSTA.IntegrationTests/
```

## Tech Stack

- ASP.NET Core 8 MVC
- Entity Framework Core 8 with SQL Server
- ASP.NET Core Identity with `ApplicationUser`
- MediatR CQRS
- FluentValidation
- AutoMapper
- Repository + Unit of Work
- MailKit SMTP email service
- Mock payment service
- Local file storage under `wwwroot/uploads`
- Vanilla JS progressive enhancement for cart, search, newsletter, and animations

## Run Locally

```bash
dotnet restore VISSTA.sln
dotnet build VISSTA.sln
dotnet ef database update --project VISSTA.Infrastructure --startup-project VISSTA.Web
dotnet run --project VISSTA.Web
```

The default connection string is in `VISSTA.Web/appsettings.json`:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=VISSTA;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

If LocalDB is unavailable, change this to a reachable SQL Server instance before running `dotnet ef database update`.

## Database

Initial migration:

```bash
dotnet ef migrations add InitialCreate --project VISSTA.Infrastructure --startup-project VISSTA.Web --output-dir Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project VISSTA.Infrastructure --startup-project VISSTA.Web
```

Seed data includes:

- Roles: `Admin`, `Customer`
- Admin user: `admin@vissta.com` / `Admin@123!`
- Categories: Women's, Men's, Accessories
- 12 VISSTA products with image paths and EGP prices

Set `"Database:InitializeOnStartup": true` in `appsettings.json` if you want the app to run migrations and seed roles/admin during startup.

## Main Routes

- `/` — VISSTA home page
- `/shop` — AJAX-filterable catalog
- `/shop/{slug}` — product detail page
- `/cart` — cart
- `/checkout` — authenticated checkout
- `/account/login`, `/account/register`, `/account/profile`, `/account/orders`
- `/admin/dashboard`, `/admin/products`, `/admin/orders`, `/admin/customers`

AJAX endpoints:

```text
POST /api/cart/add
POST /api/cart/remove
PUT  /api/cart/update
GET  /api/cart/count
GET  /api/search?q=term
POST /api/newsletter
```

## Verification

```bash
dotnet build VISSTA.sln
dotnet test VISSTA.sln --no-build
```

Both commands currently pass. Database update requires a working SQL Server/LocalDB instance.
