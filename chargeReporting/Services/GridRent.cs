using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chargeReporting.Services
{
    public class GridRent
    {
        public static double GetVariable(DateTime d)
        {
            //nighttime or daytime
            return d.Hour is > 22 or < 6 ? 0.311 : 0.417;
        }
    }
}
