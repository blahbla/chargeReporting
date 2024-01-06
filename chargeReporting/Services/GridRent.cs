using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace chargeReporting.Services
{
    public class GridRent
    {
        public static double GetVariable(DateTime d)
        {
//Energiledd, øre / kWh
//Dag*
//januar - mars: 39,59
//april - desember: 48,25
//Natt / helg *
//januar - mars: 32,09
//april - desember: 40,75

//* Perioder
//Dag: Hverdager fra 06:00 til 22:00
//Natt: Hverdager fra 22:00 til 06:00
//Helg: Lørdag og søndag samt helligdager

            //nighttime or daytime
            bool isNight = d.Hour > 22 || d.Hour < 6;
            bool isWeekend = d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
            bool isNightOrWeekend = isNight || isWeekend;


            if (d.Month >= 1 && d.Month <= 3)
                return isNightOrWeekend ? 0.3209 : 0.3959;

            if (d.Month >= 4 && d.Month <= 12)
                return isNightOrWeekend ? 0.4075 : 0.4825;

            return 0.3209;
        }
    }
}
