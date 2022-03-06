using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using chargeReporting.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tibber.Sdk;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace chargeReporting.Models
{
    public class Zaptec
    {
        static WebClient _client;

        public static async Task MakeReport(IEnumerable<string> emailMappings, string user, string password, string from, string smtp, bool currentMonth, string emailtext)
        {
            string bToken = await GetToken(user, password);
            _client = new WebClient();
            _client.Headers.Add("accept: text/plain");
            _client.Headers.Add("Authorization: Bearer "+ bToken);

            var chargers = await GetChargers();

            List<PriceResult> priceResults = new List<PriceResult>();

            var today = DateTime.Now;
            DateOnly end = new DateOnly(today.Year, today.Month, 1);
            DateTime prevmonth = today.AddMonths(-1);
            DateOnly start = new DateOnly(prevmonth.Year, prevmonth.Month, 1);

            if (currentMonth)
            {
                start = new DateOnly(today.Year, today.Month, 1);
                end = new DateOnly(today.Year, today.Month, today.Day);
            }

            foreach (var d in chargers.Data)
            {
                PriceResult priceResult = await GetPriceResult(d.Id, start, end);
                
                if(priceResult.Price == 0) continue;

                priceResult.Name = d.Name;
                priceResults.Add(priceResult);
            }

            Email email = new Email(from, smtp, emailMappings, emailtext);

            //send summary email
            email.SendSummary(priceResults);

            //send individual emails
            email.SendBills(priceResults);
        }

        private static async Task<string> GetToken(string user, string password)
        {
            var c = new WebClient();
            c.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            string parameters = "username=" + user + "&password=" + password;
            string url = "https://api.zaptec.com/oauth/token";
            string auth = c.UploadString(url, parameters);
            return JsonSerializer.Deserialize<AuthToken>(auth).access_token;
        }

        private static async Task<Chargers> GetChargers()
        {
            string url = "https://api.zaptec.com/api/chargers?PageSize=6";
            var response = _client.DownloadString(url);
            return JsonSerializer.Deserialize<Chargers>(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chargerId"></param>
        private static async Task<PriceResult> GetPriceResult(string chargerId, DateOnly start, DateOnly end)
        {
            string url = "https://api.zaptec.com/api/chargehistory?ChargerId=" + chargerId +
                         "&From=" + start.ToString("yyyy-MM-dd")
                         + "T00%3A00%3A00Z&To=" + end.ToString("yyyy-MM-dd")
                         + "T00%3A00%3A00Z";

            var response = _client.DownloadString(url);

            var session = JsonSerializer.Deserialize<Chargehistory>(response);

            if (session == null) return new PriceResult();

            double totalKw = 0;
            double totalPrice = 0;
            foreach (ChargeHistoryData d in session.Data)
            {
                var signedSessionStr = d.SignedSession.Remove(0, 5);

                var jsonSerializerSettings = new JsonSerializerSettings()
                    {DateFormatString = "yyyy-MM-ddTHH:mm:ss,fffK R"};

                var signedSession =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<SignedSession>(signedSessionStr,
                        jsonSerializerSettings);

                //find hourly kw usage
                var hour = -1;
                double hourKw = 0;
                double kwSubtract = 0; //don't use previous kW hours already added
                foreach (var t in signedSession?.RD)
                {
                    if (hour == -1) 
                    {
                        hour = t.TM.Hour;
                        kwSubtract = Convert.ToDouble(t.RV, CultureInfo.InvariantCulture);
                    }

                    if (t.TM.Hour == hour)
                    {
                        continue;
                    }

                    //new hour, fetch db price and do calculations, reset hour kw counter
                    hourKw = Convert.ToDouble(t.RV, CultureInfo.InvariantCulture);
                    hourKw = hourKw - kwSubtract;

                    string strDate = t.TM.ToString("yyyyMMddHH");
                    DateTime queryDate = DateTime.ParseExact(strDate, "yyyyMMddHH", CultureInfo.CreateSpecificCulture("nb-NO"));
                    var hourPrice = await TibberService.getPrice(queryDate.AddHours(-1)) + GridRent.GetVariable(queryDate);
                    if (hourPrice>0)
                        totalPrice += hourPrice * hourKw; //TODO: add nettleie

                    totalKw += hourKw;

                    hour = t.TM.Hour;
                    kwSubtract = Convert.ToDouble(t.RV, CultureInfo.InvariantCulture);
                }

                //add price for last hour
                string strDateLast = signedSession.RD.Last().TM.ToString("yyyyMMddHH");
                DateTime queryDateLast = DateTime.ParseExact(strDateLast, "yyyyMMddHH", CultureInfo.CreateSpecificCulture("nb-NO"));
                var lastHourPrice = await TibberService.getPrice(queryDateLast);

                hourKw = Convert.ToDouble(signedSession.RD.Last().RV, CultureInfo.InvariantCulture) - kwSubtract;

                totalPrice += lastHourPrice * hourKw; //TODO: add nettleie
                totalKw += hourKw;
            }

            PriceResult res = new PriceResult();
            res.Price = totalPrice;
            res.TotalKw = totalKw;
            
            return res;
        }

        public class PriceResult
        {
            public double Price { get; set; }
            public double TotalKw { get; set; }
            public string? Name { get; set; }
        }

        public class Chargers
        {
            public int Pages { get; set; }
            public List<ChargerData> Data { get; set; }
        }

        public class AuthToken
        {
            public string access_token { get; set; }
        }

        public class ChargerData
        {
            public int OperatingMode { get; set; }
            public bool IsOnline { get; set; }
            public string Id { get; set; }
            public string MID { get; set; }
            public string DeviceId { get; set; }
            public string SerialNo { get; set; }
            public string Name { get; set; }
            public DateTime CreatedOnDate { get; set; }
            public string CircuitId { get; set; }
            public bool Active { get; set; }
            public int CurrentUserRoles { get; set; }
            public string Pin { get; set; }
            public int DeviceType { get; set; }
            public string InstallationName { get; set; }
            public string InstallationId { get; set; }
            public int AuthenticationType { get; set; }
            public bool IsAuthorizationRequired { get; set; }

        }

        public class Chargehistory
        {
            public int Pages { get; set; }
            public string Message { get; set; }
            public List<ChargeHistoryData> Data { get; set; }
        }

        public class ChargeHistoryData
        {
            public string Id { get; set; }
            public string DeviceId { get; set; }
            public DateTime StartDateTime { get; set; }
            public DateTime EndDateTime { get; set; }
            public double Energy { get; set; }
            public int CommitMetadata { get; set; }
            public string CommitEndDateTime { get; set; }
            public string UserFullName { get; set; }
            public string ChargerId { get; set; }
            public string DeviceName { get; set; }
            public string UserEmail { get; set; }
            public string UserId { get; set; }
            public string TokenName { get; set; }
            public string ExternalId { get; set; }
            public bool ExternallyEnded { get; set; }
            public List<EnergyDetails> EnergyDetails { get; set; }
            public ChargerFirmwareVersion ChargerFirmwareVersion { get; set; }
            public string SignedSession { get; set; }
            public string ReplacedBySessionId { get; set; }
        }

        public class EnergyDetails
        {
            public DateTime Timestamp { get; set; }
            public double Energy { get; set; }
        }

        public class ChargerFirmwareVersion
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Build { get; set; }
            public int Revision { get; set; }
            public int MajorRevision { get; set; }
            public int MinorRevision { get; set; }
        }

        public class SignedSession
        {
            public string FV { get; set; }
            public string GI { get; set; }
            public List<TimeSerie> RD { get; set; }
        }

        public class TimeSerie
        {
            public DateTime TM { get; set; }
            public string TX { get; set; }
            public string RV { get; set; }
            public string RI { get; set; }
            public string RU { get; set; }
            public string RT { get; set; }
            public string ST { get; set; }
        }


    }
}