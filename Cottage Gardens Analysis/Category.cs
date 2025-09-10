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

        public Category(string name)
        {
            Name = name;
            Genuses = new Dictionary<string, Genus>();
            Groups = new Dictionary<string, Group>();
        }

        public bool Equals(Category other)
        {
            return this.Name.Equals(other.Name);
        }

    }
}
