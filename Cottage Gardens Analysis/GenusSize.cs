using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class GenusSize : IEquatable<GenusSize>
    {
        public Genus Genus { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public Dictionary<string, Item> Items { get; set; }
        public GenusSize(Genus genus, string name, string size) 
        {
            Genus = genus;
            Name = name;
            Size = size;
            Items = new Dictionary<string, Item>();
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var item in Items.Values)
            {
                item.UpdateDoNotShip(doNotShip);
            }
        }

        public bool Equals(GenusSize other)
        {
            return this.Genus.Equals(other.Genus) && this.Name.Equals(other.Name);
        }
    }
}
