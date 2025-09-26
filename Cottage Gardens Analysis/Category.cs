using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Category : IEquatable<Category>
    {
        public string Name { get; set; }
        public Dictionary<string, Genus> Genuses { get; set; }
        public Dictionary<string, Group> Groups { get; set; }
        public bool[] HasHistory { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }


        private AllocationIndex _allocationIndex;
        public Category(string name)
        {
            Name = name;
            Genuses = new Dictionary<string, Genus>();
            Groups = new Dictionary<string, Group>();
            HasHistory = new bool[Program.HistoryYears.Length];
            _allocationIndex = null;
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var genus in Groups.Values)
            {
                genus.UpdateDoNotShip(doNotShip);
            }
        }


        public AllocationIndex Index
        {
            get
            {
                if (_allocationIndex == null)
                {
                    for (int i = Program.HistoryYears.Length - 1; i >= 0; i--)
                    {
                        if (HasHistory[i])
                        {
                            AllocationIndex index = new AllocationIndex(i, Items);
                            if (_allocationIndex == null)
                            {
                                _allocationIndex = index;
                            }
                            else
                            {
                                _allocationIndex = new AllocationIndex(_allocationIndex, index);
                            }
                        }
                    }
                }
                return _allocationIndex;
            }
        }

        public void InitHistory()
        {
            if (History == null)
            {

                History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
                foreach (Group group in Groups.Values)
                {
                    if (group.History != null)
                    {
                        for (int i = 0; i < Program.HistoryYears.Length; i++)
                        {
                            if (group.History[i] != null)
                            {
                                foreach (var kvp in group.History[i])
                                {
                                    if (History[i] == null)
                                    {
                                        History[i] = new Dictionary<Store, Metrics>();
                                    }
                                    if (!History[i].ContainsKey(kvp.Key))
                                    {
                                        History[i][kvp.Key] = new Metrics(kvp.Value);
                                    }
                                    else
                                    {
                                        History[i][kvp.Key].Add(kvp.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Item> Items
        {
            get
            {
                IEnumerable<Item> items = new List<Item>();
                foreach (var group in Groups.Values)
                {
                    items = items.Concat(group.Items.Values);
                }
                return items;
            }
        }


        public bool Equals(Category other)
        {
            return this.Name.Equals(other.Name);
        }

    }
}
