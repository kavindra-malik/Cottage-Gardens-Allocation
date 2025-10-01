using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public static class Program
    {
        public const string env = "test"; // "kavin"; 


        public const string storeRankingDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\2025 Home Depot Store Ranking.csv";
        public const string storeListDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\HD store list.csv";
        public const string itemMaster = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Item Master.csv";
        public const string Dns = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Do Not Ship List.csv";


        public const string springSalesFileStem = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Spring Sales History ";

        public const string outputPath = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Output";
        

        public enum SpecLevel { Category, Genus, GenusSize, Group, Item }

        public static Dictionary<int, Store> Stores = new Dictionary<int, Store>();
        public static Dictionary<string, Category> Categories = new Dictionary<string, Category>();
        public static Dictionary<string, Genus> Genuses = new Dictionary<string, Genus>();
        public static Dictionary<string, GenusSize> GenusSizes = new Dictionary<string, GenusSize>();
        public static Dictionary<string, Group> Groups = new Dictionary<string, Group>();
        public static Dictionary<string, Item> Items = new Dictionary<string, Item>();

        public static Dictionary<Store, Metrics>[] History { get; set; }
        public static Dictionary<Store, Metrics> Benchmark { get; set; }
        public static Dictionary<Store, Allocation> Allocations { get; set; }

        public static Dictionary<string, Metrics>[] RankHistory { get; set; }
        public static Dictionary<string, Metrics> RankBenchmark { get; set; }
        public static Dictionary<string, Allocation> RankAllocations { get; set; }
        public static string[] Ranks { get; set; }


        public static Dictionary<string, Dictionary<string, HashSet<int>>> DNS = new Dictionary<string, Dictionary<string, HashSet<int>>>();
        // Dictionary key is to be tested as initial substring in the item number
        public static Dictionary<string, HashSet<int>> CategoryDNS = new Dictionary<string, HashSet<int>>();

        public static int[] HistoryYears = new int[] { 2024, 2023,2022 };
        public static int BenchmarkYear = 2025;

        public static StreamWriter itemStream { get; set; }

        static void Main()
        {
            ReadStoreRanking();
            UpdateStoreGroupAndBuyer();
            ReadItemMaster();
            ReadDoNotShip();
            for (int i = 0; i < HistoryYears.Length; i++) 
            {
                ReadSales(HistoryYears[i], i);
            }
            ReadSales(BenchmarkYear);

            if (File.Exists(Path.Combine(outputPath, "Item Allocations.CSV")))
            {
                File.Delete(Path.Combine(outputPath, "Item Allocations.CSV"));
            }

            OutputItemAllocationsHeader();
            foreach (Group group in Groups.Values.Where(g => g.HasBenchmark))
            {
//                Debug.WriteLine("Allocating Group: " + group.Name);
                group.AllocateGroupItems();
//                Debug.WriteLine("Done Allocating Group: " + group.Name);
            }
            Category cat = null;
            foreach (var item in Items.Values.Where(x => x.TotalQty > 0 && x.TargetStoreSet.Count > 0))
            {
                if (cat == null || cat != item.Group.Cat)
                {
                    cat = item.Group.Cat;
                    cat.InitHistory();
                }
                item.Output();
            }
            OutputGroupAllocationsHeader();
            foreach (Group group in Groups.Values)
            {
                if (group.Benchmark != null && group.Benchmark.Count > 0)
                {
                    group.Output();
                }
            }
        }

        #region OutputItemAllocationsHeader
        static void OutputItemAllocationsHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Store.StoreHeader);
            sb.Append(Item.ItemHeader);
            sb.Append(Metrics.BenchmarkHeader);
            sb.Append(Item.AllocationHeader);
            sb.Append(Metrics.HistoryHeader);
            OutputItemAllocation(sb.ToString());
        }
        #endregion

        #region OutputGroupAllocationsHeader
        static void OutputGroupAllocationsHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Store.StoreHeader);
            sb.Append(Group.GroupHeader);
            sb.Append(Metrics.GroupBenchmarkHeader);
            sb.Append(Group.AllocationHeader);
            sb.Append(Metrics.GroupHistoryHeader);
            OutputGroupAllocation(sb.ToString());
        }
        #endregion


        #region ReadStoreRanking
        static void ReadStoreRanking()
        {
            HashSet<string> ranks = new HashSet<string>();
            using (TextFieldParser csvParser = new TextFieldParser(storeRankingDataFile))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the first row - blank
                csvParser.ReadLine();
                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    int storeNbr = 0;
                    int market = 0;
                    if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) && int.TryParse(fields[0], out int mkt)) 
                    {
                        market = mkt;
                        if (int.TryParse(fields[1], out int nbr))
                        {
                            storeNbr = nbr;
                        }
                        else
                        {
                            throw new Exception("Non-integer store nbr");
                        }
                    }
                    string name = fields[4].Trim();
                    string storeName = name.Substring(7, 15);
                    string state = name.Substring(name.IndexOf(',') + 2, 2);
                    string city = name.Substring(26, name.IndexOf(',') - 26);
                    string rank = fields[5].Trim();
                    Stores.Add(storeNbr, new Store(storeNbr, market, storeName, city, state, null, null, rank));
                    ranks.Add(rank);
                }
            }
            Ranks = (from x in ranks orderby x select x).ToArray<string>();
        }
        #endregion

        #region UpdateStoreGroupAndBuyer
        static void UpdateStoreGroupAndBuyer()
        {
            int needUpdatingCount = Stores.Count;
            using (TextFieldParser csvParser = new TextFieldParser(storeListDataFile))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    int storeNbr = 0;
                    int market = 0;
                    if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) && int.TryParse(fields[0], out int nbr))
                    {
                        storeNbr = nbr;
                        if (int.TryParse(fields[1], out int mkt))
                        {
                            market = mkt;
                        }
                        else
                        {
                            throw new Exception("Non-integer store nbr");
                        }
                    }
                    string group = fields[3].Trim();
                    string buyer = fields[4].Trim();
                    if (Stores.TryGetValue(storeNbr, out var store))
                    {
                        store.Market = market;
                        store.Buyer = buyer;
                        needUpdatingCount--;
                    }
                    else
                    {
                        throw new Exception("Did not find store number: " + storeNbr + " in the store ranking data");
                    }
                }
            }
            if (needUpdatingCount > 0)
            {
                throw new Exception("Did not find " + needUpdatingCount + " in store list data");
            }
        }
        #endregion

        #region ReadItemMaster
        static void ReadItemMaster()
        {
            using (TextFieldParser csvParser = new TextFieldParser(itemMaster))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the first row - blank
                csvParser.ReadLine();
                // Skip the row with the column names
                csvParser.ReadLine();

                // 0,	 1,	               2,	    3,	      4,	       5,	        6,	     7,       8,	 9,	         10,        11
                // Item, Item Description, Size,    Inactive, Category,    Tag Code,    Program, Zone,    GROUP, GENUS SIZE, MULTIPLE,  GENUS

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[3]) && string.Equals(fields[3].Trim(), "N"))
                    {
                        string nbr = fields[0].Trim();
                        string desc = fields[1].Trim();
                        string size = fields[2].Trim();
                        string categoryName = fields[4].Trim();
                        string tag = fields[5].Trim();
                        string program = fields[6].Trim();
                        byte zone = 3;
                        if (!string.IsNullOrWhiteSpace(fields[7]) && !byte.TryParse(fields[7], out zone)) 
                        {
                            LogException("Active Item: " + nbr + ", Desc: " + desc + " does not have a value for Zone. Zone = 3 being used.");
                        }
                        string groupName = fields[8].Trim();
                        string genusSizeName = fields[9].Trim();
                        int multiple = 1;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && !int.TryParse(fields[10].Trim(), out multiple))
                        {
                            LogException("Active Item: " + nbr + ", Desc: " + desc + " does not have a value for multiple. Multiple = 1 being used.");
                        }
                        string genusName = fields[11].Trim();

                        if (!Categories.TryGetValue(categoryName, out var cat))
                        {
                            cat = new Category(categoryName);
                            Categories.Add(categoryName, cat);
                        }
                        if (!cat.Groups.TryGetValue(groupName, out var group))
                        {
                            if (!Groups.TryGetValue(groupName, out group))
                            {
                                group = new Group(cat, groupName);
                                Groups.Add(groupName, group);
                            }
                            cat.Groups.Add(groupName, group);
                        }
                        if (!cat.Genuses.TryGetValue(genusName, out var genus))
                        {
                            if (!Genuses.TryGetValue(genusName, out genus))
                            {
                                genus = new Genus(cat, genusName);
                                Genuses.Add(genusName, genus);
                            }
                            cat.Genuses.Add(genusName, genus);

                        }
                        if (!genus.GenusSizes.TryGetValue(genusSizeName, out var genusSize))
                        {
                            if (!GenusSizes.TryGetValue(genusSizeName, out genusSize))
                            {
                                genusSize = new GenusSize(genus, genusSizeName, size);
                                GenusSizes.Add(genusSizeName, genusSize);
                            }
                            genus.GenusSizes.Add(genusSizeName, genusSize);
                        }
                        Item item = new Item(nbr, desc, size, genusSize, group, tag, program, zone, multiple);
                        Items.Add(nbr, item);
                        genusSize.Items.Add(nbr, item);
                        group.Items.Add(nbr, item);
                    }
                }
            }
        }
        #endregion

        #region ReadDoNotShip
        static void ReadDoNotShip()
        {
            List<DoNotShipSpec> dnsSpecs = new List<DoNotShipSpec>();
            int offset = 4;
            using (TextFieldParser csvParser = new TextFieldParser(Dns))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the top 3 rows
                csvParser.ReadLine();
                csvParser.ReadLine();
                csvParser.ReadLine();

                // Initialize Specs - 5 rows:
                //      Category
                //      Genus
                //      Genus Size
                //      Group
                //      Item

                int index = offset;
                foreach (SpecLevel specLevel in Enum.GetValues(typeof(SpecLevel)))
                {
                    string[] fields = csvParser.ReadFields();
                    for (int i = index; i < fields.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(fields[i]))
                        {
                            dnsSpecs.Add(new DoNotShipSpec(specLevel, fields[i].Trim()));
                        }
                        else
                        {
                            index = i;
                            break;
                        }
                    }
                }
                // Skip headers
                csvParser.ReadLine();
                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) && int.TryParse(fields[0], out int storeNbr))
                    {
                        if (Stores.TryGetValue(storeNbr, out Store store))
                        {
                            if (byte.TryParse(fields[3], out var zone))
                            {
                                store.WeatherZone = zone;
                            }
                            else
                            {
                                LogException("Store # " + storeNbr + " : Zone value: " + fields[3] + " encountered in Do Not Ship file could not be parsed");
                            }
                            for (int i = offset; i < fields.Length; i++)
                            {
                                if (!string.IsNullOrEmpty(fields[0]) && string.Equals(fields[i].Trim(), "N", StringComparison.OrdinalIgnoreCase) && i < dnsSpecs.Count + offset)
                                {
                                    dnsSpecs[i - offset].AddStore(store);
                                }
                            }
                        }
                        else
                        {
                            LogException("Store # " + storeNbr + " encountered in Do Not Ship file not in Store Master");
                        }
                    }
                }
            }
            ProcessDnsSpecs(dnsSpecs);
        }

        static void ProcessDnsSpecs(List<DoNotShipSpec> dnsSpecs)
        {
            int count = dnsSpecs.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (dnsSpecs[i].Stores.Count == 0)
                {
                    Debug.WriteLine("Look");
                }
                switch (dnsSpecs[i].SpecificationLevel)
                {
                    case SpecLevel.Item:
                        if (Items.TryGetValue(dnsSpecs[i].Id, out var item))
                        {
                            item.UpdateDoNotShip(dnsSpecs[i].Stores);
                        }
                        break;
                    case SpecLevel.Group:
                        if (Groups.TryGetValue(dnsSpecs[i].Id, out var group))
                        {
                            group.UpdateDoNotShip(dnsSpecs[i].Stores);
                        }
                        break;
                    case SpecLevel.GenusSize:
                        if (GenusSizes.TryGetValue(dnsSpecs[i].Id, out var genusSize))
                        {
                            genusSize.UpdateDoNotShip(dnsSpecs[i].Stores);
                        }
                        break;
                    case SpecLevel.Genus:
                        if (Genuses.TryGetValue(dnsSpecs[i].Id, out var genus))
                        {
                            genus.UpdateDoNotShip(dnsSpecs[i].Stores);
                        }
                        break;
                    case SpecLevel.Category:
                        if (Categories.TryGetValue(dnsSpecs[i].Id, out var category))
                        {
                            category.UpdateDoNotShip(dnsSpecs[i].Stores);
                        }
                        break;
                }
            }
            SetDnsOnWeatherZones();
        }

        static void SetDnsOnWeatherZones()
        {
            foreach (Item item in Items.Values)
            {
                foreach (Store store in Stores.Values.Where(x => (item.DoNotShip == null || !item.DoNotShip.Contains(x)) && x.WeatherZone < item.Zone))
                {
                    if (item.DoNotShip == null)
                    {
                        item.DoNotShip = new HashSet<Store>();
                    }
                    item.DoNotShip.Add(store);
                }
            }
        }
        #endregion

        #region ReadSales
        static void ReadSales(int year, int? historyIndex = null)
        {
            int rejectedCount = 0;
            HashSet<string> rejectedItems = new HashSet<string>();
            int rejectedDns = 0;
            using (TextFieldParser csvParser = new TextFieldParser(SalesFilePath(year)))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the first 3 rows
                csvParser.ReadLine();
                csvParser.ReadLine();
                csvParser.ReadLine();
                int lineNbr = 3;
                while (!csvParser.EndOfData)
                {
                    lineNbr++;
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    if (fields.Length > 14)
                    {
                        string itemCode = fields[2].Trim();
                        if (string.IsNullOrWhiteSpace(itemCode))
                        {
                            continue;
                        }
                        if (!Items.TryGetValue(itemCode, out var item))
                        {
                            rejectedItems.Add(itemCode);
                            rejectedCount++;
                            continue;
                        }
                        int storeNbr = 0;
                        if (!string.IsNullOrWhiteSpace(fields[1]) && int.TryParse(fields[1], out storeNbr) && !string.IsNullOrWhiteSpace(itemCode))
                        {
                            if (int.TryParse(fields[1], out int nbr))
                            {
                                storeNbr = nbr;
                            }
                            else
                            {
                                throw new Exception("Line Nbr: " + lineNbr + ", Non-integer store nbr");
                            }
                        }
                        if (!Stores.TryGetValue(storeNbr, out var store))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Unknown store nbr: " + storeNbr);
                        }
                        int qtyDelivered = 0;
                        if (!string.IsNullOrWhiteSpace(fields[8]) && !int.TryParse( fields[8], NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out qtyDelivered))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Non-integer Qty Del: " + fields[8]);
                        }

                        double dollarsDelivered = 0;
                        if (!string.IsNullOrWhiteSpace(fields[9]) && !double.TryParse(fields[9], NumberStyles.Currency, CultureInfo.InvariantCulture, out dollarsDelivered))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Del " + fields[9] + " could not be parsed.");
                        }
                        double dollarsDeliveredRetail = 0;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && !double.TryParse(fields[10], NumberStyles.Currency, CultureInfo.InvariantCulture, out dollarsDeliveredRetail))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Del Retail " + fields[10] + " could not be parsed.");
                        }

                        int qtySold = 0;
                        if (!string.IsNullOrWhiteSpace(fields[11]) && !int.TryParse(fields[11], NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out qtySold))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Non-integer Qty Sold: " + fields[11]);
                        }

                        double dollarsSold = 0;
                        if (!string.IsNullOrWhiteSpace(fields[12]) && !double.TryParse(fields[12], NumberStyles.Currency, CultureInfo.InvariantCulture, out dollarsSold))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Sold " + fields[12] + " could not be parsed.");
                        }
                        double dollarsSoldRetail = 0;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && !double.TryParse(fields[13], out dollarsSoldRetail))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Sold Retail " + fields[13] + " could not be parsed.");
                        }

                        if (historyIndex.HasValue)
                        {
                            if (item.DoNotShip == null || !item.DoNotShip.Contains(store))
                            {
                                if (item.History[historyIndex.Value] == null)
                                {
                                    item.History[historyIndex.Value] = new Dictionary<Store, Metrics>();
                                }
                                item.History[historyIndex.Value].Add(store, new Metrics(qtyDelivered, qtySold, dollarsDelivered, dollarsSold, dollarsDeliveredRetail, dollarsSoldRetail));
                                item.Group.HasHistory[historyIndex.Value] = true;
                                item.Group.Cat.HasHistory[historyIndex.Value] = true;
                            }
                            else
                            {
                                rejectedDns++;
                            }
                        }
                        else
                        {
                            if (item.Benchmark == null)
                            {
                                item.Benchmark = new Dictionary<Store, Metrics>();
                            }
                            item.Benchmark.Add(store, new Metrics(qtyDelivered, qtySold, dollarsDelivered, dollarsSold, dollarsDeliveredRetail, dollarsSoldRetail));
                            if (!((qtyDelivered >= 20 && qtySold == 0) || qtyDelivered == 0))
                            {
                                item.Group.HasBenchmark = true;
                            }
                        }
                    }
                }
            }
            Debug.WriteLine("YEAR:" + year + ", Rejected Rows (Inactive Item): " + rejectedCount + ", Rejected Rows (DNS Location): " + rejectedDns + ", Inactive Item Count: " + rejectedItems.Count);
            
            // Debugging

        }
        #endregion

        #region Helper methods
        private static string SalesFilePath(int year)
        {
            return springSalesFileStem + year + ".csv";
        }

        public static void LogException(string message)
        {
            using (StreamWriter sw = File.AppendText(Path.Combine(outputPath, "Exceptions.CSV")))
            {
                sw.WriteLine(message);
            }
        }


        #region OutputItemAllocation
        public static void OutputItemAllocation(string message)
        {
            using (StreamWriter sw = File.AppendText(Path.Combine(outputPath, "Item Allocations.CSV")))
            {
                sw.WriteLine(message);
            }
        }
        #endregion

        #region OutputGroupAllocation
        public static void OutputGroupAllocation(string message)
        {
            using (StreamWriter sw = File.AppendText(Path.Combine(outputPath, "Group Allocations.CSV")))
            {
                sw.WriteLine(message);
            }
        }
        #endregion

        #region Projection
        public static Dictionary<Store, double> Projection(Dictionary<Store, double> index, HashSet<Store> allocationSet)
        {
            Dictionary<Store, double> newIndex = new Dictionary<Store, double>();
            double sum = 0;
            foreach (var kvp in index.Where(k => allocationSet.Contains(k.Key)))
            {
                newIndex.Add(kvp.Key, kvp.Value);
                sum += kvp.Value;
            }
            foreach (var x in new List<Store>(newIndex.Keys))
            {
                newIndex[x] /= sum;
            }
            return newIndex;
        }
        #endregion

        #region CombineIndex
        public static Dictionary<Store, double> CombineIndex(Dictionary<Store, double> index1, Dictionary<Store, double> index2, double weight)
        {
            double sum = 0;
            Dictionary<Store, double> newIndex = new Dictionary<Store, double>();
            foreach (KeyValuePair<Store, double> kvp in index1)
            {
                if (index2.ContainsKey(kvp.Key))
                {
                    sum += newIndex[kvp.Key] = kvp.Value * weight + index2[kvp.Key] * (1 - weight);
                }
                else
                {
                    sum += newIndex[kvp.Key] = kvp.Value;
                }
            }
            var index2NotInIndex1 = from x in index2.Keys where !index1.ContainsKey(x) select x;
            foreach (var x in index2NotInIndex1)
            {
                sum += newIndex[x] = index2[x];
            }
            foreach (var x in new List<Store>(newIndex.Keys))
            {
                newIndex[x] /= sum;
            }
            return newIndex;
        }
        #endregion

        #endregion

        #region InitAllocation
        public static void InitHistory()
        {
            if (History == null)
            {
                History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
                foreach (Category cat in Categories.Values)
                {
                    if (cat.History != null)
                    {
                        for (int i = 0; i < Program.HistoryYears.Length; i++)
                        {
                            if (cat.History[i] != null)
                            {
                                foreach (var kvp in cat.History[i])
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

            RankHistory = new Dictionary<string, Metrics>[Program.HistoryYears.Length];
        }
        #endregion

        #region InitAllocation
        public static void InitAllocation()
        {
            Allocations = new Dictionary<Store, Allocation>();
            foreach (Category cat in Categories.Values)
            {
                if (cat.Allocations != null)
                {
                    foreach (var kvp in cat.Allocations)
                    {
                        if (!Allocations.ContainsKey(kvp.Key))
                        {
                            Allocations[kvp.Key] = new Allocation(kvp.Value);
                        }
                        else
                        {
                            Allocations[kvp.Key].Add(kvp.Value);
                        }
                    }
                }
            }
            RankAllocations = new Dictionary<string, Allocation>();
            foreach (var kvp in Allocations)
            {
                if (!RankAllocations.TryGetValue(kvp.Key.Rank, out var allocation))
                {
                    RankAllocations[kvp.Key.Rank] = new Allocation(kvp.Value);
                }
                else
                {
                    RankAllocations[kvp.Key.Rank].Add(kvp.Value);
                }
            }
        }

        #endregion


    }


}
