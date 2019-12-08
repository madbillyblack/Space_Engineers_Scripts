const string SOURCE = "MDW";
const string DEST = "MIU";
const string LCD = "Load Counter MDW";

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

public void Main(string argument)
{
    if (argument == "")
    {
        _loadCount += 1;
        List<IMyTerminalBlock> source_list = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName(SOURCE, source_list);

        List<IMyTerminalBlock> dest_list = new List<IMyTerminalBlock>();
        GridTerminalSystem.SearchBlocksOfName(DEST, dest_list);

        for (int c = 0; c<source_list.Count; c++)
        {
            var source = source_list[c];
            if (source.HasInventory)
            {
                var sourceInv = source.GetInventory(0);
                for (int d = 0; d<dest_list.Count; d++)
                {
                    var dest = dest_list[d];
                    if (dest.HasInventory)
                    {
                        var destInv = dest.GetInventory(0);
                        if (!destInv.IsFull)
                        {
                            List<IMyInventoryItem> items = sourceInv.GetItems();
                            for (int i =0; i<items.Count; i++)
                            {
                                IMyInventoryItem item = items[i];
                                if(item.Content.TypeId.ToString().Equals("MyObjectBuilder_Ore"))
                                {
                                    sourceInv.TransferItemTo(destInv, 0, null, true, null);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    else
    {
        string[] argArray = argument.Split(' ');
        string action = argArray[0];

        switch(action)
        {
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
        }
    }

    string loadNumber = _loadCount.ToString();

    List<IMyTerminalBlock> displays = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(LCD, displays);
    for(int d = 0; d<displays.Count; d++)
    {
        IMyTextPanel display = displays[d] as IMyTextPanel;
        display.WritePublicText("LOAD COUNT: " +  loadNumber);
    }
    Echo(loadNumber);
}
