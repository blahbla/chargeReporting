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

        public Email(string from, string smtp, IEnumerable<string> emails, string emailtext)
        {
            _from = from;
            _smtp = smtp;
            _emails = emails;
            _emailtext = emailtext;
        }

        public void SendSummary(List<Zaptec.PriceResult> priceResults)
        {
            var summaryEmails = _emails.ToList().Where(e => e.StartsWith("summary"));
            string body = "";
            foreach (var p in priceResults)
            {
                body += p.Name + ", antall kWh: " + Math.Round(p.TotalKw, 2).ToString(CultureInfo.CreateSpecificCulture("nb-NO")) 
                        + ", kostnad: " + Math.Round(p.Price, 2).ToString(CultureInfo.CreateSpecificCulture("nb-NO")) + ",-" + System.Environment.NewLine;
            }

            foreach (var e in summaryEmails)
            {
                var email = e.Replace("summary->", "");
                SendEmail(email, body, "regnskap for strømforbruk lading: "+DateTime.Now.AddMonths(-1).ToString("MMMM"));
            }
        }

        public void SendBills(List<Zaptec.PriceResult> priceResults)
        {
            string body = "";
            foreach (var p in priceResults)
            {
                if(p.Name==null) continue;

                body = p.Name + ", antall kWh: " + Math.Round(p.TotalKw, 0).ToString()
                        + ", kostnad: " + Math.Round(p.Price, 0).ToString() + ",-" + System.Environment.NewLine;

                body += System.Environment.NewLine + System.Environment.NewLine + _emailtext;

                var email = _emails.ToList().Where(e => e.IndexOf(p.Name)>-1).FirstOrDefault();
                if(email == null) continue;

                SendEmail(email.Remove(0, email.IndexOf("->") + 2), body, "regning for strømforbruk på elbil lading: "+DateTime.Now.AddMonths(-1).ToString("MMMM"));
            }
        }

        private void SendEmail(string email, string body, string subject)
        {
            MailMessage mail = new MailMessage()
            {
                From = new MailAddress(_from),
                Subject = subject,
                Body = body,
                Priority = MailPriority.Normal
            };
            mail.To.Add(email);

            mail.Headers.Add("Message-Id",
                         String.Format("<{0}@{1}>",
                         Guid.NewGuid().ToString(),
                        _smtp));

            var smtpClient = new SmtpClient(_smtp)
            {
                Port = 25,
                //Credentials = new NetworkCredential("email", "password"),
                //EnableSsl = true,               
            };

            smtpClient.Send(mail);

        }
    }
}
