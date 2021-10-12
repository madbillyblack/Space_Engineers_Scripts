﻿//////////////////////////////////
// PRESSURE CHIEF			  	//
// Pressure management script	//
// Author: SJ_Omega				//
//////////////////////////////v1.0

// USER CONSTANTS -  Feel free to change as needed:
const string VAC_TAG = "XoX"; // Tag used to designate External reference vents (i.e. Vacuum vents).


// AVOID CHANGING ANYTHING BELOW THIS LINE!!!!!-----------------------------------------------------------------------------------------------------
const string OPENER = "[|";
const string CLOSER = "|]";
const char SPLITTER = '|';
const string INI_HEAD = "Pressure Chief";
const string NORMAL = "255,255,255";
const string EMERGENCY = "255,0,0";


// Globals //
static string _statusMessage;
int _currentSector;

static List<IMyAirVent> _vents;
static List<IMyDoor> _doors;
static List<IMyTextPanel> _lcds;
static List<IMySoundBlock> _lockAlarms;
static List<IMyTimerBlock> _lockTimers;
static List<IMyShipConnector> _connectors;
static List<IMyShipMergeBlock> _mergeBlocks;
static List<IMyLightingBlock> _lights;
static List<Sector> _sectors;
static List<Bulkhead> _bulkheads;



// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// SECTOR //
public class Sector{
	
	public string Tag;
	public IMyAirVent Vent;
	public List<IMyDoor> Doors;
	public List<IMyLightingBlock> Lights;
	public List<IMyTextPanel> LCDs;
	public List<IMyShipMergeBlock> MergeBlocks;
	public List<IMyShipConnector> Connectors;
	public List<Bulkhead> Bulkheads;
	public string Type; // Room, Lock, Dock, or Vacuum
	public string NormalColor;
	public string EmergencyColor;
	public IMyTimerBlock LockTimer;
	public IMySoundBlock LockAlarm;
	
	public Sector(IMyAirVent airVent){
		this.Vent = airVent;
		this.Tag = TagFromName(airVent.CustomName);
		this.NormalColor = GetKey(airVent, "Normal_Color", "");
		this.EmergencyColor = GetKey(airVent, "Emergency_Color", "");

		if(this.Tag == VAC_TAG)
			this.Type = "Vacuum";
		else
			this.Type = "Room";
		
		this.Doors = new List<IMyDoor>();
		this.Lights = new List<IMyLightingBlock>();
		this.LCDs = new List<IMyTextPanel>();
		this.MergeBlocks = new List<IMyShipMergeBlock>();
		this.Connectors = new List<IMyShipConnector>();
		this.Bulkheads = new List<Bulkhead>();
	}
	
	public void Monitor(){
		foreach(Bulkhead myBulkhead in this.Bulkheads)
			myBulkhead.Monitor();
			
		bool depressurized = this.Vent.GetOxygenLevel() < 0.7 || this.Vent.Depressurize;
			
		if(this.Lights.Count > 0){
			foreach(IMyLightingBlock myLight in this.Lights){
				if(depressurized)
					myLight.Color = ColorFromString(GetKey(myLight, "Emergency_Color", EMERGENCY));
				else
					myLight.Color = ColorFromString(GetKey(myLight, "Normal_Color", NORMAL));
			}
		}
	}
}


// BULKHEAD //   Wrapper class for doors so that they can directly access their sectors.
public class Bulkhead{
	public IMyDoor Door;
	public string TagA;
	public string TagB;
	public Sector SectorA;
	public Sector SectorB;
	public IMyAirVent VentA;
	public IMyAirVent VentB;
	public IMyTextPanel LCDa;
	public IMyTextPanel LCDb;
	public bool Override;
	
	public Bulkhead(IMyDoor myDoor){
		this.Door = myDoor;
		string[] tags = MultiTags(myDoor.CustomName);
		
		this.TagA = tags[0];
		this.TagB = tags[1];
		this.Override = false;
	}

	public void Monitor(){
		if(this.SectorA == null || this.SectorB == null)
			return;
		
		float pressureA = this.VentA.GetOxygenLevel();
		float pressureB = this.VentB.GetOxygenLevel();
		
		if(this.Override || pressureA == pressureB)
			this.Door.GetActionWithName("OnOff_On").Apply(this.Door);
		else
			this.Door.GetActionWithName("OnOff_Off").Apply(this.Door);
	}
	
	public void SetOverride(bool overrided){
		this.Override = overrided;
		SetIni(this.Door, "Override", overrided.ToString());
		_statusMessage = this.Door.CustomName + " Override status set to " + overrided.ToString();
	}
}


// PROGRAM /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public Program(){
	_statusMessage = "";
	_currentSector = 0;
	Build();
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
}


// SAVE ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Save(){}


// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Main(string arg){
	if(arg != ""){
		string[] args = arg.Split(' ');
		
		string command = args[0].ToUpper();
		
		string cmdArg = "";
		
		if(args.Length > 1){
			for(int i = 1; i < args.Length; i++)
				cmdArg += args[i];
		}
		
		switch(command){
			case "OVERRIDE":
				Bulkhead overrideDoor = GetBulkhead(cmdArg);
				overrideDoor.SetOverride(true);
				break;
			case "RESTORE":
				Bulkhead restoreDoor = GetBulkhead(cmdArg);
				restoreDoor.SetOverride(false);
				break;
			case "REFRESH":
				Build();
				break;
			default:
				_statusMessage = "UNRECOGNIZED COMMAND: " + arg;
				break;
		}
	}
	
	
	Echo(_statusMessage);
	
	_currentSector++;
	if(_currentSector >= _sectors.Count)
		_currentSector = 0;


	Sector sector = _sectors[_currentSector];
	Echo(sector.Type + " " + sector.Tag);
	sector.Monitor();

/*
	foreach(Bulkhead bulkhead in sector.Bulkheads){
		Echo("\n" + bulkhead.Door.CustomName);
		//try{
		bulkhead.Monitor();
		//}catch{
		//	_statusMessage = "MONITOR ERROR at " + bulkhead.Door.CustomName;
		//}

			
		if(bulkhead.LCDa != null)
			Echo("* " + bulkhead.LCDa.CustomName);
		if(bulkhead.LCDb != null)
			Echo("* " + bulkhead.LCDb.CustomName);
	}
*/	
}


// TOOL FUNCTIONS //--------------------------------------------------------------------------------------------------------------------------------

// TAG FROM NAME //
public static string TagFromName(string name){
	
	int start = name.IndexOf(OPENER) + OPENER.Length; //Start index of tag substring
	int length = name.IndexOf(CLOSER) - start; //Length of tag
	
	return name.Substring(start, length);
}


// MULTITAGS // Returns 2 entry array of tags for blocks with multiple tags.
public static string[] MultiTags(string name){
	string bigTag = TagFromName(name);
	string[] output = bigTag.Split(SPLITTER);
	
	if(output.Length == 2)
		return output;
	
	_statusMessage = "Split Error for " + name;
	return new string[]{"ERROR", "ERROR"};
}


// PARSE BOOL //
bool ParseBool(string val)
{
	string uVal = val.ToUpper();
	if(uVal == "TRUE" || uVal == "T" || uVal == "1")
	{
		return true;
	}
	
	return false;
}


// GET BULKHEAD //
Bulkhead GetBulkhead(string tag){
	foreach(Bulkhead bulkhead in _bulkheads){
		if(tag.Contains(bulkhead.TagA) && tag.Contains(bulkhead.TagB))
			return bulkhead;
	}
	
	_statusMessage = "No Door with tag " + tag + " found!";
	return null;
}


// COLOR FROM STRING //
static Color ColorFromString(string rgb){
	string[] values = rgb.Split(',');
	if(values.Length < 3)
		return Color.Purple;
	
	byte[] outputs = new byte[3];
	for(int i=0; i<3; i++){
		bool success = byte.TryParse(values[i], out outputs[i]);
		if(!success)
			outputs[i] = 0;
	}
	
	return new Color(outputs[0],outputs[1],outputs[2]);
}



// INIT FUNCIONS -----------------------------------------------------------------------------------------------------------------------------------

// BUILD // Searches grid for all components and adds them to current run.
void Build(){
	_vents = new List<IMyAirVent>();
	_doors = new List<IMyDoor>();
	_lcds = new List<IMyTextPanel>();
	_lockAlarms = new List<IMySoundBlock>();
	_lockTimers = new List<IMyTimerBlock>();
	_connectors = new List<IMyShipConnector>();
	_mergeBlocks = new List<IMyShipMergeBlock>();
	_lights = new List<IMyLightingBlock>();
	_sectors = new List<Sector>();
	_bulkheads = new List<Bulkhead>();
	
	
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(OPENER, blocks);
	
	if(blocks.Count > 0){
		foreach(IMyTerminalBlock block in blocks){
			switch(block.BlockDefinition.TypeIdString){
				case "MyObjectBuilder_AirVent":
					_vents.Add(block as IMyAirVent);
					break;
				case "MyObjectBuilder_Door":
					_doors.Add(block as IMyDoor);
					break;
				case "MyObjectBuilder_TextPanel":
					_lcds.Add(block as IMyTextPanel);
					break;
				case "MyObjectBuilder_SoundBlock":
					_lockAlarms.Add(block as IMySoundBlock);
					break;
				case "MyObjectBuilder_TimerBlock":
					_lockTimers.Add(block as IMyTimerBlock);
					break;
				case "MyObjectBuilder_ShipConnector":
					_connectors.Add(block as IMyShipConnector);
					break;
				case "MyObjectBuilder_MergeBlock":
					_mergeBlocks.Add(block as IMyShipMergeBlock);
					break;
				case "MyObjectBuilder_InteriorLight":
				case "MyObjectBuilder_ReflectorLight":
					_lights.Add(block as IMyLightingBlock);
					break;
			}
		}
		
		foreach(IMyAirVent vent in _vents){
			Sector sector = new Sector(vent);
			_sectors.Add(sector);
		}
		
		if(_sectors.Count < 1 || _doors.Count < 1)
			return;
		
		// Assign double-tagged components
		AssignDoors();
		AssignLCDs();
		
		// Assign single-tagged components
		if(_lights.Count > 0){
			foreach(IMyLightingBlock light in _lights)
				AssignLight(light);
		}
		
		if(_lockTimers.Count > 0){
			foreach(IMyTimerBlock timer in _lockTimers)
				AssignTimer(timer);
		}
		
		if(_lockAlarms.Count > 0){
			foreach(IMySoundBlock alarm in _lockAlarms)
				AssignAlarm(alarm);
		}
		
		if(_mergeBlocks.Count > 0){
			foreach(IMyShipMergeBlock mergeBlock in _mergeBlocks)
				AssignMergeBlock(mergeBlock);
		}
		
		if(_connectors.Count > 0){
			foreach(IMyShipConnector connector in _connectors)
				AssignConnector(connector);
		}
	}
}


// ASSIGN DOORS //
void AssignDoors(){
	foreach(IMyDoor door in _doors){
		string[] tags = MultiTags(door.CustomName);
		Bulkhead bulkhead = new Bulkhead(door);
		bulkhead.Override = ParseBool(GetKey(door, "Override", "false"));
		
		foreach(Sector sector in _sectors){
			if(sector.Tag == tags[0]){
				sector.Doors.Add(door);
				sector.Bulkheads.Add(bulkhead);
				bulkhead.SectorA = sector;
				bulkhead.VentA = sector.Vent;
			}else if(sector.Tag == tags[1]){
				sector.Doors.Add(door);
				sector.Bulkheads.Add(bulkhead);
				bulkhead.SectorB = sector;
				bulkhead.VentB = sector.Vent;
			}
		}
		
		if(bulkhead.SectorA == null)
			_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[0];
		else if(bulkhead.SectorB == null)
			_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[1];
		else
			_bulkheads.Add(bulkhead);
	}
}


// ASSIGN LCD //
void AssignLCDs(){
	if(_lcds.Count < 1)
		return;
	
	foreach(IMyTextPanel lcd in _lcds){
		string[] tags = MultiTags(lcd.CustomName);
		string tag = tags[0] + SPLITTER + tags[1];
		string reverseTag = tags[1] + SPLITTER + tags[0];
	
		foreach(Bulkhead bulkhead in _bulkheads){
			string doorName = bulkhead.Door.CustomName;
			if(doorName.Contains(tag))
				bulkhead.LCDa = lcd;
			else if(doorName.Contains(reverseTag))
				bulkhead.LCDb = lcd;
		}
	}
}


// ASSIGN LIGHT //
void AssignLight(IMyLightingBlock light){
	string tag = TagFromName(light.CustomName);
	
	EnsureKey(light, "Normal_Color", "255,255,255");
	EnsureKey(light, "Emergency_Color", "255,0,0");
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.Lights.Add(light);
			
			if(sector.NormalColor != "")
				SetIni(light, "Normal_Color", sector.NormalColor);
			if(sector.EmergencyColor != "")
				SetIni(light, "Emergency_Color", sector.EmergencyColor);
			
			
			return;
		}
	}
}


// ASSIGN TIMER //
void AssignTimer(IMyTimerBlock timer){
	string tag = TagFromName(timer.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.LockTimer = timer;
			sector.Type = "Lock"; //Set Sector Type to Lock if a Timer is present
			return;
		}
	}
}


// ASSIGN ALARM //
void AssignAlarm(IMySoundBlock alarm){
	string tag = TagFromName(alarm.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.LockAlarm = alarm;
			return;
		}
	}
}


// ASSIGN MERGE BLOCK //
void AssignMergeBlock(IMyShipMergeBlock mergeBlock){
	string tag = TagFromName(mergeBlock.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.MergeBlocks.Add(mergeBlock);
			sector.Type = "Dock"; //Set Sector Type to Dock if Merge Blocks are present.
			return;
		}
	}
}


// ASSIGN CONNECTOR //
void AssignConnector(IMyShipConnector connector){
	string tag = TagFromName(connector.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.Connectors.Add(connector);
			return;
		}
	}
}






// INI FUNCTIONS -----------------------------------------------------------------------------------------------------------------------------------



// ENSURE KEY //
static void EnsureKey(IMyTerminalBlock block, string key, string defaultVal){
	if(!block.CustomData.Contains(INI_HEAD)||!block.CustomData.Contains(key))
		SetIni(block, key, defaultVal);
}


// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
static string GetKey(IMyTerminalBlock block, string key, string defaultVal){
	EnsureKey(block, key, defaultVal);
	MyIni blockIni = GetIni(block);
	return blockIni.Get(INI_HEAD, key).ToString();
}


// SET INI // Update ini key for block, and write back to custom data.
static void SetIni(IMyTerminalBlock block, string key, string arg){
	MyIni blockIni = GetIni(block);
	blockIni.Set(INI_HEAD, key, arg);
	block.CustomData = blockIni.ToString();
}


// GET INI //
static MyIni GetIni(IMyTerminalBlock block){
	MyIni iniOuti = new MyIni();
	
	MyIniParseResult result;
	if (!iniOuti.TryParse(block.CustomData, out result)) 
		throw new Exception(result.ToString());
	
	return iniOuti;
}