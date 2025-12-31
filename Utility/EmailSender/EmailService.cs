using System.Net;
using System.Net.Mail;

namespace Utility.EmailSender
{
    public class EmailService : IEmailService
    {
        private readonly string _senderEmail;
        private readonly string _senderPassword;
        private readonly string _smtpHost;
        private readonly int _smtpPort;

        public EmailService(string senderEmail, string senderPassword, string smtpHost = "smtp.office365.com", int smtpPort = 587)
        {
            _senderEmail = senderEmail;
            _senderPassword = senderPassword;
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(_senderEmail);
                mailMessage.To.Add(to);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;

                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
    }
}





