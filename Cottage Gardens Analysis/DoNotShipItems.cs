using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class DoNotShipItems
    {
        public double[] Weight { get; set; }
        public HashSet<Item> Items { get; set; }
        public DoNotShipItems() 
        { 
            Weight = new double[Program.HistoryYears.Length];
            Items = new HashSet<Item>();
        }

        public void AddItem(Item item)
        {
            Items.Add(item);
            for (int i = 0; i < Program.HistoryYears.Length; i++)
            {
                Weight[i] += item.TotalSold(i);
            }
        }
    }
}
