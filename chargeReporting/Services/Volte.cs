using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace chargeReporting.Services
{
    public class Volte
    {
        static WebClient _client;

        public Volte(string voltekey)
        {
            _client = new WebClient();
            _client.Headers.Add("accept: */*");
            _client.Headers.Add("Authorization: Token " + voltekey);
        }

        public async Task<List<VoltePrice>> GetSpotPrices(DateOnly from, DateOnly to)
        {
            string url = "https://volte-web-prod.azurewebsites.net/api/prices/spot/?price_area=NO1&start="
                         + from.ToString("yyyy-MM-dd") + "T00%3A00&end="
                         + to.ToString("yyyy-MM-dd") + "T00%3A00";
            string response = _client.DownloadString(url);

            JArray deserializeObject = JsonConvert.DeserializeObject<JArray>(response);

            List<VoltePrice> voltePrices = new List<VoltePrice>();
            //double volteMarkup = GetVolteMarkup();

            foreach (JToken jToken in deserializeObject)
            {
                VoltePrice price = new VoltePrice();
                if (jToken.First != null) price.date = jToken.First.ToObject<DateTime>();
                if (jToken.Last != null) price.spot = jToken.Last.ToObject<double>()/1000;
                if (jToken.Last != null) price.total = (jToken.Last.ToObject<double>() / 1000) * 1.25; // + volteMarkup;

                voltePrices.Add(price);
            }

            return voltePrices;
        }

        //seems to be already included
        public double GetVolteMarkup()
        {
            //TODO: maybe later, for now fixed value
            //string url = "https://volte-web-prod.azurewebsites.net/api/prices/markup?price_list_id=Volte_2021";
            //string response = _client.DownloadString(url);
            //JsonSerializer.Deserialize<VolteMarkup>(response);
            return 0.008;
        }

        public class VoltePrice
        {
            public DateTime date { get; set; }
            public double spot { get; set; }
            public double total { get; set; }
        }

        public class VolteMarkup
        {
            public string name  { get; set; }
            public double value { get; set; }
            public DateTime start_datetime { get; set; }
            public DateTime end_datetime { get; set; }
        }
    }
}
