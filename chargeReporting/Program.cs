using System;
using System.Globalization;
using chargeReporting.Models;
using chargeReporting.Services;
using CommandLine;

namespace chargeReporting
{
    class Program
    {
        public class Options
        {
            [Option('d', "db", Required = true, HelpText = "mysql db connection string to store prices from tibber")]
            public string MySqlConnectionString { get; set; }

            [Option('p', "prices", Required = false, HelpText = "get todays prices from tibber graphql api, when this parameter specified it will only run price fetch")]
            public bool GetPrices { get; set; }

            [Option('e', "emails",Required = false, HelpText = "comma separted list, map zaptec name to email. for summary emails see example. \"P1 - John Doe->xyz99@something.com\",\"summary->admin@something.com\"")]
            public IEnumerable<string> EmailMappings { get; set; }

            [Option('f', "from", Required = false, HelpText = "email from address")]
            public string EmailFrom { get; set; }
            
            [Option('s', "smtp", Required = false, HelpText = "smtp server")]
            public string Smtp { get; set; }

            [Option('u', "user", Required = false, HelpText = "zaptec user")]
            public string ZaptecUser { get; set; }

            [Option('z', "zaptecpassword", Required = false, HelpText = "zaptec password")]
            public string ZaptecPassword { get; set; }

            [Option('t', "tibberkey", Required = false, HelpText = "tibber api key")]
            public string TibberKey { get; set; }

        }

        static async Task Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("nb-NO", false);

            try
            {
                Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    Db._connectionstring = o.MySqlConnectionString;

                    if (o.GetPrices)
                    {
                        TibberService.SaveTodaysPrices(o.TibberKey).Wait();
                        return;
                    }

                    Zaptec.MakeReport(o.EmailMappings, o.ZaptecUser, o.ZaptecPassword, o.EmailFrom, o.Smtp).Wait();

                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}