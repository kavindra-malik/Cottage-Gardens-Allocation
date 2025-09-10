using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
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

        public const string storeRankingDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\2025 Home Depot Store Ranking.csv";
        public const string storeListDataFile = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\HD store list.csv";
        public const string itemHierarchy = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\InvUDFCustom - as of 9-5-2025.csv";
        public const string outputPath = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Output";
        public const string shrubsDNS = @"C:\Users\"" + env + @""\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\DNS - Shrubs.csv";
        public const string treesDNS = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\DNS - Trees.csv";

        public const string springSalesFileStem = @"C:\Users\" + env + @"\OneDrive - Intellection LLC\Current Clients\Cottage Gardens\Data\Spring Sales History ";

        public static Dictionary<int, Store> Stores = new Dictionary<int, Store>();
        public static Dictionary<string, Item> Items = new Dictionary<string, Item>();
        public static Dictionary<string, Category> Categories = new Dictionary<string, Category>();

        public static Dictionary<string, Dictionary<string, HashSet<int>>> DNS = new Dictionary<string, Dictionary<string, HashSet<int>>>();
        // Dictionary key is to be tested as initial substring in the item number
        public static Dictionary<string, HashSet<int>> CategoryDNS = new Dictionary<string, HashSet<int>>();

        public static (int year, double weight)[] HistoryYears = { (2024, 0.6), (2023, 0.25), (2022, 0.15) };

        static void Main()
        {
            ReadStoreRanking();
            UpdateStoreGroupAndBuyer();
            ReadItemHierarchy();
            ReadShrubsDNS();
            ReadTreesDNS();
            foreach ((int year, double weight) in HistoryYears) 
            {
                    ReadSalesHistory(year, weight);
            }
            CalcIndex();
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

        #region ReadItemHierarchy
        static void ReadItemHierarchy()
        {
            using (TextFieldParser csvParser = new TextFieldParser(itemHierarchy))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the first row - blank
                csvParser.ReadLine();
                // Skip the row with the column names
                csvParser.ReadLine();

                // 0,	 1,	               2,	    3,	      4,	       5,	        6,	     7,       8,	 9,	         10
                // Item, Item Description, Size,    Inactive, Category,    Tag Code,    Program, Zone,    GROUP, GENUS SIZE, GENUS

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
                        byte zone = 0;
                        if (!string.IsNullOrWhiteSpace(fields[7]) && !byte.TryParse(fields[7], out zone)) { }
                        string groupName = fields[8].Trim();
                        string genusSizeName = fields[9].Trim();
                        string genusName = fields[10].Trim();

                        if (!Categories.TryGetValue(categoryName, out var cat))
                        {
                            cat = new Category(categoryName);
                            Categories.Add(categoryName, cat);
                        }
                        if (!cat.Groups.TryGetValue(groupName, out var group))
                        {
                            group = new Group(cat, groupName);
                            cat.Groups.Add(groupName, group);
                        }
                        if (!cat.Genuses.TryGetValue(genusName, out var genus))
                        {
                            genus = new Genus(cat, genusName);
                            cat.Genuses.Add(genusName, genus);
                        }
                        if (!genus.GenusSizes.TryGetValue(genusSizeName, out var genusSize))
                        {
                            genusSize = new GenusSize(genus, genusSizeName, size);
                            genus.GenusSizes.Add(genusSizeName, genusSize);
                        }
                        Item item = new Item(nbr, desc, genusSize, group, tag, program, zone);
                        Items.Add(nbr, item);
                        genusSize.Items.Add(nbr, item);
                        group.Items.Add(nbr, item);
                    }
                }
            }
        }
        #endregion

        #region ReadShrubsDNS
        static void ReadShrubsDNS()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputPath, "Exceptions.CSV"),true))
            {

                using (TextFieldParser csvParser = new TextFieldParser(shrubsDNS))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    // Skip the top row
                    csvParser.ReadLine();
                    // Read and trim Genus names
                    string[] genus = csvParser.ReadFields();
                    for (int i = 0; i < genus.Length; i++)
                    {
                        genus[i] = genus[i].Trim().ToUpper();
                    }
                    // Read and trim prefix names
                    string[] columns = csvParser.ReadFields();
                    for (int i = 0; i < columns.Length; i++)
                    {
                        columns[i] = columns[i].Trim().ToUpper();
                    }

                    while (!csvParser.EndOfData)
                    {
                        // Read current line fields, pointer moves to the next line.
                        string[] fields = csvParser.ReadFields();
                        if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) && int.TryParse(fields[0], out int storeNbr))
                        {
                            /*
                            string state = fields[2].Trim();
                            if (!int.TryParse(fields[3], out int zone))
                            {
                                sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + "," + fields[3] + ", Expected to be a weather zone");
                                continue;
                            }
                            */
                            for (int i = 4; i < fields.Length; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(fields[i]))
                                {
                                    string genusId = genus[i];
                                    if (!string.IsNullOrWhiteSpace(genusId))
                                    {
                                        sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + ", Value in file cell " + GetExcelColumnName(i + 4) + "2 expected to be a valid genus-size. Found null");
                                        continue;
                                    }
                                    string prefix = columns[i];
                                    if (!string.IsNullOrWhiteSpace(prefix)) 
                                    {
                                        sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + ", Value in file column " + GetExcelColumnName(i + 4) + "3 expected to be a valid item prefix. Found null");
                                        continue;
                                    }
                                    if (!DNS.TryGetValue(genusId, out var prefixDNS))
                                    {
                                        prefixDNS = new Dictionary<string, HashSet<int>>();
                                        DNS.Add(genusId, prefixDNS);
                                    }
                                    if (!prefixDNS.TryGetValue(prefix, out var storeSet))
                                    {
                                        storeSet = new HashSet<int>();
                                        prefixDNS.Add(prefix, storeSet);
                                    }
                                    storeSet.Add(storeNbr);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region ReadTreesDNS
        static void ReadTreesDNS()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputPath, "Exceptions.CSV"), true))
            {

                using (TextFieldParser csvParser = new TextFieldParser(treesDNS))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    // Skip the top row
                    csvParser.ReadLine();
                    // Read and trim Genus names
                    string[] genus = csvParser.ReadFields();
                    for (int i = 0; i < genus.Length; i++)
                    {
                        genus[i] = genus[i].Trim().ToUpper();
                    }
                    // Read and trim prefix names
                    string[] columns = csvParser.ReadFields();
                    for (int i = 0; i < columns.Length; i++)
                    {
                        columns[i] = columns[i].Trim().ToUpper();
                    }

                    // Inventory UDF Maintenance Report	Item Description	Item Grouping
                    while (!csvParser.EndOfData)
                    {
                        // Read current line fields, pointer moves to the next line.
                        string[] fields = csvParser.ReadFields();
                        if (fields.Length > 0 && !string.IsNullOrWhiteSpace(fields[0]) && int.TryParse(fields[0], out int storeNbr))
                        {
                            /*
                            string state = fields[2].Trim();
                            if (!int.TryParse(fields[3], out int zone))
                            {
                                sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + "," + fields[3] + ", Expected to be a weather zone");
                                continue;
                            }
                            */
                            for (int i = 4; i < fields.Length; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(fields[i]))
                                {
                                    string genusId = genus[i];
                                    if (!string.IsNullOrWhiteSpace(genusId))
                                    {
                                        sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + ", Value in file cell " + GetExcelColumnName(i + 4) + "2 expected to be a valid genus-size. Found null");
                                        continue;
                                    }
                                    string prefix = columns[i];
                                    if (!string.IsNullOrWhiteSpace(prefix))
                                    {
                                        sw.WriteLine(Path.GetFileName(shrubsDNS) + "," + DateTime.Today.ToString() + ", Value in file column " + GetExcelColumnName(i + 4) + "3 expected to be a valid item prefix. Found null");
                                        continue;
                                    }
                                    if (!DNS.TryGetValue(genusId, out var prefixDNS))
                                    {
                                        prefixDNS = new Dictionary<string, HashSet<int>>();
                                        DNS.Add(genusId, prefixDNS);
                                    }
                                    if (!prefixDNS.TryGetValue(prefix, out var storeSet))
                                    {
                                        storeSet = new HashSet<int>();
                                        prefixDNS.Add(prefix, storeSet);
                                    }
                                    storeSet.Add(storeNbr);
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region ReadSalesHistory
        static void ReadSalesHistory(int year, double weight)
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

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    if (fields.Length > 14)
                    {
                        string itemCode = fields[2].Trim();
                        if (string.IsNullOrWhiteSpace(itemCode))
                        {
                            throw new Exception("Item Code =  Null encountered");
                        }
                        if (!Items.TryGetValue(itemCode, out var item))
                        {
                            throw new Exception("Unknown Item Code: " + itemCode);
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
                                throw new Exception("Non-integer store nbr");
                            }
                        }
                        if (!Stores.TryGetValue(storeNbr, out var store))
                        {
                            throw new Exception("Unknown store nbr: " + storeNbr);
                        }
                        double dollarDelivered = 0;
                        if (!string.IsNullOrWhiteSpace(fields[10]) && double.TryParse(fields[10], out double d))
                        {
                            dollarDelivered = d;
                        }
                        double dollarSales = 0;
                        if (!string.IsNullOrWhiteSpace(fields[14]) && double.TryParse(fields[14], out d))
                        {
                            dollarSales = d;
                        }
                        if (!item.Group.SalesMeasures.TryGetValue(store, out var storeMetrics))
                        {
                            storeMetrics = new Metrics();
                            item.Group.SalesMeasures.Add(store, storeMetrics);
                        }
                        storeMetrics.Add(dollarDelivered, dollarSales, weight);
                    }
                }
            }
        }
        #endregion
        static void CalcIndex()
        {
            foreach (Category category in Categories.Values)
            {
                foreach (Group group in category.Groups.Values.Where(g => g.SalesMeasures.Count > 0))
                {
                    double sum = 0;
                    int count = 0;
                    foreach (Metrics metrics in group.SalesMeasures.Values)
                    {
                        sum += metrics.CompositeMetric;
                        count++;
                    }
                    sum /= count;
                    if (sum <= 0)
                    {
                        throw new Exception("Encountered Sum of Composite Metrics = 0. It indicates misaligned Sales History data columns.");
                    }
                    foreach (Metrics metrics in group.SalesMeasures.Values)
                    {
                        metrics.Index = metrics.CompositeMetric / sum;
                    }
                }
            }
        }


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
        #endregion

    }


}
