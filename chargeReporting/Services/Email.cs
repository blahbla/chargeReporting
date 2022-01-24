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

        public Email(string from, string smtp, IEnumerable<string> emails)
        {
            _from = from;
            _smtp = smtp;
            _emails = emails;
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
            var billEmails = _emails.ToList().Where(e => !e.StartsWith("summary"));

            string body = "";
            foreach (var p in priceResults)
            {
                body += p.Name + ", antall kWh: " + Math.Round(p.TotalKw, 2).ToString()
                        + ", kostnad: " + Math.Round(p.Price, 2).ToString() + ",-" + System.Environment.NewLine;
            }

            foreach (var e in billEmails)
            {
                var email = e.Remove(0, e.IndexOf("->")+2);
                SendEmail(email, body, "regning for strømforbruk på elbil lading: "+DateTime.Now.AddMonths(-1).ToString("MMMM"));
            }
        }

        private void SendEmail(string email, string body, string subject)
        {
            var smtpClient = new SmtpClient(_smtp)
            {
                Port = 25,
                //Credentials = new NetworkCredential("email", "password"),
                //EnableSsl = true,
            };

            smtpClient.Send(_from, email, subject, body);

        }
    }
}
