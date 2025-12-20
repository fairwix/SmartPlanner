namespace SmartPlanner.Application.Common.Models
{
    public class AppSettings
    {
        public string AppName { get; set; } = "Smart Planner";
        public string BaseUrl { get; set; } = "https://smartplanner.com";
        public FrontendUrls FrontendUrls { get; set; } = new();
        public JwtSettings Jwt { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
    }

    public class FrontendUrls
    {
        public string LoginUrl { get; set; } = "/login";
        public string DashboardUrl { get; set; } = "/dashboard";
        public string ResetPasswordUrl { get; set; } = "/reset-password";
        public string ConfirmEmailUrl { get; set; } = "/confirm-email";
    }

    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "SmartPlanner";
        public string Audience { get; set; } = "SmartPlannerClients";
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
