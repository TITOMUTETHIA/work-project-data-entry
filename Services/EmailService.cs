namespace WorkTicketApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            // In a real app, use SMTP or SendGrid here.
            // For now, we log the link to the console so you can test it.
            _logger.LogInformation("=================================================");
            _logger.LogInformation($"Sending Password Reset Email to: {email}");
            _logger.LogInformation($"Reset Link: {resetLink}");
            _logger.LogInformation("=================================================");
            
            return Task.CompletedTask;
        }
    }
}