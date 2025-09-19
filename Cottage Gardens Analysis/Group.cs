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
        public bool HasSales { get; set; }
        public Dictionary<string, Item> Items { get; set; }

        public Group(Category cat, string name)
        {
            Cat = cat;
            Name = name;
            HasSales = false;
            Items = new Dictionary<string, Item>();
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var item in Items.Values)
            {
                item.UpdateDoNotShip(doNotShip);
            }
        }

        #region
        public void AllocateGroupItems()
        {
            Dictionary<Store, DoNotShipItems> dnsItemsByStore = new Dictionary<Store, DoNotShipItems>();

            foreach (var item in Items.Values)
            {
                foreach (Store store in item.DoNotShip)
                {
                    if (!dnsItemsByStore.TryGetValue(store, out  var doNotShip))
                    {
                        doNotShip = new DoNotShipItems();
                        dnsItemsByStore.Add(store, doNotShip);
                    }
                    doNotShip.AddItem(item);
                }
            }
            AllocationIndex index = GetCompositeIndex(dnsItemsByStore);
            foreach (var item in Items.Values.Where(x => x.TotalQty > 0))
            {
                item.Allocate(index);
            }

        }
        #endregion



        private AllocationIndex GetCompositeIndex(Dictionary<Store, DoNotShipItems> dnsItemsByStore)
        {
            AllocationIndex index0 = AllocateYear(0, dnsItemsByStore);
            AllocationIndex index1 = AllocateYear(1, dnsItemsByStore);
            AllocationIndex index2 = AllocateYear(2, dnsItemsByStore);
            return new AllocationIndex(index0, new AllocationIndex(index1, index2));
        }

        public AllocationIndex AllocateYear(int index, Dictionary<Store, DoNotShipItems> dnsItemsByStore)
        {
            if (dnsItemsByStore.Count > 0)
            {
                Dictionary<Store, double> preallocatedIndex = new Dictionary<Store, double>();
                double totalIndex = 1;
                List<Store> mostConstrainedStores = GetMostConstrainedStores(index, dnsItemsByStore, preallocatedIndex);
                while (mostConstrainedStores != null && mostConstrainedStores.Count > 0)
                {
                    AllocationIndex allocationIndex = new AllocationIndex(index, this, dnsItemsByStore[mostConstrainedStores.FirstOrDefault()].Items, preallocatedIndex, totalIndex);
                    foreach (Store store in mostConstrainedStores)
                    {
                        if (allocationIndex.Index.TryGetValue(store, out double value))
                        {
                            preallocatedIndex.Add(store, value);
                            totalIndex -= value;
                        }
                    }
                    mostConstrainedStores = GetMostConstrainedStores(index, dnsItemsByStore, preallocatedIndex);

                }
                return new AllocationIndex(index, this, null, preallocatedIndex, totalIndex);
            }
            else
            {
                return new AllocationIndex(index, this);
            }
        }

        public List<Store> GetMostConstrainedStores(int index, Dictionary<Store, DoNotShipItems> dnsItemsByStore, Dictionary<Store,double> frozenStores = null)
        {
            if (dnsItemsByStore.Count > 0)
            {
                List<Store> mostConstrainedStores = new List<Store>();
                double maxWeight = double.MinValue;
                foreach (var kvp in dnsItemsByStore.Where(x => frozenStores == null || !frozenStores.ContainsKey(x.Key)))
                {
                    if (kvp.Value.Weight[index] > maxWeight)
                    {
                        maxWeight = kvp.Value.Weight[index];
                        mostConstrainedStores = new List<Store>() { kvp.Key };
                    }
                    else if (kvp.Value.Weight[index] >= maxWeight + 0.00001 && kvp.Value.Items.SetEquals(dnsItemsByStore[mostConstrainedStores.FirstOrDefault()].Items))
                    {
                        mostConstrainedStores.Add(kvp.Key);
                    }
                }
                return mostConstrainedStores;
            }
            return null;
        }


        public bool Equals(Group other)
        {
            return this.Cat.Equals(other.Cat) && this.Name.Equals(other.Name);
        }
    }
}
