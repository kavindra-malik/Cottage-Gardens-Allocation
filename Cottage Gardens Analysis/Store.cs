using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    internal class Store : IEquatable<Store>
    {
        public string Account { get; set; }
        public int Nbr { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Market { get; set; }
        public string Group { get; set; }
        public string Buyer { get; set; }
        public string Rank { get; set; }
        public int WeatherZone { get; set; }


        public Store(int nbr, int market, string name = null, string city = null, string state = null, string group = null, string buyer = null, string rank = null, int weatherZone = 0) 
        {
            Account = "Home Depot";
            Nbr = nbr;
            Market = market;
            Name = name;
            City = city;
            State = state;
            Group = group;
            Buyer = buyer;
            Rank = rank;
            WeatherZone = weatherZone;
        }

        public bool Equals(Store other)
        {
            return this.Nbr == other.Nbr;
        }
    }
}
