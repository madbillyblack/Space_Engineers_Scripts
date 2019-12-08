/*****************************************************\
/                                                                       \
/                   MadBillyBlack's                         \
/       PRESSURE LOCKDOWN PROTOCOL      \
/                                                                       \
/*****************************************************\

PURPOSE:
****************************************
*  Prevents undesired space walks by sealing doors between rooms that are pressurized and rooms that are not.

FEATURES:
****************************************
*   Closes all doors in a room when it depressurizes.
*   Checks the balance of pressure between adjacent rooms.
*   Locks doors between rooms that have different pressures.
*   Prints pressure status of room and adjacent rooms to LCDs at each door.
*   Switches to emergency lighting if a room is depressurized.

SET-UP:
***************************************
*   Build rooms with air vents, doors, lcd's, and lights as desired.

*   Place a Corner LCD or Flat LCD Top or Flat LCD Bottom on either side of each door between rooms.
    *Do not do this if the two rooms share an air vent.

*   Set up lighting to include lights that you want to turn on when the room is pressurized and lights that you want to turn on when it is
    depressurized.

*   Rename all blocks to include SECTOR TAGS associated with the room's that they are in. (See NAMING CONVENTIONS below).

ACTIONS:
*****************************************
*OPTION 1: Vent Triggered:
    *   Set up vent actions in each room to run this program with the following arguments:
        *   ACTION 1 (100% pressure): CheckDoors <SectorTag>
            *   Example: Vent S01, Action 1: Argument "CheckDoors S01"
        *   ACTION 2 (0% pressure): CloseDoors <SectorTag>
            *   Example: Vent S01, Action 2: Argument "CloseDoors S01"

*OPTION 2: Timer Loop:      
    *   Set up a program timer to run this program without an argument.
    *   WARNING: This will not automatically close doors.
        *   To close doors you will still need to put the 'CloseDoors <SectorTag>' argument into ACTION 2 of every vent.
        *   You could also close the doors by other means, like linking vent ACTION 2 to door groups.
 
ARGUMENTS: 
***************************************
*   CloseDoors <sectorTag>
    *   Set this on the second action (depressurize) for the sector's air vent.
    *   This closes all doors in the sector.

*   CheckDoors <sectorTag>
    *   Set this on the first action (pressurize) for the sector's air vent.
    *   This checks the pressure in adjacent rooms, and locks doors to rooms of unequal pressure.
    *   Also displays the pressure differences on designated LCDs.

*   OverrideDoor <sectTag1>_<secTag2>
    *   This is an optional command.
    *   If a door is locked because of unequal pressure between two sectors,
        this will depressurize the vents in both sectors, and unlock the door.
    *   An underscore '_' is REQUIRED between the sector tags in the argument.
        *   BAD: OverrideDoor S01 S02
        *   GOOD: OverrideDoor S01_S02

*   ResetOverride <secTag1>_<secTag2>
    *   This is also an optional argument.
    *   If Depressurize has been set to On OverrideDoor command, this will restore Depressurize to off for both sectors.
    *   The an underscore is also required between the sector tags in this command.



NAMING CONVENTIONS:
***************************************
* Rename all vents, lights, doors, and pressure lcds involved with this script to have SECTOR TAGS corresponding to their particular room.

    *   Suggested tags: S01, Sector1, SectorA, Room1, R01, etc.
    *   Do not include spaces in sector tag.  Use underscores if needed.
        * Bad: S 01
        * Good: S_01

*   VENTS:
    *   Air vents just require one sector tag.
    *   Example: Vent S01

*   DOORS:
    *   Doors require a sector tag for both of the sectors they connect.
        *   These sector tags should be separated from eachother by a [Space].
        *   Order of tags does not matter
        *   Example: Door S03 S02
    *   <<IMPORTANT>> The first part of the Door name should be only one word, i.e. not include any spaces.
        *   The tags need to be the second and third words in the door name.
        *   Bad:    Sliding Door S03 S04
        *   Good:   Sliding_Door S03 S04

*   LCD's:
    *   Like doors, LCD's need to have two sector tags, but here the order does matter.
        *   The first sector tag represents the sector that the LCD is actually located in.
        *   The second sector tag represents the sector on the other side of the door from this LCD.
        *   Example:
            *   LCD S03 S02 is located in Sector 3 next to Door S02 S03.
            *   On the other side of the door is LCD S02 S03, which is located in Sector 2.
    *   This script is designed for Corner LCD's as well as Flat LCD Tops and Flat LCD Bottoms.
        *   Probably works with regular LCD's and Text Panels as well.

*   LIGHTS:
    *   Lights that change based on pressure need to have sector tags followed by a [Space] a LIGHT TAG.
        * Default light tags: "w" and "r".
        * Light tags can be changed in the CONSTANTS section below.
    *   Light tag w is for lights that will be on when the room is pressurized and off when it is depressurized.
        *   Example: Light S01 w
    *   Light tag r is for lights that will be off when the room is pressurized and on when it is depressurizd.
        *Example: Light S01 r
    *   Light tags do not have to be followed immediately by a [space]
        *Example: Light S01 w13
    *   If you want a light to remain on, whether the room is depressurized or not, then do not include a light tag.

*   ADDITIONAL:

    *   All tagged blocks can include your own personal tags after the sector tags.
        * Example: "Door S04 S03 OMC"

*/
    
//////////////////
// CONSTANTS//
/////////////////
const string WHITE_TAG = "w";
/**
    *Tag for lights that turn on when sector is pressurized.

        * Place in light name, following Sector Tag and a [Space]. 
        * Does not need to be followed by a [Space].
        * Example: "Light S01 w01"  This is light 01 in sector 01.
        * Invalid: "Light S01w01".  This light will not be activated.
*/

const string RED_TAG = "r";
/**
    *Tag for lights that turn on when sector is depressurized.
 
        * Place in light name, following Sector Tag and a [Space].  
        * Does not need to be followed by a [Space]. 
        * Example: "Light S01 r01"  This is emergency light 01 in sector 01. 
        * Invalid: "Light S01r01".  This light will not be activated. 
*/

 
public void Main(string arg)
{
    //Default.  No argument.  This can be looped on a timer block.  This will NOT automatically close doors.
    if (arg == "")
    {
        DoorMonitor();
        MonitorLights();
    }
    else
    {
        string[] argArray = arg.Split(' ');
        string action = argArray[0];
        string sector = argArray[1];
    

        switch(action)
        {
            case "CloseDoors":
                DoorCheck(sector); 
                DoorClose(sector);
                UpdateLights(sector);
                break;
            case "CheckDoors":
                DoorCheck(sector);
                UpdateLights(sector);
                break;
            case "OverrideDoor":
                DoorOverride(sector);
                break;
            case "ResetOverride":
                OverrideReset(sector);
                break;
        }
    }
}


/**
*Gets all doors that contain sector tag in name and closes them.
*/
public void DoorClose(string secTag) 
{ 
    List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>();    
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);   
    for(int i=0; i<doors.Count; i++)   
    {   
        IMyDoor door = doors[i] as IMyDoor;   
        string name = door.CustomName;   
        if (name.Contains(secTag))   
        {   
            door.GetActionWithName("Open_Off").Apply(door);   
        }
    }
}


/**
*Gets all doors that contain sector tag in name, and checks the pressure on both sides.
    *Locks door if pressures are different.
    *Unlocks door if pressures are the same.
    *Calls LCDPrint method for LCD's on either side of the door.
*/
public void DoorCheck(string secTag)
{
    //Get all doors.  Continue if they contain secTag
    List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>();     
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);    
    for(int i=0; i<doors.Count; i++)    
    {    
        IMyDoor door = doors[i] as IMyDoor;    
        string name = door.CustomName;
        if (name.Contains(secTag))    
        {
                //Get both sector tags associated with door.
                string[] doorTags = name.Split(' ');
                string secA = doorTags[1];
                string secB = doorTags[2];

                //Update LCD Displays
                LCDPrint(secA, secB);
                LCDPrint(secB, secA);

                //Get all vents.  Find the one associated with secA, then secB, and continue.
                List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>();  
                GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents);  
                for (int j = 0; j<vents.Count; j++)  
                { 
                    IMyAirVent ventA = vents[j] as IMyAirVent;  
                    string ventNameA = ventA.CustomName;
                    if (ventNameA.Contains(secA))  
                    {  
                        for (int c = 0; c<vents.Count; c++) 
                        { 
                            IMyAirVent ventB = vents[c] as IMyAirVent;
                            string ventNameB = ventB.CustomName; 
                            if(ventNameB.Contains(secB)) 
                            {
                                // Get pressure from both ventA and ventB.  Lock door if different.  Unlock if the same.
                                if(ventA.GetOxygenLevel() > 0 && ventB.GetOxygenLevel() > 0)
                                {
                                    door.GetActionWithName("OnOff_On").Apply(door); 
                                }
                                else if(ventA.GetOxygenLevel() == 0 && ventB.GetOxygenLevel() == 0)
                                {
                                    door.GetActionWithName("OnOff_On").Apply(door); 
                                }
                                else if(ventA.GetOxygenLevel() > 0 && ventB.GetOxygenLevel() == 0) 
                                { 
                                     door.GetActionWithName("OnOff_Off").Apply(door); 
                                }
                                else if(ventA.GetOxygenLevel() == 0 && ventB.GetOxygenLevel() > 0) 
                                { 
                                     door.GetActionWithName("OnOff_Off").Apply(door); 
                                }
                            }
                        }
                    }
                }
            }
        }   
    }


/*
*  This is the default method (no argument) that will check vent pressures for all doors on the station, provided that the doors adhere to
*  the established naming conventions.  This method will not automatically shut the doors.  Closing the doors will have to be achieved
*   by another means, such as the DoorClose method.
*/
public void DoorMonitor() 
{ 
    //Get all doors.
    List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>();      
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);     
    for(int i=0; i<doors.Count; i++)     
    {     
        IMyDoor door = doors[i] as IMyDoor;     
        string name = door.CustomName;

        string[] doorTags = name.Split(' '); 
        if (doorTags.Length>2)     
        { 
                //Get both sector tags associated with door. 

                string secA = doorTags[1]; 
                string secB = doorTags[2]; 
 
                //Update LCD Displays 
                LCDPrint(secA, secB); 
                LCDPrint(secB, secA); 
 
                //Get all vents.  Find the one associated with secA, then secB, and continue. 
                List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>();   
                GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents);   
                for (int j = 0; j<vents.Count; j++)   
                {  
                    IMyAirVent ventA = vents[j] as IMyAirVent;   
                    string ventNameA = ventA.CustomName; 
                    if (ventNameA.Contains(secA))   
                    {
                        for (int c = 0; c<vents.Count; c++)  
                        {  
                            IMyAirVent ventB = vents[c] as IMyAirVent; 
                            string ventNameB = ventB.CustomName;  
                            if(ventNameB.Contains(secB))  
                            { 
                                // Get pressure from both ventA and ventB.  Lock door if different.  Unlock if the same. 
                                if(ventA.GetOxygenLevel() > 0 && ventB.GetOxygenLevel() > 0) 
                                { 
                                    door.GetActionWithName("OnOff_On").Apply(door);  
                                } 
                                else if(ventA.GetOxygenLevel() == 0 && ventB.GetOxygenLevel() == 0) 
                                { 
                                    door.GetActionWithName("OnOff_On").Apply(door);  
                                } 
                                else if(ventA.GetOxygenLevel() > 0 && ventB.GetOxygenLevel() == 0)  
                                {  
                                     door.GetActionWithName("OnOff_Off").Apply(door);  
                                } 
                                else if(ventA.GetOxygenLevel() == 0 && ventB.GetOxygenLevel() > 0)  
                                {  
                                     door.GetActionWithName("OnOff_Off").Apply(door);  
                                } 
                            } 
                        } 
                    } 
                } 
            } 
        }    
    }



/**
 * Gets all lights in sector, and updates them based on vent status.
 * 
 * If pressurized, white lights are on, red lights off.
 * If not pressurized, white lights are off, red lights on.
 */
public void UpdateLights(string secTag)
{
    //Get all vents.  If vent contains secTag continue.
    List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents);
    for(int i = 0; i<vents.Count; i++)
    {
        IMyAirVent vent = vents[i] as IMyAirVent;
        string ventName = vent.CustomName;
        if (ventName.Contains(secTag))
        {
            //Get all white lights with secTag.
            List<IMyTerminalBlock> whiteLights = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(secTag + " " + WHITE_TAG, whiteLights);

            //Get all red lights with secTag.
            List<IMyTerminalBlock> redLights = new List<IMyTerminalBlock>();  
            GridTerminalSystem.SearchBlocksOfName(secTag + " " + RED_TAG, redLights);
 
            //If sector pressurized: turn white lights on and red lights off. Otherwise, turn white lights off and red lights on.
            if (vent.GetOxygenLevel()>0)
            {
                for(int j = 0; j<whiteLights.Count; j++)
                {
                    IMyInteriorLight wLight = whiteLights[j] as IMyInteriorLight;
                    wLight.GetActionWithName("OnOff_On").Apply(wLight);
                }
                for(int c = 0; c<redLights.Count; c++)
                {
                    IMyInteriorLight rLight = redLights[c] as IMyInteriorLight; 
                    rLight.GetActionWithName("OnOff_Off").Apply(rLight);
                }
            }
            else
            {
                for(int j = 0; j<whiteLights.Count; j++) 
                { 
                    IMyInteriorLight wLight = whiteLights[j] as IMyInteriorLight; 
                    wLight.GetActionWithName("OnOff_Off").Apply(wLight); 
                } 
                for(int c = 0; c<redLights.Count; c++) 
                { 
                    IMyInteriorLight rLight = redLights[c] as IMyInteriorLight;  
                    rLight.GetActionWithName("OnOff_On").Apply(rLight); 
                }
            }

        }
    }
}

/*
*   A default method that does not require an argument.  This will check pressure in all vents on the station and change the lights accordingly.
*   White lights turn on when pressurized, and red lights when depressurized.
*/
public void MonitorLights() 
{ 
    //Get all vents.  If vent contains secTag continue. 
    List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents); 
    for(int i = 0; i<vents.Count; i++) 
    { 
        IMyAirVent vent = vents[i] as IMyAirVent; 
        string[] ventNames = vent.CustomName.Split();
         
        if (ventNames.Length>1) 
        {
            string secTag = ventNames[1]; 

            //Get all white lights with secTag.
            List<IMyTerminalBlock> whiteLights = new List<IMyTerminalBlock>(); 
            GridTerminalSystem.SearchBlocksOfName(secTag + " " + WHITE_TAG, whiteLights); 
 
            //Get all red lights with secTag. 
            List<IMyTerminalBlock> redLights = new List<IMyTerminalBlock>();   
            GridTerminalSystem.SearchBlocksOfName(secTag + " " + RED_TAG, redLights); 
  
            //If sector pressurized: turn white lights on and red lights off. Otherwise, turn white lights off and red lights on. 
            if (vent.GetOxygenLevel()>0) 
            { 
                for(int j = 0; j<whiteLights.Count; j++) 
                { 
                    IMyInteriorLight wLight = whiteLights[j] as IMyInteriorLight; 
                    wLight.GetActionWithName("OnOff_On").Apply(wLight); 
                } 
                for(int c = 0; c<redLights.Count; c++) 
                { 
                    IMyInteriorLight rLight = redLights[c] as IMyInteriorLight;  
                    rLight.GetActionWithName("OnOff_Off").Apply(rLight); 
                } 
            } 
            else 
            { 
                for(int j = 0; j<whiteLights.Count; j++)  
                {  
                    IMyInteriorLight wLight = whiteLights[j] as IMyInteriorLight;  
                    wLight.GetActionWithName("OnOff_Off").Apply(wLight);  
                }  
                for(int c = 0; c<redLights.Count; c++)  
                {  
                    IMyInteriorLight rLight = redLights[c] as IMyInteriorLight;   
                    rLight.GetActionWithName("OnOff_On").Apply(rLight);  
                } 
            } 
 
        } 
    } 
}


/**
*   Sets the vents in two sectors to Depressurize, and unlocks the door between them.
*/
public void DoorOverride(string sectors)
{
    //Get the sector tags from the argument.
    string[] sectorArr = sectors.Split('_');
    string secA = sectorArr[0];
    string secB = sectorArr[1];
    
    //Get the vents from both sectors and set them to "Depressurize".
    List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>(); 
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents); 
    for(int i = 0; i<vents.Count; i++) 
    { 
        IMyAirVent vent = vents[i] as IMyAirVent; 
        string ventName = vent.CustomName; 
        if (ventName.Contains(secA) || ventName.Contains(secB))
        {
            Echo(ventName);
            vent.GetActionWithName("Depressurize_On").Apply(vent);
        }
    }

    //Finds the door between Sector A and Sector B, then powers it on.
    List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>();      
    GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors);     
    for(int c=0; c<doors.Count; c++)     
    {     
        IMyDoor door = doors[c] as IMyDoor;     
        string name = door.CustomName; 
        if (name.Contains(secA) && name.Contains(secB))
        {
            Echo(name);
            door.GetActionWithName("OnOff_On").Apply(door);
        }
    }
}


/**
*   Restores Depressurize to "Off" in two sectors.
*/
public void OverrideReset(string sectors) 
{ 
    string[] sectorArr = sectors.Split('_'); 
    string secA = sectorArr[0]; 
    string secB = sectorArr[1]; 
    List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>();  
    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents);  
    for(int i = 0; i<vents.Count; i++)  
    {  
        IMyAirVent vent = vents[i] as IMyAirVent;  
        string ventName = vent.CustomName;  
        if (ventName.Contains(secA) || ventName.Contains(secB)) 
        { 
            Echo(ventName); 
            vent.GetActionWithName("Depressurize_Off").Apply(vent); 
        } 
    } 
}


/**
*Gets LCD on ONE side of door, checks pressures, then updates display.
*/
public void LCDPrint(string sec1, string sec2)  
{
    //Get LCD with tag: sec1 sec2
    string lcdName = "" + sec1 + " " + sec2;

    List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcds);
    for (int i = 0; i<lcds.Count; i++)
    {
        IMyTextPanel lcd = lcds[i] as IMyTextPanel;
        string label = lcd.CustomName;
        if (label.Contains(lcdName))
        {
             //Get Vents with tags sec1 and sec2 and assign them as vent1 and vent2 
            List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyAirVent>(vents); 
            for (int j = 0; j<vents.Count; j++) 
            {
                IMyAirVent vent1 = vents[j] as IMyAirVent; 
                string ventName1 = vent1.CustomName; 
                if (ventName1.Contains(sec1)) 
                { 
                    for (int c = 0; c<vents.Count; c++)
                    {
                        IMyAirVent vent2 = vents[c] as IMyAirVent;
                        string ventName2 = vent2.CustomName;
                        if(ventName2.Contains(sec2))
                        {
                                //pixel constants: 
                                string w = "";     // constant white pixel 
                                string b = "";     // constant black pixel 
                                string y = "";     // constant yellow pixel 
 
                                //pixel variables:
                                string g = "";  // green on/off pixel (on = green   off = black) 
                                string r = "";  // red on/off pixel 
                                string o = "";  // green/red overlap pixel 
                                string k = "";  // left display blue on/off pixel 
                                string l = "";  // right display blue on/off pixel 
                                string h = "";  // left display white on/off pixel 
                                string u = "";  // right display white on/off pixel

                                //Check Pressure between sec1 and sec2.  Update variable pixels depending on states.
                                if(vent1.GetOxygenLevel() > 0 && vent2.GetOxygenLevel() > 0)
                                {
                                    g = ""; 
                                    r = ""; 
                                    o = ""; 
                                    k = ""; 
                                    l = ""; 
                                    h = ""; 
                                    u = "";
                                }
                                else if(vent1.GetOxygenLevel() == 0 && vent2.GetOxygenLevel() == 0)
                                {
                                    g = "";  
                                    r = "";  
                                    o = ""; 
                                    k = ""; 
                                    l = ""; 
                                    h = ""; 
                                    u = "";  
                                }
                                else if(vent1.GetOxygenLevel() > 0 && vent2.GetOxygenLevel() == 0)
                                {
                                    g = ""; 
                                    r = ""; 
                                    o = ""; 
                                    k = ""; 
                                    l = ""; 
                                    h = ""; 
                                    u = "";
                                }
                                else if(vent1.GetOxygenLevel() == 0 && vent2.GetOxygenLevel() > 0)
                                {
                                    g = "";  
                                    r = "";  
                                    o = ""; 
                                    k = ""; 
                                    l = ""; 
                                    h = ""; 
                                    u = "";
                                }

                                //This is the common LCD print out that changes depending on the defined pixel variables.
                                lcd.WritePublicText( 
                                    // 01
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 02
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 03
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 04
                                    w+w+w+w+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+
                                    k+k+k+k+k+k+k+k+  
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+w+w+w+w+"\n"+ 
    
                                    // 05
                                    w+w+w+w+k+k+y+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+
                                    k+k+k+k+k+k+k+k+   
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+w+w+w+w+"\n"+ 
 
                                    // 06
                                    w+w+w+w+k+y+y+y+k+k+h+h+k+k+k+k+h+h+h+k+k+k+k+k+k+w+w+w+k+k+k+k+w+w+k+k+k+k+k+k+k+w+w+w+
                                    w+w+k+k+k+k+k+k+k+k+k+ 
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+l+l+u+u+u+l+l+l+l+l+l+w+w+w+l+l+l+l+w+w+l+l+l+l+l+l+l+w+w+w+w+w+l+l+l+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
 
                                    // 07
                                    w+w+w+w+k+k+y+k+k+h+h+h+k+k+k+h+h+h+h+h+k+k+k+k+w+w+w+w+w+k+k+k+w+w+k+k+k+k+k+k+k+w+w+w+
                                    w+w+w+k+k+k+k+k+k+k+k+ 
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+u+u+u+l+l+l+u+u+u+u+u+l+l+l+l+w+w+w+w+w+l+l+l+w+w+l+l+l+l+l+l+l+w+w+w+w+w+w+l+l+l+l+l+l+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 08
                                    w+w+w+w+k+k+k+k+h+h+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+k+k+
                                    k+w+w+k+k+k+k+k+k+k+ 
                                    w+w+w+b+b+b+b+b+b+b+b+b+b+b+b+b+b+b+b+w+w+w+ 
                                    l+l+l+l+u+u+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+l+l+l+w+w+l+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
 
                                    // 09
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+k+k+
                                    k+k+w+w+k+k+k+k+k+k+ 
                                    w+w+w+b+b+r+r+b+b+g+b+b+g+b+b+r+r+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+l+l+l+l+w+w+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
 
                                    // 10
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+k+k+
                                    k+k+w+w+k+k+k+k+k+k+ 
                                    w+w+w+b+b+b+r+r+g+g+b+b+g+g+r+r+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+l+l+l+l+w+w+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
  
                                    // 11
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+k+k+
                                    k+k+w+w+k+k+k+k+k+k+ 
                                    w+w+w+b+b+b+b+o+o+b+b+b+b+o+o+b+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+l+l+l+l+w+w+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
 
                                    // 12
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+k+k+
                                    k+w+w+k+k+k+k+k+k+k+ 
                                    w+w+w+b+b+b+g+g+r+r+b+b+r+r+g+g+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+l+l+l+w+w+l+l+l+l+l+l+l+w+w+
                                    w+w+"\n"+ 
 
                                    // 13
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+k+k+k+k+w+w+w+
                                    w+w+w+k+k+k+k+k+k+k+k+ 
                                    w+w+w+b+b+g+g+b+b+r+r+r+r+b+b+g+g+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+l+l+l+w+w+w+w+w+w+l+l+l+l+l+l+l+l+w+
                                    w+w+w+"\n"+ 
 
                                    // 14   
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+k+w+k+k+k+w+w+w+
                                    w+w+k+k+k+k+k+k+k+k+k+ 
                                    w+w+w+b+g+g+b+b+b+b+r+r+b+b+b+b+g+g+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+w+l+l+l+w+w+w+w+w+l+l+l+l+l+l+l+l+l+w+
                                    w+w+w+"\n"+ 
 
                                    // 15
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+w+w+k+k+k+w+w+k+
                                    k+k+k+k+w+w+k+k+k+k+k+ 
                                    w+w+w+b+b+g+g+b+b+b+r+r+b+b+b+g+g+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+w+w+l+l+l+w+w+l+l+l+l+l+w+w+l+l+l+l+l+w+
                                    w+w+w+"\n"+ 
 
                                    // 16
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+w+w+k+k+k+k+w+w+k+
                                    k+k+k+w+w+w+w+k+w+k+k+ 
                                    w+w+w+b+b+b+g+g+b+r+r+r+r+b+g+g+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+w+w+l+l+l+l+w+w+l+l+l+l+w+w+w+w+l+w+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 17
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+w+w+k+k+k+k+k+w+w+k+
                                    k+k+w+w+k+k+w+w+w+k+k+ 
                                    w+w+w+b+b+b+b+g+o+r+b+b+r+o+g+b+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+w+w+l+l+l+l+l+w+w+l+l+l+w+w+l+l+w+w+w+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 18
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+w+k+k+k+k+k+k+w+w+k+
                                    k+w+w+k+k+k+k+w+w+k+k+ 
                                    w+w+w+b+b+b+b+r+o+g+b+b+g+o+r+b+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+w+l+l+l+l+l+l+w+w+l+l+w+w+l+l+l+l+w+w+l+l+w+
                                    w+w+w+"\n"+ 
 
                                    // 19
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+w+w+k+k+k+k+k+w+w+k+
                                    k+w+w+k+k+k+k+w+w+k+k+ 
                                    w+w+w+b+b+b+r+r+b+g+b+b+g+b+r+r+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+w+w+l+l+l+l+l+w+w+l+l+w+w+l+l+l+l+w+w+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 20
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+w+w+k+k+k+k+w+w+k+
                                    k+w+w+k+k+k+k+w+w+k+k+ 
                                    w+w+w+b+b+r+r+b+b+b+b+b+b+b+b+r+r+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+w+w+l+l+l+l+w+w+l+l+w+w+l+l+l+l+w+w+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 21
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+h+h+k+k+k+h+h+k+k+w+w+k+k+k+w+w+k+k+w+w+k+k+w+w+k+k+k+w+w+k+
                                    k+k+w+w+k+k+w+w+w+k+k+ 
                                    w+w+w+b+b+b+b+b+b+b+b+b+b+b+b+b+b+b+b+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+u+u+l+l+l+u+u+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+w+w+l+l+l+w+w+l+l+l+w+w+l+l+w+w+w+l+l+
                                    w+w+w+w+"\n"+ 
 
                                    // 22
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+k+h+h+h+h+h+k+k+k+k+w+w+w+w+w+k+k+k+w+w+k+k+k+w+w+k+k+w+w+k+
                                    k+k+k+w+w+w+w+w+w+k+k+ 
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+l+u+u+u+u+u+l+l+l+l+w+w+w+w+w+l+l+l+w+w+l+l+l+w+w+l+l+w+w+l+l+l+l+w+w+w+w+w+w+
                                    l+l+w+w+w+w+"\n"+ 
 
                                    // 23
                                    w+w+w+w+k+k+k+k+k+k+h+h+k+k+k+k+h+h+h+k+k+k+k+k+k+w+w+w+k+k+k+k+w+w+k+k+k+k+w+w+k+w+w+k+
                                    k+k+k+k+w+w+k+w+w+k+k+ 
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+ 
                                    l+l+l+l+l+l+u+u+l+l+l+l+u+u+u+l+l+l+l+l+l+w+w+w+l+l+l+l+w+w+l+l+l+l+w+w+l+w+w+l+l+l+l+l+w+w+l+w+w+l+l+  
                                    w+w+w+w+"\n"+ 
 
                                    // 24
                                    w+w+w+w+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+
                                    k+k+k+k+k+k+k+k+   
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+  
                                    l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+w+w+w+w+"\n"+ 
 
                                    // 25
                                    w+w+w+w+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+k+
                                    k+k+k+k+k+k+k+k+    
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+   
                                    l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+l+w+w+w+w+"\n"+ 
  
                                    // 26
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 27
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 28
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 29
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 
                                    // 30
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n" 
                                    );
                        }
                    } 
                } 
            }
        }
    }    
}
