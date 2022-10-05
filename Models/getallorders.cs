using System;
namespace toKinitoC_Api.Models
{
    public class getallorders
    {
        public string? serial { get; set; }
        public string? purchaseDate { get; set; }   //Ημερομηνία Αγοράς
        public string? arParast { get; set; }       //Αριθμός παραστικού αγοράς
        public string? name { get; set; }           //Προμηθευτής
        public string? SaleDate { get; set; }       //Ημερομηνία πώλησης
        public string? arPar { get; set; }          //Αριθμός παραστατικού πώλησης
        public string? Customer { get; set; }       //Πελάτης

    }
}

