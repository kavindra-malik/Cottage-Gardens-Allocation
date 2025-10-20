using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Allocation
{
    public class Genus : IEquatable<Genus>
    {
        public Category Cat { get; set; }
        public string Name { get; set; }
        public Dictionary<string, GenusSize> GenusSizes { get; set; }
        public Genus(Category cat, string name)
        {
            Cat = cat;
            Name = name;
            GenusSizes = new Dictionary<string, GenusSize>();
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var genusSize in GenusSizes.Values)
            {
                genusSize.UpdateDoNotShip(doNotShip);
            }
        }

        public bool Equals(Genus other)
        {
            return this.Cat.Equals(other.Cat) && string.Equals(this.Name, other.Name);
        }
    }
}
