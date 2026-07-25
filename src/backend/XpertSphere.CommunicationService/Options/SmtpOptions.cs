namespace XpertSphere.CommunicationService.Options;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@xpertsphere.local";
    public bool EnableSsl { get; set; } = false;
}
