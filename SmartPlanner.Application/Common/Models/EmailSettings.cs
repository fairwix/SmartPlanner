namespace SmartPlanner.Application.Common.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 25;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Smart Planner";
        public bool UseSsl { get; set; } = true;
        public bool RequiresAuthentication { get; set; } = false;
        public string? Username { get; set; }
        public string? Password { get; set; }


        public bool UseFileSystem { get; set; } = true;
        public string FileSystemPath { get; set; } = "Emails";
    }
}
