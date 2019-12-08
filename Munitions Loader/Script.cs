//DEFINITIONS:
//Values and strings inside quotes can be changed here.

const string SOURCE = "[SRC]";
const string AMMO = "NATO_25x184mm";
const string MISL = "Missile200mm";
const string FUEL = "Uranium";

//All following definition groups follow the format:
// const string CONSTANT = "Inventory Tag Here";
// const int AMMO = number of ammo here;

//Small Ship - Medium Cargo - Both Ammo Types
const string XS_MAG = "[XSMG]";
const int XS_MAG_AMMO = 75;
const int XS_MAG_MISL = 36;

//Small Ship - Medium Cargo - Bullets Only
const string GS_MAG = "[GSMG]"; 
const int GS_MAG_AMMO = 210; 

//Small Ship - Medium Cargo - Missiles Only 
const string MS_MAG = "[MSMG]"; 
const int MS_MAG_MISL = 56;

//Large Ship - Small Cargo/Small Ship - Large Cargo - Both Ammo Types
const string XL_MAG = "[XLMG]";
const int XL_MAG_AMMO = 301;
const int XL_MAG_MISL = 180;

//Large Ship - Small Cargo/Small Ship - Large Cargo - Bullets Only 
const string GL_MAG = "[GLMG]"; 
const int GL_MAG_AMMO = 976;

//Large Ship - Small Cargo/Small Ship - Large Cargo - Missiles Only 
const string ML_MAG = "[MLMG]"; 
const int ML_MAG_MISL = 260;

//Small Ship - Connector - Both Ammo Types
const string XC_MAG = "[XCMG]";
const int XC_MAG_AMMO = 34;
const int XC_MAG_MISL = 10;

//Small Ship - Connector - Bullets Only 
const string GC_MAG = "[GCMG]"; 
const int GC_MAG_AMMO = 72;

//Small Ship - Connector - Missiles Only 
const string MC_MAG = "[MCMG]";  
const int MC_MAG_MISL = 19;

//Small Ship - Small Cargo - Bullets Only
const string N_MAG = "[GNMG]";
const int N_MAG_AMMO = 7;

//Small Ship - Gattling Gun
const string G_WEP = "[GTGN]";
const int G_WEP_AMMO = 4;

//Small Ship - Small Reactor
const string SS_RE = "[SSRE]";
const int SS_FUEL = 20;

//Small Ship - Large Reactor
const string SL_RE = "[SLRE]";
const int SL_FUEL = 100;

//Large Ship - Small Reactor
const string LS_RE = "[LSRE]";
const int LS_FUEL = 200;

//Large Ship - Large Reactor
const string LL_RE = "[LLRE]";
const int LL_FUEL = 500;


//Runs "reload" function for all defined inventory types.
public void Main()
{
    reload(XS_MAG, XS_MAG_AMMO, XS_MAG_MISL);
    reload(GS_MAG, GS_MAG_AMMO, 0);
    reload(MS_MAG, 0, MS_MAG_MISL);

    reload(XL_MAG, XL_MAG_AMMO, XL_MAG_MISL); 
    reload(GL_MAG, GL_MAG_AMMO, 0); 
    reload(ML_MAG, 0, ML_MAG_MISL);

    reload(XC_MAG, XC_MAG_AMMO, XC_MAG_MISL); 
    reload(GC_MAG, GC_MAG_AMMO, 0); 
    reload(MC_MAG, 0, MC_MAG_MISL);

    reload(N_MAG, N_MAG_AMMO, 0);
    reload(G_WEP, G_WEP_AMMO, 0);

    refuel(SS_RE, SS_FUEL);
    refuel(SL_RE, SL_FUEL);
    refuel(LS_RE, LS_FUEL);
    refuel(LL_RE, LL_FUEL);
}


//Finds all inventories containing defined tag, and loads them with defined amounts of ammo.
void reload(string dest, int ammo_qty, int missile_qty)
{
    //Builds list of all source inventories.
    List<IMyTerminalBlock> source_list = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(SOURCE, source_list);

    //Builds list of all destination inventories.
    List<IMyTerminalBlock> dest_list = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(dest, dest_list);

    //Cycles through destination inventories.
    for (int c = 0; c < dest_list.Count; c++)
    {
        //Cycles through source inventories.
        for (int d = 0; d < source_list.Count; d++)
        {
            //Retrieve source and destination inventories.
            IMyInventory sourceInv = ((IMyInventoryOwner) source_list[d]).GetInventory(0); 
            IMyInventory destInv = ((IMyInventoryOwner) dest_list[c]).GetInventory(0); 

            //Load Selected Inventory With Ammo and Missiles
            ensureMinimumAmount(sourceInv, destInv, AMMO, ammo_qty);  
            ensureMinimumAmount(sourceInv, destInv, MISL, missile_qty); 
        }
    }
}


//Finds all reactors containing defined tag, and loads them with defined amounts of fuel. 
void refuel(string dest, int fuel_qty) 
{ 
    //Builds list of all source inventories. 
    List<IMyTerminalBlock> source_list = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.SearchBlocksOfName(SOURCE, source_list); 
 
    //Builds list of all destination inventories. 
    List<IMyTerminalBlock> dest_list = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.SearchBlocksOfName(dest, dest_list); 
 
    //Cycles through destination inventories. 
    for (int c = 0; c < dest_list.Count; c++) 
    { 
        //Cycles through source inventories. 
        for (int d = 0; d < source_list.Count; d++) 
        { 
            //Retrieve source and destination inventories. 
            IMyInventory sourceInv = ((IMyInventoryOwner) source_list[d]).GetInventory(0);  
            IMyInventory destInv = ((IMyInventoryOwner) dest_list[c]).GetInventory(0);  
 
            //Load Selected Reactor With Uranium 
            ensureMinimumAmount(sourceInv, destInv, FUEL, fuel_qty);               
        } 
    } 
}

//---------------------------------------------------------------------------------//
//ALL CODE BELOW THIS POINT WRITTEN BY PILOTERROR42//
//---------------------------------------------------------------------------------//

void ensureMinimumAmount(IMyInventory source, IMyInventory dest, string itemType, int num)
{
    while(!hasEnoughOfItem(dest, itemType, num))
    {
        int? index = indexOfItem(source, itemType);
        if(index == null)
            return;
        source.TransferItemTo(dest, (int) index, null, true, num - numberOfItemInContainer(dest, itemType));
    }
}


bool hasEnoughOfItem(IMyInventory inventoryToSearch, string itemName, int minAmount)
{
    return numberOfItemInContainer(inventoryToSearch, itemName) >= minAmount;
}


int numberOfItemInContainer(IMyInventory inventoryToSearch, string itemName)
{
    int total = 0;
    List<IMyInventoryItem> items = inventoryToSearch.GetItems();
    for (int c = 0; c < items.Count; c++)
    {
        if (items[c].Content.SubtypeId.ToString().Equals(itemName))
        {
            total += (int)(items[c].Amount);
        }
    }
    return total;
}


Nullable <int> indexOfItem(IMyInventory source, string item)
{
    List<IMyInventoryItem> items = source.GetItems();
    for (int c = 0; c < items.Count; c++)
    {
        if(items[c].Content.SubtypeId.ToString().Equals(item))
        {
            return c;
        }
    }
    return null;
}


