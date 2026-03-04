using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace barberShop
{
    public class EmailBeallitasok
    {
        public string Host { get; set; } = "";
        public int Port {  get; set; }
        public bool EnableSsl { get; set; }
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public interface IEmailKuldo
    {
        Task SendAsync(string kinek, string targy, string body);
    }

    public class SmtpEmailKuldo : IEmailKuldo
    {
        private readonly EmailBeallitasok _beallitasok;

        public SmtpEmailKuldo(IOptions<EmailBeallitasok> options)
        {
            _beallitasok=options.Value;
        }

        public async Task SendAsync(string kinek, string targy,string body)
        {
            using var client = new SmtpClient(_beallitasok.Host, _beallitasok.Port)
            {
                EnableSsl = _beallitasok.EnableSsl,
                Credentials = new NetworkCredential(_beallitasok.User, _beallitasok.Password)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_beallitasok.User, "BestBarberShop"),
                Subject = targy,
                Body=body,
                IsBodyHtml=false
            };
            mail.To.Add(kinek);

            await client.SendMailAsync(mail);
        }
    }
}
