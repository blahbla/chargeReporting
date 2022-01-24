using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MySqlConnector;
using Tibber.Sdk;
using Dapper;

namespace chargeReporting.Services
{
    public class TibberService
    {
        public static async Task SaveTodaysPrices(string key)
        {
            var client = new TibberApiClient(key);

            var basicData = await client.GetBasicData();
            var homeId = basicData.Data.Viewer.Homes.First().Id.Value;
            var consumption = await client.GetHomeConsumption(homeId, EnergyResolution.Monthly);

            var customQueryBuilder =
                new TibberQueryBuilder()
                    .WithAllScalarFields()
                    .WithViewer(
                        new ViewerQueryBuilder()
                            .WithAllScalarFields()
                            //.WithAccountType()
                            .WithHome(
                                new HomeQueryBuilder()
                                    .WithAllScalarFields()
                                    .WithAddress(new AddressQueryBuilder().WithAllFields())
                                    .WithCurrentSubscription(
                                        new SubscriptionQueryBuilder()
                                            .WithAllScalarFields()
                                            //.WithSubscriber(new LegalEntityQueryBuilder().WithAllFields())
                                            .WithPriceInfo(new PriceInfoQueryBuilder().WithToday(new PriceQueryBuilder().WithAllFields()))
                                    )
                                    //.WithOwner(new LegalEntityQueryBuilder().WithAllFields())
                                    //.WithFeatures(new HomeFeaturesQueryBuilder().WithAllFields())
                                    //.WithMeteringPointData(new MeteringPointDataQueryBuilder().WithAllFields())
                                    ,
                                homeId
                            )
                    );

            var customQuery = customQueryBuilder.Build(); // produces plain GraphQL query text
            var result = await client.Query(customQuery);

            ICollection<Price> priceInfoToday = result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Today;

            await using MySqlConnection conn = Db.GetConnection();

            foreach (var price in priceInfoToday)
            {
                string sql = @" 
                    Insert into charge_tibber_price (total, energy, tax, startsAt) values (@total, @energy, @tax, @startsAt)
                ";
                try
                {
                    await conn.ExecuteAsync(sql, new
                    {
                        total = price.Total,
                        energy = price.Energy,
                        tax = price.Tax,
                        startsAt = price.StartsAt
                    });
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                    //throw;
                }
            }
            //priceInfoToday.First().Total;
        }

        public static async Task<double> getPrice(DateTime time)
        {
            await using MySqlConnection conn = Db.GetConnection();

            string sql = @" 
                    Select * from charge_tibber_price where startsat=@time
                ";
            TibberPrice tp = new TibberPrice();
            try
            {
                tp = await conn.QueryFirstOrDefaultAsync<TibberPrice>(sql, new
                {
                    time
                });
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (tp == null) return 0;
            return tp.total;
        }

        public class TibberPrice
        {
            public DateTime startsAt { get; set; }
            public double energy  { get; set; }
            public double total  { get; set; }
            public double tax  { get; set; }
        }
    }
}
