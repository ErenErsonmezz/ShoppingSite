using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ShoppingSite.Email
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client=new SendGridClient(Options.SendGridKey);
            var message = new SendGridMessage()
            {
                From=new EmailAddress("erenersonmez619@outlook.com","TeknotikShop"),
                Subject=subject,
                PlainTextContent=htmlMessage,
                HtmlContent=htmlMessage
            };
            message.AddTo(new EmailAddress(email));
            try
            {
                return client.SendEmailAsync(message);
            }
            catch 
            {
                throw;
            }
            return null;
        }
        public EmailOptions Options { get; set; }
        public EmailSender(IOptions<EmailOptions> options)
        {
            Options=options.Value;
        }
    }
}
