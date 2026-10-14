namespace SmartPlanner.Application.Common.Models
{
    public class RateLimitSettings
    {
        public bool EnableRateLimiting { get; set; } = true;
        public int PermitLimit { get; set; } = 10;
        public int WindowSeconds { get; set; } = 60;
        public int QueueLimit { get; set; } = 2;

        public RateLimitRule LoginRateLimit { get; set; } = new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "1m",
            Limit = 5
        };

        public RateLimitRule RegistrationRateLimit { get; set; } = new RateLimitRule
        {
            Endpoint = "POST:/api/auth/register",
            Period = "1h",
            Limit = 3
        };

        public RateLimitRule PasswordResetRateLimit { get; set; } = new RateLimitRule
        {
            Endpoint = "POST:/api/auth/forgot-password",
            Period = "1h",
            Limit = 3
        };
    }

    public class RateLimitRule
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public long Limit { get; set; }
    }
}
