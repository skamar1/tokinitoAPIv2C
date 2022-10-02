using System;
namespace toKinitoC_Api.Models
{
    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public int adminLevel { get; set; }
        public string remarks { get; set; }
    }
}

