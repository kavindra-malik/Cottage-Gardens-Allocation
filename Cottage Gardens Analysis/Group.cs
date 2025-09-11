using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Group : IEquatable<Group>
    {
        public Category Cat { get; set; }
        public string Name { get; set; }
        public Dictionary<string, Item> Items { get; set; }


        public Group(Category cat, string name)
        {
            Cat = cat;
            Name = name;
            Items = new Dictionary<string, Item>();
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var item in Items.Values)
            {
                item.UpdateDoNotShip(doNotShip);
            }
        }

        public void AllocateGroupItems()
        {
            Dictionary<Store, double> basis = new Dictionary<Store, double>();
            Dictionary<Store, decimal> cumulativeWeight = new Dictionary<Store, decimal>();
            Dictionary<Store, double> index = new Dictionary<Store, double>();

            foreach (var item in Items.Values)
            {
                for (int i = 0; i < Program.HistoryYears.Length; i++)
                {
                    if (item.History[i] != null)
                    {
                        foreach (KeyValuePair<Store, Metrics> kvp in item.History[i])
                        {
                            if (!basis.ContainsKey(kvp.Key))
                            {
                                basis.Add(kvp.Key, (kvp.Value.DollarDelivered + kvp.Value.DollarSold) * (double)Program.HistoryYears[i].weight);
                                cumulativeWeight.Add(kvp.Key, Program.HistoryYears[i].weight);
                            }
                            else
                            {
                                basis[kvp.Key] += (kvp.Value.DollarDelivered + kvp.Value.DollarSold) * (double) Program.HistoryYears[i].weight;
                                cumulativeWeight[kvp.Key] += Program.HistoryYears[i].weight;
                            }
                        }
                    }
                }
            }
            double sum = 0;
            foreach (Store store in basis.Keys)
            {
                if (cumulativeWeight[store] > 0 && cumulativeWeight[store] != 1)
                {
                    basis[store] /= (double) cumulativeWeight[store];
                }
                sum += basis[store];
            }
            foreach (KeyValuePair<Store, double> kvp in basis)
            {
                index.Add(kvp.Key, kvp.Value / sum);
            }

            foreach (Item item in Items.Values)
            {
                item.Allocate(index);
            }
        }


        public bool Equals(Group other)
        {
            return this.Cat.Equals(other.Cat) && this.Name.Equals(other.Name);
        }
    }
}
