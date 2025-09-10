using System;
using System.Collections.Generic;
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
        public string Program { get; set; }
        public byte Zone { get; set; }
        public int Multiple { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public HashSet<Store> IncompatibleStores { get; set; }


        // Item, Item Description, Size,    Inactive, Category,    Tag Code,    Program, Zone,    GROUP, GENUS SIZE, GENUS
        public Item(string nbr, string desc, GenusSize genusSize, Group group, string tag, string program, byte zone)
        {
            Nbr = nbr;
            Desc = desc;
            GenusSize = genusSize;
            Group = group;
            Tag = tag;
            Program = program;
            Zone = zone;
            Multiple = 1;
            History = new Dictionary<Store, Metrics>[Program.HistoryYears.Count()];
            IncompatibleStores = new HashSet<Store>();
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
