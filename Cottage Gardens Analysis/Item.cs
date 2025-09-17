using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Cottage_Gardens_Analysis
{
    public class Item : IEquatable<Item>
    {
        public string Nbr { get; set; }
        public string Desc { get; set; }
        public GenusSize GenusSize { get; set; }
        public Group Group { get; set; }
        public string Tag { get; set; }
        public string ItemProgram { get; set; }
        public byte Zone { get; set; }
        public int Multiple { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public HashSet<Store> DoNotShip { get; set; }
        public Dictionary<Store, int> Allocation { get; set; }

        private int _totalQty;
        private double _historicalSales;


        // Item, Item Description, Size,    Inactive, Category,    Tag Code,    Program, Zone,    GROUP, GENUS SIZE, GENUS
        public Item(string nbr, string desc, GenusSize genusSize, Group group, string tag, string program, byte zone, int multiple)
        {
            Nbr = nbr;
            Desc = desc;
            GenusSize = genusSize;
            Group = group;
            Tag = tag;
            ItemProgram = program;
            Zone = zone;
            Multiple = multiple;
            History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
            DoNotShip = null;
            _totalQty = -1;
            _historicalSales = -1;
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            if (DoNotShip == null)
            {
                DoNotShip = doNotShip;
            }
        }

        public void Allocate(Dictionary<Store, double> index)
        {
            Dictionary<Store, double> storeIndex = index;
            var dns = from x in index.Keys where DoNotShip.Contains(x) select x;
            if (dns.Any())
            {
                storeIndex = new Dictionary<Store, double>();
                double sum = 0;
                foreach (KeyValuePair<Store, double> kvp in index)
                {
                    if (!DoNotShip.Contains(kvp.Key) && kvp.Key.WeatherZone >= Zone)
                    {
                        storeIndex.Add(kvp.Key, kvp.Value);
                        sum += kvp.Value;
                    }
                }
                foreach (Store store in storeIndex.Keys)
                {
                    storeIndex[store] /= sum;
                }
            }
            Allocation = new Dictionary<Store, int>();
            int totalQty = TotalQty;
            List<StoreResidual> residuals = new List<StoreResidual>();
            int totalAllocated = 0;
            foreach (KeyValuePair <Store, double> kvp in storeIndex)
            {
                int floor = Multiple * (int)Math.Floor(totalQty * kvp.Value / Multiple);
                double residual = totalQty * kvp.Value - floor;
                Allocation.Add(kvp.Key, floor);
                totalAllocated += floor;
                residuals.Add(new StoreResidual(kvp.Key, residual));
            }
            if (totalAllocated < TotalQty) 
            {
                residuals.Sort();
                for (int i = 0; i < residuals.Count; i++)
                {
                    Allocation[residuals[i].Store] += Multiple;
                    totalAllocated += Multiple;
                    if (totalAllocated == totalQty)
                    {
                        break;
                    }
                }
            }
        }

        public int TotalQty
        {
            get
            {
                if (_totalQty < 0)
                {
                    _totalQty = 0;
                    foreach (KeyValuePair<Store, Metrics> kvp in Benchmark)
                    {
                        if (!DoNotShip.Contains(kvp.Key) && kvp.Key.WeatherZone >= Zone)
                        {
                            _totalQty += kvp.Value.QtyDelivered;
                        }
                    }
                    _totalQty = Multiple * (int)Math.Round((double)_totalQty / Multiple, 0);
                }
                return _totalQty;
            }
        }

        public double HistoricalSales
        {
            get
            {
                if (_historicalSales < 0)
                {
                    _historicalSales = 0;
                    for (int i = 0; i < Program.HistoryYears.Length; i++)
                    {
                        if (History[i] != null)
                        {
                            double sum = 0;
                            foreach (Metrics metrics in History[i].Values)
                            {
                                sum += metrics.DollarSold;
                            }
                            _historicalSales += sum * (double) Program.HistoryYears[i].weight;
                        }
                    }
                    _totalQty = Multiple * (int)Math.Round((double)_totalQty / Multiple, 0);
                }
                return _totalQty;
            }
        }


        public override int GetHashCode()
        {
            return Nbr.GetHashCode();
        }

        public bool Equals(Item other)
        {
            return this.Nbr == other.Nbr;
        }
    }
}
