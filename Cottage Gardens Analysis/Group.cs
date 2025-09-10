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
        public Dictionary<Store, Metrics> SalesMeasures { get; set; }
        public Group(Category cat, string name)
        {
            Cat = cat;
            Name = name;
            Items = new Dictionary<string, Item>();
            SalesMeasures = new Dictionary<Store, Metrics>();
        }

        public bool Equals(Group other)
        {
            return this.Cat.Equals(other.Cat) && this.Name.Equals(other.Name);
        }
    }
}
