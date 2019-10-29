///////////////////////////////////////////////
///											///
/// USAP - UNIVERSAL SHIP ASSISTANT PROGRAM ///
///											///
///////////////////////////////////////////////


//DEFINITIONS:
//Values and strings inside quotes can be changed here.


///////////////////////
/// USER CONSTANTS ///  User constants should be sufficient for basic program set up.
/////////////////////

const string SHIP_TAG = ""; // Label added to your terminal blocks to identify them as part of this ship

const string SOURCE = "[SRC]"; //Inventory that program will search for supplies (usually on a separate grid).

const bool SMALL_SHIP = true; //Specifies if ship to be reloaded is a small ship. For large ships set to false.

const string REF_BLOCK = "Connector [MCMG]"; //Name of reference block that program will reference for distance.
											 //-- Recommended: Connector or Merge Block

// TIMER CONSTANTS //
//-----------------//
//Names of timers to activate:
const string TIMER_A = "Door Timer"; 
const string TIMER_B = "Reload Timer";

//Program Run Argument that will activate timer
const string ARGUMENT_A = "DoorOpen";
const string ARGUMENT_B = "MissileReload";

//Max distance (in meters) that program will check for timer
const int REF_DIST = 12;



//////////////////////////
/// INVENTORY CONSTANTS /// You can tweek the values here if you want.  Default  values are based on 1x inventory setting.
/////////////////////////
const string AMMO = "NATO_25x184mm";
const string MISL = "Missile200mm";
const string FUEL = "Uranium";

//All following definition groups follow the format:
// const string CONSTANT = "Inventory Tag Here";
// const int AMMO = number of ammo here;


// LARGE SHIP - SMALL CARGO / SMALL SHIP - LARGE CARGO //
//-----------------------------------------------------//

//BULLETS ONLY//
const string GL_MAG = "[GLMG]"; 
const int GL_MAG_AMMO = 976;

//MISSILES ONLY// 
const string ML_MAG = "[MLMG]"; 
const int ML_MAG_MISL = 260;

//BOTH AMMO TYPES//
const string XL_MAG = "[XLMG]";
const int XL_MAG_AMMO = 301;
const int XL_MAG_MISL = 180;



//SMALL SHIP INVENTORIES//
//----------------------//

//---CONNECTOR---//

//BULLETS ONLY//
const string GC_MAG = "[GCMG]"; 
const int GC_MAG_AMMO = 72;

//MISSILES ONLY//
const string MC_MAG = "[MCMG]";  
const int MC_MAG_MISL = 19;

//BOTH AMMO TYPES//
const string XC_MAG = "[XCMG]";
const int XC_MAG_AMMO = 34;
const int XC_MAG_MISL = 10;


//---SMALL CARGO---//

//BULLETS ONLY//
const string N_MAG = "[GNMG]";
const int N_MAG_AMMO = 7;


//---MEDIUM CARGO---//

//BULLETS ONLY//
const string GS_MAG = "[GSMG]"; 
const int GS_MAG_AMMO = 210; 

//MISSILES ONLY//
const string MS_MAG = "[MSMG]"; 
const int MS_MAG_MISL = 56;

//BOTH AMMO TYPES//
const string XS_MAG = "[XSMG]";
const int XS_MAG_AMMO = 75;
const int XS_MAG_MISL = 36;


//---WEAPONS---//

//GATTLING GUN//
const string G_WEP = "[GTGN]";
const int G_WEP_AMMO = 4;

//RELOADABLE ROCKET LAUNCHER//
const string SR_WEP = "[SRKL]";
const int SR_WEP_MISL = 4;


//---REACTORS---//

//SMALL REACTOR//
const string SS_RE = "[SSRE]";
const int SS_FUEL = 20;

//LARGE REACTOR//
const string SL_RE = "[SLRE] TTc";
const int SL_FUEL = 100;



//LARGE SHIP INVENTORIES//
//----------------------//

//---WEAPONS---//

//ROCKET LAUNCHER//
const string LR_WEP = "[LRKL]";
const int LR_WEP_MISL = 19;

//---REACTORS---//

//SMALL REACTOR//
const string LS_RE = "[LSRE]";
const int LS_FUEL = 200;

//LARGE REACTOR//
const string LL_RE = "[LLRE]";
const int LL_FUEL = 500;



int _loadCount; 

public Program() 
{ 
    if (Storage.Length > 0) 
    { 
        _loadCount = int.Parse(Storage); 
    } 
    else 
    { 
        _loadCount = 0; 
    } 
 
 
 
    // The constructor, called only once every session and 
    // always before any other method is called. Use it to 
    // initialize your script.  
    //      
    // The constructor is optional and can be removed if not 
    // needed. 
} 

public void Save()  
{ 
    Storage = _loadCount.ToString(); 
    // Called when the program needs to save its state. Use 
    // this method to save your state to the Storage field 
    // or some other means.  
    //  
    // This method is optional and can be removed if not 
    // needed. 
}



////////////
/// MAIN ///
////////////

public void Main(string argument){
    
	string[] argArray = argument.Split(' ');
    string action = argArray[0];


    switch(action) 
    { 
        case ARGUMENT_A: 
            Activate(TIMER_A); 
            break; 
        case ARGUMENT_B: 
            Activate(TIMER_B); 
            break;
		case "ResetCount":
                _loadCount = 0;
                break;
        case "SetCount":
                if(argArray.Length > 1)
                {
                    string qty = argArray[1];
                    _loadCount = int.Parse(qty);
                }
                break;
			
        default:
			resupply();
            break;
    }
}


////////////////
/// ACTIVATE /// Finds and activates "Timer Block A" which is linked to detaching mechanisms.
////////////////

public void Activate(string trigger) 
{ 
    List<IMyTerminalBlock> timers_list = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.SearchBlocksOfName(trigger, timers_list); 
    IMyTerminalBlock ref_block = GridTerminalSystem.GetBlockWithName(REF_BLOCK) 
        as IMyTerminalBlock; 
    double max_dist = REF_DIST; 
    for(int i=0; i<timers_list.Count; i++) 
    { 
        var distance = Vector3D.Distance(ref_block.GetPosition(), timers_list[i].GetPosition()); 
        IMyTerminalBlock timer = timers_list[i] as IMyTimerBlock; 
        if (distance < max_dist) 
        { 
            timer.GetActionWithName("TriggerNow").Apply(timer); 
        } 
    } 
}


//////////////
/// RELOAD /// Finds all inventories containing defined tag, and loads them with defined amounts of ammo.
//////////////

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
		IMyTerminalBlock destBlock = dest_list[c];
		if (destBlock.HasInventory && destBlock.CustomName.Contains(SHIP_TAG)){

			//Cycles through source inventories.
			for (int d = 0; d < source_list.Count; d++){
			
				IMyTerminalBlock sourceBlock = source_list[d];
				if(sourceBlock.HasInventory){
				
					//Retrieve source and destination inventories.
					var sourceInv = sourceBlock.GetInventory(0); 
					var destInv = destBlock.GetInventory(0); 

					//Load Selected Inventory With Ammo and Missiles
					ensureMinimumAmount(sourceInv, destInv, AMMO, ammo_qty);  
					ensureMinimumAmount(sourceInv, destInv, MISL, missile_qty); 
				}
			}
		}
    }
}



//////////////
/// REFUEL /// Finds all reactors containing defined tag, and loads them with defined amounts of fuel.
//////////////

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
		IMyTerminalBlock destBlock = dest_list[c];
		if (destBlock.HasInventory && destBlock.CustomName.Contains(SHIP_TAG)){

			//Cycles through source inventories.
			for (int d = 0; d < source_list.Count; d++){
			
				IMyTerminalBlock sourceBlock = source_list[d];
				if(sourceBlock.HasInventory){
				
					//Retrieve source and destination inventories.
					var sourceInv = sourceBlock.GetInventory(0); 
					var destInv = destBlock.GetInventory(0); 

					//Load Selected Reactor With Uranium 
					ensureMinimumAmount(sourceInv, destInv, FUEL, fuel_qty);
				}
			}
		}
    }
}

////////////////
/// RESUPPLY /// Runs Reload and Refuel for specified ship size and drops off ore for mining ships.
////////////////

void resupply(){

//Large Ship - Small Cargo / Small Ship - Large Cargo
//Bullets Only
	reload(GL_MAG, GL_MAG_AMMO, 0); 
//Missiles Only
	reload(ML_MAG, 0, ML_MAG_MISL);
//Both Ammo Types
	reload(XL_MAG, XL_MAG_AMMO, XL_MAG_MISL); 

//Small Ship Inventories
	if(SMALL_SHIP == true){

	//CONNECTOR
	//Bullets Only
		reload(GC_MAG, GC_MAG_AMMO, 0);
	//Missiles Only
		reload(MC_MAG, 0, MC_MAG_MISL);
	//Both Ammo Types
		reload(XC_MAG, XC_MAG_AMMO, XC_MAG_MISL); 

	//SMALL CARGO - Bullet Only
	    reload(N_MAG, N_MAG_AMMO, 0);
	
	//MEDIUM CARGO
	//Bullets Only
		reload(GS_MAG, GS_MAG_AMMO, 0);
	//Missiles Only
		reload(MS_MAG, 0, MS_MAG_MISL);
	//Both Ammo Types
		reload(XS_MAG, XS_MAG_AMMO, XS_MAG_MISL);
		
	//WEAPONS
	//Gattling Gun
	    reload(G_WEP, G_WEP_AMMO, 0);
    //Reloadable Rocket Launcher
		reload(SR_WEP, 0, SR_WEP_MISL);
		
	//REACTORS
	//Small Reactor
	    refuel(SS_RE, SS_FUEL);
	//Large Reactor
        refuel(SL_RE, SL_FUEL);
	}else{
	
	//WEAPONS
	//Rocket Launcher
		reload(LR_WEP, 0, LR_WEP_MISL);
	
	//REACTORS
	//Small Reactor
		refuel(LS_RE, LS_FUEL);
	//Large Reactor
		refuel(LL_RE, LL_FUEL);
	}

}


//---------------------------------------------------//
// ALL CODE BELOW THIS POINT WRITTEN BY PILOTERROR42 //  With slight tweeks to keep current.
//---------------------------------------------------//

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
    List<MyInventoryItem> items = new List<MyInventoryItem>();
    inventoryToSearch.GetItems(items);
    for (int c = 0; c < items.Count; c++)
    {
        if (items[c].ToString().Equals(itemName))
        {
            total += (int)(items[c].Amount);
        }
    }
    return total;
}


Nullable <int> indexOfItem(IMyInventory source, string item)
{
    List<MyInventoryItem> items = new List<MyInventoryItem>();
    items.Clear();

    source.GetItems(items);
//source.GetItems();
    for (int c = 0; c < items.Count; c++)
    {
        if(items[c].ToString().Equals(item))
        {
            return c;
        }
    }
    return null;
}


