using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using chargeReporting.Models;

namespace chargeReporting.Services
{
    public class Email
    {
        private string _from;
        private string _smtp;
        private IEnumerable<string> _emails;
        private string _emailtext;
        private string _smtpUser;
        private string _smtpPwd;
        private int _smtpPort;
        private int _months;
        private string _periodHeader;

        public Email(string from, string smtp, IEnumerable<string> emails, string emailtext, string smtpUser, string smtpPwd, int smtpPort, int months)
        {
            _from = from;
            _smtp = smtp;
            _emails = emails;
            _emailtext = emailtext;
            _smtpUser = smtpUser;
            _smtpPwd = smtpPwd;
            _smtpPort = smtpPort;
            _months = months;

            _periodHeader = DateTime.Now.AddMonths(-1).ToString("MMMM");

            if (months > 1)
                _periodHeader = DateTime.Now.AddMonths(Math.Abs(_months) * -1).ToString("MMMM") + " til og med " + DateTime.Now.AddMonths(-1).ToString("MMMM");
        }

        public void SendSummary(List<Zaptec.PriceResult> priceResults)
        {
            var summaryEmails = _emails.ToList().Where(e => e.StartsWith("summary"));
            string body = "";
            foreach (var p in priceResults)
            {
                body += p.Name + " | antall kWh: " + Math.Round(p.TotalKw, 0).ToString(CultureInfo.CreateSpecificCulture("nb-NO"))
                        + " | kostnad: " + Math.Round(p.Price, 0).ToString(CultureInfo.CreateSpecificCulture("nb-NO")) + ",-"
                        + " | strømstøtte: " + Math.Round(p.Subsidization, 0).ToString(CultureInfo.CreateSpecificCulture("nb-NO")) + ",-"
                        + " | å betale: " + Math.Round(p.Price - p.Subsidization, 0).ToString(CultureInfo.CreateSpecificCulture("nb-NO")) + ",-<br>";
            }

            foreach (var e in summaryEmails)
            {
                var email = e.Replace("summary->", "");
                SendEmail(email, body, "regnskap for strømforbruk lading: " + _periodHeader);
            }
        }

        public void SendBills(List<Zaptec.PriceResult> priceResults)
        {
            string body = "<html lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta charset=\"utf-8\" /><title>Elbil lading faktura</title></head><body>Fakturadato: " + DateTime.Now.ToShortDateString() + "<br>";
            foreach (var p in priceResults)
            {
                if (p.Name == null) continue;

                body = p.Name + "<br>kWh: " + Math.Round(p.TotalKw, 0).ToString()
                        + "<br>kostnad: " + Math.Round(p.Price, 0).ToString() + ",-"
                        + "<br>strømstøtten: " + Math.Round(p.Subsidization, 0).ToString() + ",-"
                        + "<br>periode: " + _periodHeader;
                //+ "<br>å betale: " + Math.Round(p.Price - p.Subsidization, 0).ToString() + ",-";

                body += @"<br><br><br><br><table style=""border: 0px solid black; width: 600px; border-spacing: 0; border-collapse: collapse; "">
<tr>
    <td colspan=""2"">Betalingsinformasjon</td>
    <td>Betalingsfrist</td>
</tr>
<tr>
    <td colspan=""2"">" + p.Name + @"<br /><br /><br /></td>
    <td>" + DateTime.Now.AddDays(10).ToString("dd.MM.yyyy") + @"<br><br><br></td>
</tr>
<tr style=""border: 0px solid black;"">
    <td style=""border: 0px solid black; border-left: 1px solid black;"">Kundeidentifikasjon (KID)</td>
    <td style=""border: 0px solid black; border-left: 1px solid black;"">Kroner</td>
    <td style=""border: 0px solid black; border-left: 1px solid black;"">Til konto</td>
</tr>
<tr style=""border: 0px solid black;"">
    <td style=""border: 0px solid black; border-left: 1px solid black;""></td>
    <td style=""border: 0px solid black; border-left: 1px solid black;"">" + Math.Round(p.Price - p.Subsidization, 0).ToString() + @"</td>
    <td style=""border: 0px solid black; border-left: 1px solid black;"">" + _emailtext + @"</td>
</tr>
</table>
</body>
</html>";

                var email = _emails.ToList().Where(e => e.IndexOf(p.Name) > -1).FirstOrDefault();
                if (email == null) continue;

                SendEmail(email.Remove(0, email.IndexOf("->") + 2), body, "Regning for elbil lading: " + _periodHeader);
            }
        }

        private void SendEmail(string email, string body, string subject)
        {
            MailMessage mail = new MailMessage()
            {
                From = new MailAddress(_from),
                Subject = subject,
                Body = body,
                Priority = MailPriority.Normal,
                IsBodyHtml = true
            };
            mail.To.Add(email);

            mail.Headers.Add("Message-Id",
                         String.Format("<{0}@{1}>",
                         Guid.NewGuid().ToString(),
                        _smtp));

            var smtpClient = new SmtpClient
            {
                Host = _smtp,
                Port = 25
            };

            if (_smtpPort != 0)
            {
                smtpClient.Port = _smtpPort;
            };

            if (_smtpUser != "")
            {
                smtpClient.Credentials = new NetworkCredential(_smtpUser, _smtpPwd);
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            }

            smtpClient.Send(mail);

        }
    }
}
