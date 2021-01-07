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

*   OpenLock <sectorTag>
    *   This argument is for exterior airlocks and hangars that you may frequently want to depressurize.
    *   Locks and hangars will need two extra components:
        * Vaccuum Vent - Placed on outside of station.
	        * Only need one per station.
          * Should have its own special sector tag (default 'X00')
        * Lock Timer
	  * You need one dedicated timer for each hangar or airlock.
          * Include respective sector tag in timer's name.
          * TIMER ACTIONS:
            * Call this program with argument "OverrideDoor <sectorTag>_<vaccuumTag>"
            * Open exterior doors of hangar/lock

*   CloseLock <sectorTag>
    *   Closes an airlock that's been opened.

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
    *   You can include an exterior 'Vaccuum Vent'.
        * This script uses the Vaccuum Vent as a reference for balancing the pressure
          in airlocks and hangars.
        * The Vaccuum Vent should have its own special tag (default 'X00')
        * Vaccuum Vent tag can be changed in the 'CONSTANTS' section below.

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
// CONSTANTS //
//////////////////

const string SHIPTAG = "AQB";
/**
    *This is the tag used to distinguish terminal blocks of a specific ship
    *Needs to be included in all vents, doors, timers, connectors, and lights involved in this script.
*/

const string VACTAG = "X0X";
/**
    Sector Tag for exterior 'Vaccuum  Vent'
    * This vent acts as a reference for the "OpenLock" and "CloseLock" functions
*/

const string EXIT_TAG = "[x]";

const int LOCK_TIME = 5;

const int MERGE_DIST = 4;


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

public class Vessel
{
	public String vesselTag;
	public List<IMyAirVent> vents = new List<IMyAirVent>();
	public List<IMyDoor> doors = new List<IMyDoor>();
	public List<IMyInteriorLight> lights = new List<IMyInteriorLight>();
	public List<IMyTextPanel> lcds = new List<IMyTextPanel>();
	public List<IMyTimerBlock> timers = new List<IMyTimerBlock>();
	public List<IMySoundBlock> alarms = new List<IMySoundBlock>();
	public List<IMyShipConnector> connectors = new List<IMyShipConnector>();
	public List<IMyShipMergeBlock> mergeBlocks = new List<IMyShipMergeBlock>();
	
	public Vessel(String arg)
	{
		this.vesselTag = arg;
	}
}



public Program()
{
      //  DoorMonitor(myVessel);
      // MonitorLights(myVessel);
}

public void Main(string arg)
{
  //Default.  No argument.  This can be looped on a timer block.  This will NOT automatically close doors.

//COLLECT ALL ASSETS - Collect bloccks into general "All" lists, then add blocks with the SHIPTAG to "My" lists.

	
	Vessel myVessel = new Vessel(SHIPTAG);
	Vessel dockedVessel = new Vessel("MB3");
	BuildShip(myVessel);
	BuildShip(dockedVessel);
	
	//VENTS
    List<IMyAirVent> my_vents = new List<IMyAirVent>();
	Echo("VENTS - Docked: " + dockedVessel.vents.Count + "  Grid: " + myVessel.vents.Count);
	
	//DOORS
	List<IMyDoor> my_doors = new List<IMyDoor>();
	Echo("DOORS - Docked: " + dockedVessel.doors.Count + "  Grid: " + myVessel.doors.Count);
	
	//LIGHTS
	List<IMyInteriorLight> my_lights = new List<IMyInteriorLight>();
	Echo("LIGHTS - Docked: " + dockedVessel.lights.Count + "  Grid: " + myVessel.lights.Count);

	//LCDs
	List<IMyTextPanel> my_lcds = new List<IMyTextPanel>();

	
	//TIMERS
	List<IMyTimerBlock> my_timers = new List<IMyTimerBlock>();
	Echo("TIMERS - Docked: " + dockedVessel.timers.Count + "  Grid: " + myVessel.timers.Count);
	
	//ALARMS
	List<IMySoundBlock> my_alarms = new List<IMySoundBlock>();
	Echo("ALARMS - Docked: " + dockedVessel.alarms.Count + "  Grid: " + myVessel.alarms.Count);
	
	//CONNECTORS
	Echo("CONNECTORS - Docked: " + dockedVessel.connectors.Count + "  Grid: " + myVessel.connectors.Count);
	
	//MERGE BLOCKS
	Echo("MERGE CLAMPS - Docked: " + dockedVessel.mergeBlocks.Count + "  Grid: " + myVessel.mergeBlocks.Count);

	
	
	if (arg == "")
    {
        DoorMonitor(myVessel);
        MonitorLights(myVessel);
    }
    else
    {
        string[] argArray = arg.Split(' ');
        string action = argArray[0];
        string sector = argArray[1];
    

        switch(action)
        {
            case "CloseDoors":
                DoorCheck(myVessel, sector); 
                DoorClose(myVessel, sector);
                UpdateLights(myVessel, sector);
                break;
            case "CheckDoors":
                DoorCheck(myVessel, sector);
                UpdateLights(myVessel, sector);
                break;
            case "OverrideDoor":
                DoorOverride(myVessel, sector);
                break;
            case "ResetOverride":
                OverrideReset(myVessel, sector);
                break;
	           case "OpenLock":
                OpenLock(myVessel, sector);
                break;
            case "CloseLock":
                CloseLock(myVessel, sector);
                break;
            case "CycleLock":
                CycleLock(myVessel, sector);
                break;
			case "Timer":
				TimerCall(myVessel, sector);
				break;
        }
    }
}


public void BuildShip(Vessel vessel)
{
	String shipTag = vessel.vesselTag;
	
	
	//ADD VENTS
	List<IMyAirVent> all_vents = new List<IMyAirVent>();
	GridTerminalSystem.GetBlocksOfType<IMyAirVent>(all_vents);
	for(int v = 0; v < all_vents.Count; v++)
	{
		IMyAirVent vent = all_vents[v];
		if (vent.CustomName.Contains(shipTag))
		{
			vessel.vents.Add(vent);
		}
	}
		
	//ADD DOORS
	List<IMyDoor> all_doors = new List<IMyDoor>();
	GridTerminalSystem.GetBlocksOfType<IMyDoor>(all_doors);
	for(int d = 0; d < all_doors.Count; d++)
	{
		IMyDoor door = all_doors[d];
		if(door.CustomName.Contains(shipTag))
		{
			vessel.doors.Add(door);
		}
	}

	//LIGHTS
	List<IMyInteriorLight> all_lights = new List<IMyInteriorLight>();
	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(all_lights);
	for(int l = 0; l < all_lights.Count; l++)
	{
		IMyInteriorLight light = all_lights[l];
		if(light.CustomName.Contains(shipTag))
		{
			vessel.lights.Add(light);
		} 
	}		

	//LCDs
	List<IMyTextPanel> all_lcds = new List<IMyTextPanel>();
	GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(all_lcds);
	for(int c = 0; c < all_lcds.Count; c++)
	{
		IMyTextPanel lcd = all_lcds[c];
		if(lcd.CustomName.Contains(shipTag))
		{
			vessel.lcds.Add(lcd);
		}
	}
		
	//TIMERS
	List<IMyTimerBlock> all_timers = new List<IMyTimerBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyTimerBlock>(all_timers);
	for(int t = 0; t < all_timers.Count; t++)
	{
		IMyTimerBlock timer = all_timers[t];
		if(timer.CustomName.Contains(shipTag))
		{
			vessel.timers.Add(timer);
		}
	}
		
	//ALARMS
	List<IMySoundBlock> all_alarms = new List<IMySoundBlock>();
	GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(all_alarms);
	for(int a = 0; a < all_alarms.Count; a++)
	{
		IMySoundBlock alarm = all_alarms[a];
		if(alarm.CustomName.Contains(shipTag))
		{
			vessel.alarms.Add(alarm);
		}
	}
		
	//CONNECTORS
	List<IMyShipConnector> all_connectors = new List<IMyShipConnector>();
	GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(all_connectors);
	for(int c = 0; c < all_connectors.Count; c++)
	{
		IMyShipConnector connector = all_connectors[c];
		if(connector.CustomName.Contains(shipTag))
		{
			vessel.connectors.Add(connector);
		}
	}		
	
	//MERGE BLOCKS
	List<IMyShipMergeBlock> all_mergeBlocks = new List<IMyShipMergeBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(all_mergeBlocks);
	for(int m = 0; m < all_mergeBlocks.Count; m++)
	{
		IMyShipMergeBlock mergeBlock = all_mergeBlocks[m];
		if(mergeBlock.CustomName.Contains(shipTag))
		{
			vessel.mergeBlocks.Add(mergeBlock);
		}
	}

}


/**  DOOR CLOSE
**********************
*Gets all doors that contain sector tag in name and closes them.
*/
public void DoorClose(Vessel vessel, string secTag) 
{  
    for(int d=0; d<vessel.doors.Count; d++)   
    {   
        IMyDoor door = vessel.doors[d] as IMyDoor;   
        string name = door.CustomName;   
        if (name.Contains(secTag))   
        {   
            door.GetActionWithName("Open_Off").Apply(door);   
        }
    }
}



/**   DOOR CHECK
************************
*Gets all doors that contain sector tag in name, and checks the pressure on both sides.
    *Locks door if pressures are different.
    *Unlocks door if pressures are the same.
    *Calls LCDPrint method for LCD's on either side of the door.
*/
public void DoorCheck(Vessel vessel, string secTag)
{   
    for(int d=0; d < vessel.doors.Count; d++)    
    {    
        IMyDoor door = vessel.doors[d] as IMyDoor;    
        string name = door.CustomName;
        if (name.Contains(secTag))    
        {
                //Get both sector tags associated with door.
                string[] doorTags = name.Split(' ');
                string secA = doorTags[1];
                string secB = doorTags[2];

                //Update LCD Displays
                LCDPrint(vessel, secA, secB);
                LCDPrint(vessel, secB, secA);

                //Cycle through vents. Find the one associated with secA, then secB, and continue.
                for (int j = 0; j<vessel.vents.Count; j++)  
                { 
                    IMyAirVent ventA = vessel.vents[j] as IMyAirVent;  
                    string ventNameA = ventA.CustomName;
                    if (ventNameA.Contains(secA))  
                    {  
                        for (int c = 0; c < vessel.vents.Count; c++) 
                        { 
                            IMyAirVent ventB = vessel.vents[c] as IMyAirVent;
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

	BuildDock(vessel, secTag);
		
    }



/**   DOOR MONITOR (DEFAULT)
*****************************************
*  This is the default method (no argument) that will check vent pressures for all doors on the station, provided that the doors adhere to
*  the established naming conventions.  This method will not automatically shut the doors.  Closing the doors will have to be achieved
*   by another means, such as the DoorClose method.
*/
public void DoorMonitor(Vessel vessel) 
{
	for(int v = 0; v < vessel.vents.Count; v++)
	{
		string ventName = vessel.vents[v].CustomName;
		if(!ventName.Contains(VACTAG))
		{
			string[] ventInfo = ventName.Split(' ');
			if(ventInfo.Length > 1)
			{
				string secTag = ventInfo[1];
				DoorCheck(vessel, secTag);
			}
		}
	}
/** 
    for(int d = 0; d < vessel.doors.Count; d++)     
    {     
        IMyDoor door = vessel.doors[d] as IMyDoor;     
        string name = door.CustomName;

        string[] doorTags = name.Split(' '); 
        if (doorTags.Length>2)     
        { 
                //Get both sector tags associated with door. 

                string secA = doorTags[1]; 
                string secB = doorTags[2]; 
 
                //Update LCD Displays 
                LCDPrint(vessel, secA, secB); 
                LCDPrint(vessel, secB, secA); 
 
                //Cycle through vents.  Find the one associated with secA, then secB, and continue.   
                for (int v = 0; v < vessel.vents.Count; v++)   
                {  
                    IMyAirVent ventA = vessel.vents[v] as IMyAirVent;   
                    string ventNameA = ventA.CustomName; 
                    if (ventNameA.Contains(secA))   
                    {
                        for (int u = 0; u < vessel.vents.Count; u++)  
                        {  
                            IMyAirVent ventB = vessel.vents[u] as IMyAirVent; 
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
    } */		
}



/**  UPDATE LIGHTS
********************************
 * Gets all lights in sector, and updates them based on vent status.
 * 
 * If pressurized, white lights are on, red lights off.
 * If not pressurized, white lights are off, red lights on.
 */
public void UpdateLights(Vessel vessel, string secTag)
{
    //Get all vents.  If vent contains secTag continue.
    for(int v = 0; v < vessel.vents.Count; v++)
    {
        IMyAirVent vent = vessel.vents[v] as IMyAirVent;
        string ventName = vent.CustomName;
        if (ventName.Contains(secTag))
        {
            //Go through lights and sort sector lights into red and white light lists.
            List<IMyTerminalBlock> whiteLights = new List<IMyTerminalBlock>();
			List<IMyTerminalBlock> redLights = new List<IMyTerminalBlock>();
			for(int l = 0; l < vessel.lights.Count; l++)
			{
				IMyInteriorLight light = vessel.lights[l];
				string lightName = light.CustomName;
				if(lightName.Contains(secTag + " " + WHITE_TAG))
				{
					whiteLights.Add(light);
				}
				else if(lightName.Contains(secTag + " " + RED_TAG))
				{
					redLights.Add(light);
				}
			}
			
			//<OLD CODE> GridTerminalSystem.SearchBlocksOfName(secTag + " " + WHITE_TAG, whiteLights);  
			//<OLD CODE> GridTerminalSystem.SearchBlocksOfName(secTag + " " + RED_TAG, redLights);
 
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



/**  MONITOR LIGHTS (DEFAULT)
******************************************
*   A default method that does not require an argument.  This will check pressure in all vents on the station and change the lights accordingly.
*   White lights turn on when pressurized, and red lights when depressurized.
*/
public void MonitorLights(Vessel vessel) 
{ 
    //Cycle through vents.  If vent contains secTag continue. 
    for(int v = 0; v < vessel.vents.Count; v++) 
    { 
        IMyAirVent vent = vessel.vents[v] as IMyAirVent; 
        string[] ventNames = vent.CustomName.Split();
         
        if (ventNames.Length>1) 
        {
            string secTag = ventNames[1]; 
			UpdateLights(vessel, secTag);
        } 
    } 
}



/**  EMERGENCY LIGHTS
*******************************
* Turns white lights off and red lights on in specified sector.
*/
public void EmergencyLights(Vessel vessel, string secTag)
{
    //Go through lights and sort sector lights into red and white light lists.
    List<IMyTerminalBlock> whiteLights = new List<IMyTerminalBlock>();
	List<IMyTerminalBlock> redLights = new List<IMyTerminalBlock>();
	for(int l = 0; l < vessel.lights.Count; l++)
	{
		IMyInteriorLight light = vessel.lights[l];
		string lightName = light.CustomName;
		if(lightName.Contains(secTag + " " + WHITE_TAG))
		{
			whiteLights.Add(light);
		}
		else if(lightName.Contains(secTag + " " + RED_TAG))
		{
			redLights.Add(light);
		}
	}
	
    for(int w = 0; w < whiteLights.Count; w++) 
    { 
        IMyInteriorLight wLight = whiteLights[w] as IMyInteriorLight; 
		wLight.GetActionWithName("OnOff_Off").Apply(wLight); 
    } 
    for(int r = 0; r < redLights.Count; r++) 
    { 
        IMyInteriorLight rLight = redLights[r] as IMyInteriorLight;  		
		rLight.GetActionWithName("OnOff_On").Apply(rLight);   
    }
}



/**  DOOR OVERRIDE
***************************
*   Sets the vents in two sectors to Depressurize, and unlocks the door between them.
*/
public void DoorOverride(Vessel vessel, string sectors)
{
    //Get the sector tags from the argument.
    string[] sectorArr = sectors.Split('_');
    string secA = sectorArr[0];
    string secB = sectorArr[1];
    
    //Get the vents from both sectors and set them to "Depressurize".
    for(int v = 0; v < vessel.vents.Count; v++) 
    { 
        IMyAirVent vent = vessel.vents[v] as IMyAirVent; 
        string ventName = vent.CustomName; 
		if (ventName.Contains(secA) || ventName.Contains(secB))
		{
			Echo(ventName);
			vent.GetActionWithName("Depressurize_On").Apply(vent);
		}
    }

    //Finds the door between Sector A and Sector B, then powers it on.   
    for(int d = 0; d < vessel.doors.Count; d++)     
    {     
        IMyDoor door = vessel.doors[d] as IMyDoor;     
        string name = door.CustomName;
		if (name.Contains(secA) && name.Contains(secB))
		{
			Echo(name);
			door.GetActionWithName("OnOff_On").Apply(door);
		}
    }
}



/**  OVERRIDE RESET
****************************
*   Restores Depressurize to "Off" in two sectors.
*/
public void OverrideReset(Vessel vessel, string sectors) 
{ 
    string[] sectorArr = sectors.Split('_'); 
    string secA = sectorArr[0]; 
    string secB = sectorArr[1]; 
    for(int v = 0; v < vessel.vents.Count; v++)  
    {  
        IMyAirVent vent = vessel.vents[v] as IMyAirVent;  
        string ventName = vent.CustomName;  
        if (ventName.Contains(secA) || ventName.Contains(secB)) 
        { 
            Echo(ventName); 
            vent.GetActionWithName("Depressurize_Off").Apply(vent); 
        } 
    } 
}



/**  OPEN LOCK SEQUENCE
**********************************
*   Innitiates a lock opening by depressurizing the lock and starting the LOCK TIMER.
*   LOCK TIMER ACTIONS:
*   --Call this program with argument "OverrideDoor <sectorTag>_<vaccuumTag>".
*     See "Arguments", subsection "OverrideDoor" for proper syntax.
*   --Open outer lock door
*/
public void OpenLock(Vessel vessel, string secTag)
{
    DoorClose(vessel, secTag);
    EmergencyLights(vessel, secTag);

    for(int a = 0; a < vessel.alarms.Count; a++) 
    { 
        IMySoundBlock alarm = vessel.alarms[a] as IMySoundBlock; 
        string alarmName = alarm.CustomName; 
        if (alarmName.Contains(secTag))
        {
            Echo(alarmName);
            alarm.GetActionWithName("PlaySound").Apply(alarm);
        }
    }

    for(int v = 0; v < vessel.vents.Count; v++) 
    { 
        IMyAirVent vent = vessel.vents[v] as IMyAirVent; 
        string ventName = vent.CustomName; 
        if (ventName.Contains(secTag) && vent.GetOxygenLevel() > 0)
        {
            Echo(ventName);
            vent.GetActionWithName("Depressurize_On").Apply(vent);
        }
    }

    for(int t = 0; t < vessel.timers.Count; t++) 
    { 
        IMyTimerBlock timer = vessel.timers[t] as IMyTimerBlock; 
        string timerName = timer.CustomName; 
        if (timerName.Contains(secTag))
        {
            Echo(timerName);
            timer.GetActionWithName("Start").Apply(timer);
        }
    }
}


/** CLOSE LOCK
*********************   
* Sets "depressurization" option in airlock or hangar vent to off.
* Closes doors of hangar or lock.
*/
public void CloseLock(Vessel vessel, string secTag)
{ 
    for(int v = 0; v < vessel.vents.Count; v++) 
    { 
        IMyAirVent vent = vessel.vents[v] as IMyAirVent; 
        string ventName = vent.CustomName; 
        if (ventName.Contains(secTag))
        {
            Echo(ventName);
            vent.GetActionWithName("Depressurize_Off").Apply(vent);
        }
    }
    DoorClose(vessel, secTag);	
}


/** TIMER CALL
*********************
* Method triggered from Timer, which chooses actions based on
* timer's Custom Data.
*/
public void TimerCall(Vessel vessel, string secTag)
{
	for(int t = 0; t < vessel.timers.Count; t++)
	{
		IMyTimerBlock timer = vessel.timers[t] as IMyTimerBlock;
		string timerName = timer.CustomName;
		if(timerName.Contains(secTag))
		{
			string arg = timer.CustomData;
			Echo("TIMER CUSTOM DATA = " +arg);

			switch(arg)
			{	
				case "Check":
					LockCheck(vessel, secTag, timer);
					break;
				case "DockDetach":
					DockDetach(vessel, secTag, timer);
					break;
				case "DockReset":
					DockReset(vessel, secTag, timer);
					break;
				default:
					FlushLock(vessel, secTag, timer);
					break;		
			}
			
		}
	}
}

/** DOCK DETACH
* Turns off Merge Block and Unlocks Connector
* for Undocking
*/
public void DockDetach(Vessel vessel, string secTag, IMyTimerBlock timer)
{
	for(int m = 0; m < vessel.mergeBlocks.Count; m++)
	{
		IMyShipMergeBlock mergeBlock = vessel.mergeBlocks[m] as IMyShipMergeBlock;
		if(mergeBlock.CustomName.Contains(secTag))
		{
			for(int c = 0; c < vessel.connectors.Count; c++)
			{
				IMyShipConnector connector = vessel.connectors[c] as IMyShipConnector;
				if(connector.CustomName.Contains(secTag))
				{
					connector.Disconnect();
					mergeBlock.GetActionWithName("OnOff_Off").Apply(mergeBlock);
				}
			}
		}
	}
	
	timer.CustomData = "DockReset";
	timer.TriggerDelay = 10;
	timer.StartCountdown();	
}


/** DOCK RESET
*****************************
*  Reactivates Connector and Merge Block
*  for specified Dock
*/
public void DockReset(Vessel vessel, string secTag, IMyTimerBlock timer)
{
	for(int m = 0; m < vessel.mergeBlocks.Count; m++)
	{
		IMyShipMergeBlock mergeBlock = vessel.mergeBlocks[m] as IMyShipMergeBlock;
		if(mergeBlock.CustomName.Contains(secTag))
		{
			mergeBlock.GetActionWithName("OnOff_On").Apply(mergeBlock);
		}
	}
	
	for(int v = 0; v < vessel.vents.Count; v++)
	{
		IMyAirVent vent = vessel.vents[v] as IMyAirVent;
		delayReset(vent, timer);
	}
timer.CustomData = "";
}


/** LOCK CHECK
* Timer Method that checks Lock Pressure
* then resets Timer to Default Method
*/
public void LockCheck(Vessel vessel, string secTag, IMyTimerBlock timer)
{
	DoorCheck(vessel, secTag);
	for (int v = 0; v < vessel.vents.Count; v++)
	{
		IMyAirVent vent = vessel.vents[v];
		if(vent.CustomName.Contains(secTag))
		{
			delayReset(vent, timer);
			timer.CustomData = "";
		}
	}
}


/**  FLUSH LOCK
* Default Method for Timers
* Depressurizes Lock and opens outer door.
*/
public void FlushLock(Vessel vessel, string secTag, IMyTimerBlock timer)
{
	Echo("DEFAULT");
	string sectors = secTag + "_" + VACTAG;
	DoorOverride(vessel, sectors);
	for(int d = 0; d < vessel.doors.Count; d++)
	{
		IMyDoor door = vessel.doors[d] as IMyDoor;
		string doorName = door.CustomName;
		if(doorName.Contains(secTag) && doorName.Contains(VACTAG) && !doorName.Contains(EXIT_TAG))
		{
			door.GetActionWithName("OnOff_On").Apply(door);
			door.GetActionWithName("Open_On").Apply(door);
		}
	}
			
	timer.CustomData = "Check";
	timer.TriggerDelay = 1;
	timer.StartCountdown();	
}


/** DELAY RESET
**********************
* Checks Lock Vent 
*/
void delayReset(IMyAirVent vent, IMyTimerBlock timer)
{
	int delay;
	string[] args = vent.CustomData.Split(' ');
	if(args.Length > 1)
	{
		delay = int.TryParse(args[1], out delay)? delay: LOCK_TIME;
	}
	else
	{
		delay = LOCK_TIME;
	}
		timer.TriggerDelay = delay;
}


/**   CYCLE LOCK
********************
* Checks lock vent pressure and opens or closes lock accordingly.
* Intended for use with a single timer
*/
public void CycleLock(Vessel vessel, string secTag)
{    
    for(int v = 0; v < vessel.vents.Count; v++) 
    {
        IMyAirVent vent = vessel.vents[v] as IMyAirVent; 
        string ventName = vent.CustomName; 
        if (ventName.Contains(secTag) && vent.GetOxygenLevel() > 0)
        {
            OpenLock(vessel, secTag);
        }
        else
        {
            CloseLock(vessel, secTag);
        }
    }
}


/**  GET DOCKED
***********************
* Gets information about Docked Ships for specific sectors.
*
*/
public string GetDocked(Vessel vessel, string secTag)
{
	string dockedName = "";
	for(int m = 0; m < vessel.mergeBlocks.Count; m++)
	{
		IMyShipMergeBlock my_mergeBlock = vessel.mergeBlocks[m] as IMyShipMergeBlock;
		if(my_mergeBlock.CustomName.Contains(secTag))
		{
			List<IMyShipMergeBlock> all_mergeBlocks = new List<IMyShipMergeBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(all_mergeBlocks);
			for(int n=0; n < all_mergeBlocks.Count; n++)
			{
				IMyShipMergeBlock mergeBlock = all_mergeBlocks[n];
				string mergeName = mergeBlock.CustomName;
				{
					if(!mergeName.Contains(SHIPTAG))
					{
						var distance = Vector3D.Distance(mergeBlock.GetPosition(), my_mergeBlock.GetPosition());
						if(distance < MERGE_DIST)
						{
							dockedName = mergeName;
							return dockedName;
						}
					}

				}
			}
		}
	}
	
	return dockedName;
}


/** BUILD DOCK
* Checks if Sector has Merge Block. Writes Sector Data to Merge Block
*
*/
public void BuildDock(Vessel vessel, string secTag)
{
	int mergeCount = 0;
	for(int m = 0; m < vessel.mergeBlocks.Count; m++)
	{
		IMyShipMergeBlock mergeBlock = vessel.mergeBlocks[m];
		string mergeName = mergeBlock.CustomName;
		if(mergeName.Contains(secTag))
		{
			mergeCount++;
			string mergeInfo = secTag + " " + SHIPTAG;
			mergeBlock.CustomData = mergeInfo;
		}
	}
	if(mergeCount > 0)
	{
		Echo(GetDocked(vessel, secTag));
	}
}



/**  LCD PRINT
********************
*Gets LCD on ONE side of door, checks pressures, then updates display.
*/
public void LCDPrint(Vessel vessel, string sec1, string sec2)
{
   Color lcdColor = new Color(127,127,127);

    //Get LCD with tag: sec1 sec2
    string lcdName = "" + sec1 + " " + sec2;

    for (int i = 0; i < vessel.lcds.Count; i++)
    {
        IMyTextPanel lcd = vessel.lcds[i] as IMyTextPanel;
        string label = lcd.CustomName;
        if (label.Contains(lcdName))
        {
             //Get Vents with tags sec1 and sec2 and assign them as vent1 and vent2 ; 
            for (int j = 0; j < vessel.vents.Count; j++) 
            {
                IMyAirVent vent1 = vessel.vents[j] as IMyAirVent; 
                string ventName1 = vent1.CustomName; 
                if (ventName1.Contains(sec1)) 
                { 
                    for (int c = 0; c < vessel.vents.Count; c++)
                    {
                        IMyAirVent vent2 = vessel.vents[c] as IMyAirVent;
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

                               // Set Text Padding to 0
                               lcd.TextPadding=0.01f;
                               lcd.Font="Monospace";
                               lcd.FontColor = lcdColor;
                               lcd.FontSize = 0.398f;
                               lcd.ContentType = ContentType.TEXT_AND_IMAGE;


                                //This is the common LCD print out that changes depending on the defined pixel variables.
                                lcd.WriteText( 
 /*                                   // 01
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+
                                    w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+w+"\n"+ 
 */
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
