using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chargeReporting.Services;
using Dapper;
using MySqlConnector;

namespace chargeReporting
{
    public static class Db
    {
        public static string _connectionstring = "";
        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionstring);
        }

       
    }
}
