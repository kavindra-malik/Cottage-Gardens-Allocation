using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class DoNotShipItems
    {
        public double SalesWeight { get; set; }
        public HashSet<Item> Items { get; set; }
        public DoNotShipItems() 
        { 
            SalesWeight = 0;
            Items = new HashSet<Item>();
        }

    }
}
