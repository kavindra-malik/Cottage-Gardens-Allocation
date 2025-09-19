using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public static class Program
    {
        public const string env = "test"; // "kavin"; 

        public const string outputPath = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Output";

        public const string storeRankingDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\2025 Home Depot Store Ranking.csv";
        public const string storeListDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\HD store list.csv";
        public const string itemMaster = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Item Master.csv";
        public const string Dns = @"C:\Users\"" + env + @""\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Do Not Ship List.csv";


        public const string springSalesFileStem = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Spring Sales History ";

        public enum SpecLevel { Category, Genus, GenusSize, Group, Item }

        public static Dictionary<int, Store> Stores = new Dictionary<int, Store>();
        public static Dictionary<string, Category> Categories = new Dictionary<string, Category>();
        public static Dictionary<string, Genus> Genuses = new Dictionary<string, Genus>();
        public static Dictionary<string, GenusSize> GenusSizes = new Dictionary<string, GenusSize>();
        public static Dictionary<string, Group> Groups = new Dictionary<string, Group>();
        public static Dictionary<string, Item> Items = new Dictionary<string, Item>();


        public static Dictionary<string, Dictionary<string, HashSet<int>>> DNS = new Dictionary<string, Dictionary<string, HashSet<int>>>();
        // Dictionary key is to be tested as initial substring in the item number
        public static Dictionary<string, HashSet<int>> CategoryDNS = new Dictionary<string, HashSet<int>>();

        public static (int year, decimal weight)[] HistoryYears = { (2024, 0.6M), (2023, 0.25M), (2022, 0.15M) };
        public static int BenchmarkYear = 2025;

        static void Main()
        {
            ReadStoreRanking();
            UpdateStoreGroupAndBuyer();
            ReadItemMaster();
            ReadDoNotShip();
            for (int i = 0; i < HistoryYears.Length; i++) 
            {
                ReadSales(HistoryYears[i].year, i);
            }
            ReadSales(BenchmarkYear);
            foreach (Group group in Groups.Values)
            {
                group.AllocateGroupItems();
            }

        }

        #region ReadStoreRanking
        static void ReadStoreRanking()
        {

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
                }
            }
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
                            group = new Group(cat, groupName);
                            cat.Groups.Add(groupName, group);
                            Groups.Add(groupName, group);
                        }
                        if (!cat.Genuses.TryGetValue(genusName, out var genus))
                        {
                            genus = new Genus(cat, genusName);
                            cat.Genuses.Add(genusName, genus);
                            Genuses.Add(genusName, genus);
                        }
                        if (!genus.GenusSizes.TryGetValue(genusSizeName, out var genusSize))
                        {
                            genusSize = new GenusSize(genus, genusSizeName, size);
                            genus.GenusSizes.Add(genusSizeName, genusSize);
                            GenusSizes.Add(genusSizeName, genusSize);
                        }
                        Item item = new Item(nbr, desc, genusSize, group, tag, program, zone, multiple);
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
                            dnsSpecs.Add(new DoNotShipSpec(SpecLevel.Category, fields[i].Trim()));
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
                                if (!string.IsNullOrEmpty(fields[i]) && string.Equals(fields[i].Trim(), "N", StringComparison.OrdinalIgnoreCase) && i < dnsSpecs.Count)
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
        }
        #endregion

        #region ReadSales
        static void ReadSales(int year, int? historyIndex = null)
        {
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
                            throw new Exception("Line Nbr: " + lineNbr + ", Item Code =  Null encountered");
                        }
                        if (!Items.TryGetValue(itemCode, out var item))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Unknown Item Code: " + itemCode);
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
                        if (!string.IsNullOrWhiteSpace(fields[8]) && !int.TryParse( fields[8], out qtyDelivered))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Non-integer Qty Del: " + fields[8]);
                        }

                        double dollarsDelivered = 0;
                        if (!string.IsNullOrWhiteSpace(fields[9]) && double.TryParse(fields[9], out dollarsDelivered))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Del " + fields[9] + " could not be parsed.");
                        }
                        double dollarsDeliveredRetail = 0;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && double.TryParse(fields[10], out dollarsDeliveredRetail))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Del Retail " + fields[10] + " could not be parsed.");
                        }

                        int qtySold = 0;
                        if (!string.IsNullOrWhiteSpace(fields[11]) && !int.TryParse(fields[11], out qtySold))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", Non-integer Qty Sold: " + fields[11]);
                        }

                        double dollarsSold = 0;
                        if (!string.IsNullOrWhiteSpace(fields[12]) && double.TryParse(fields[12], out dollarsSold))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Sold " + fields[12] + " could not be parsed.");
                        }
                        double dollarsSoldRetail = 0;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && double.TryParse(fields[13], out dollarsSoldRetail))
                        {
                            throw new Exception("Line Nbr: " + lineNbr + ", $ Sold Retail " + fields[13] + " could not be parsed.");
                        }

                        if (historyIndex.HasValue)
                        {
                            if (!item.DoNotShip.Contains(store) && item.Zone <= store.WeatherZone)
                            {
                                item.History[historyIndex.Value].Add(store, new Metrics(qtyDelivered, qtySold, dollarsDelivered, dollarsSold, dollarsDeliveredRetail, dollarsSoldRetail));
                            }
                        }
                        else
                        {
                            item.Benchmark.Add(store, new Metrics(qtyDelivered, qtySold, dollarsDelivered, dollarsSold, dollarsDeliveredRetail, dollarsSoldRetail));
                        }
                    }
                }
            }
        }
        #endregion

        #region Helper methods
        private static string SalesFilePath(int year)
        {
            return springSalesFileStem + year + ".csv";
        }
        private static string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }

        public static void LogException(string message)
        {
            using (StreamWriter sw = File.AppendText(Path.Combine(outputPath, "Exceptions.CSV")))
            {
                sw.WriteLine(message);
            }
        }
        #endregion


    }


}
