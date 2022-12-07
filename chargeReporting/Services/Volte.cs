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
        public static double AvgPrice { get; set; }

        public Volte(string voltekey)
        {
            _client = new WebClient();
            _client.Headers.Add("accept: */*");
            _client.Headers.Add("Authorization: Token " + voltekey);
        }

        public async Task<List<VoltePrice>> GetSpotPrices(DateOnly from, DateOnly to)
        {
            //https://api.volte.no/api/v2/prices/spot/?price_areas=NO1&aggregator=NONE&resolution_unit=HOUR&resolution_value=0&start=2022-10-25T00:00&end=2022-12-01T00:00
            string url = "https://api.volte.no/api/v2/prices/spot/?price_areas=NO1&aggregator=NONE&resolution_unit=HOUR&resolution_value=0&start="
                         + from.ToString("yyyy-MM-dd") + "T00%3A00&end="
                         + to.ToString("yyyy-MM-dd") + "T00%3A00";
            string response = _client.DownloadString(url);

            var deserializeObject = JsonConvert.DeserializeObject<JObject>(response);

            List<VoltePrice> voltePrices = new List<VoltePrice>();
            //double volteMarkup = GetVolteMarkup();

            foreach (JToken jToken in deserializeObject["response"][0]["data"])
            {
                VoltePrice price = new VoltePrice();
                if (jToken["timestamp"] != null) price.date = jToken["timestamp"].ToObject<DateTime>();
                if (jToken["value"] != null) price.spot = jToken["value"].ToObject<double>()/1000;
                if (jToken["value"] != null) price.total = (jToken["value"].ToObject<double>() / 1000) * 1.25; // + volteMarkup;

                voltePrices.Add(price);
            }

            //get average from list object
            AvgPrice = voltePrices.Average(x => x.spot);

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
            public double subsidization { get; set; }
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
