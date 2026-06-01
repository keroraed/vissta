namespace VISSTA.Infrastructure.Settings;

public sealed class EmailSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "hello@vissta.com";
    public string FromName { get; set; } = "VISSTA";
    public bool UseSsl { get; set; }
}

public sealed class SiteSettings
{
    public string PublicBaseUrl { get; set; } = "https://vissta.com";
}

public sealed class StorageSettings
{
    public string UploadRoot { get; set; } = "uploads";
}

public sealed class PaymentSettings
{
    public bool AlwaysApprove { get; set; } = true;
}
