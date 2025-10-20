using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Allocation
{
    public class Store : IEquatable<Store>
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
        public byte WeatherZone { get; set; }


        public Store(int nbr, int market, string name = null, string city = null, string state = null, string group = null, string buyer = null, string rank = null, byte weatherZone = 3) 
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

        public string Detail
        {
            get
            {
                return Market + "," + Nbr + "," + Name + "," + City + "," + State + "," + WeatherZone + "," + Rank;
            }
        }

        public static string Header
        {
            get
            {
                return "Market, Store Nbr, Store Name, City, State,Weather Zone, Rank";
            }
        }


        public bool Equals(Store other)
        {
            if (other == null) return false;
            return this.Nbr == other.Nbr;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Nbr, Account); ;
        }

        public override bool Equals(object obj)
        {
            return obj is Store store && Equals(store);
        }
    }
}
