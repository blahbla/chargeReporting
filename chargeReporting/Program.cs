﻿using System;
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
            [Option('d', "db", Required = false, HelpText = "mysql db connection string to store prices from tibber")]
            public string MySqlConnectionString { get; set; }

            [Option('p', "prices", Required = false, HelpText = "get todays prices from tibber graphql api, when this parameter specified it will only run price fetch")]
            public bool GetPrices { get; set; }

            [Option('e', "emails",Required = false, HelpText = "space separted list, map zaptec name to email. for summary emails see example. \"P1 - John Doe->xyz99@something.com\" \"summary->admin@something.com\"")]
            public IEnumerable<string> EmailMappings { get; set; }

            [Option('f', "from", Required = false, HelpText = "email from address")]
            public string EmailFrom { get; set; }
            
            [Option('s', "smtp", Required = false, HelpText = "smtp server")]
            public string Smtp { get; set; }

            [Option('m', "smtpuser", Required = false, HelpText = "smtp username")]
            public string SmtpUser { get; set; } = "";

            [Option('w', "smtppwd", Required = false, HelpText = "smtp password")]
            public string SmtpPassword { get; set; } = "";

            [Option('l', "smtpport", Required = false, HelpText = "smtp tcl port")]
            public int SmtpPort { get; set; } = 0;

            [Option('u', "user", Required = false, HelpText = "zaptec user")]
            public string ZaptecUser { get; set; }

            [Option('z', "zaptecpassword", Required = false, HelpText = "zaptec password")]
            public string ZaptecPassword { get; set; }

            [Option('t', "tibberkey", Required = false, HelpText = "tibber api key")]
            public string TibberKey { get; set; }

            [Option('c', "currentmonth", Required = false, HelpText = "get reports of current month instead of previous month")]
            public bool CurrentMonth { get; set; }

            [Option('n', "numberofmonths", Required = false, HelpText = "get reports of specified number of months counting from the previous full month")]
            public int NumberOfMonths { get; set; } = 1;

            [Option('a', "addemailtxt", Required = false, HelpText = "Bank account number for bills")]
            public string EmailText { get; set; }

            [Option('g', "grid", Required = false, Default = false, HelpText = "Add grid rent, default false")]
            public bool GridRent { get; set; }

            [Option('v', "volte", Required = false, Default = "", HelpText = "Volte api key")]
            public string Volte { get; set; }

            [Option('b', "subsidization", Required = false, Default = "", HelpText = "Power subsidization percentage from norwegian government")]
            public int Subsidization { get; set; }

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
                        Services.Tibber.SaveTodaysPrices(o.TibberKey).Wait();
                        return;
                    }

                    Zaptec.MakeReport(o.EmailMappings, o.ZaptecUser, o.ZaptecPassword, o.EmailFrom, o.Smtp, o.SmtpUser, o.SmtpPassword, o.SmtpPort, o.CurrentMonth, o.EmailText, o.GridRent, o.Volte, o.Subsidization, o.NumberOfMonths).Wait();

                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}