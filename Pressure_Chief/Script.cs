//////////////////////////////////
// PRESSURE CHIEF			  	//
// Pressure management script	//
// Author: SJ_Omega				//
//////////////////////////////v1.0

// USER CONSTANTS -  Feel free to change as needed:



// AVOID CHANGING ANYTHING BELOW THIS LINE!!!!!-----------------------------------------------------------------------------------------------------
const string OPENER = "[|";
const string CLOSER = "|]";
const char SPLITTER = '|';
const string INI_HEAD = "Pressure Chief";

// Globals //
static string _statusMessage;

List<IMyAirVent> _vents;
List<IMyDoor> _doors;
List<IMyTextPanel> _lcds;
List<IMySoundBlock> _lockAlarms;
List<IMyTimerBlock> _lockTimers;
List<IMyShipConnector> _connectors;
List<IMyShipMergeBlock> _mergeBlocks;
List<IMyLightingBlock> _lights;
List<Sector> _sectors;



// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class Sector{
	
	public string tag;
	public IMyAirVent vent;
	public List<IMyDoor> doors;
	public List<IMyLightingBlock> lights;
	public string type; // User Defined Defaults to "Room"
	public IMyTimerBlock lockTimer;
	public IMySoundBlock lockAlarm;
	public IMyShipConnector connector;
	
	public Sector(IMyAirVent airVent){
		this.vent = airVent;
		this.tag = TagsFromName(airVent.CustomName);
		this.doors = new List<IMyDoor>();
		this.lights = new List<IMyLightingBlock>();
	}
}


public Program()
{
	_statusMessage = "";
	Build();
	Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save()
{
}


// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Main(string argument, UpdateType updateSource)
{
	if(_sectors.Count < 1){
		Echo("NOTHING TO SEE");
		return;
	}
		
	foreach(Sector sector in _sectors){
		Echo(sector.type + " " + sector.tag);
		if(sector.doors.Count > 0){
			foreach(IMyDoor door in sector.doors)
				Echo("* " + door.CustomName);
		}
		Echo(" ");
	}
}


// TAGS FROM NAME //
public static string TagsFromName(string name){
	
	int start = name.IndexOf(OPENER) + OPENER.Length; //Start index of tag substring
	int length = name.IndexOf(CLOSER) - start; //Length of tag
	
	return name.Substring(start, length);
}


// MULTITAGS // Returns 2 entry array of tags for blocks with multiple tags.
public static string[] MultiTags(string name){
	string bigTag = TagsFromName(name);
	string[] output = bigTag.Split(SPLITTER);
	
	if(output.Length == 2)
		return output;
	
	_statusMessage = "Split Error for " + name;
	return new string[]{"ERROR", "ERROR"};
}



// BUILD FUNCIONS -----------------------------------------------------------------------------------------------------------------------------------

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
			sector.type = GetKey(vent, "Sector_Type", "Room");
			_sectors.Add(sector);
		}
		
		if(_sectors.Count < 1 || _doors.Count < 1)
			return;
		
		foreach(IMyDoor door in _doors)
			AssignDoor(door);		
	}
}


// ASSIGN DOOR //
void AssignDoor(IMyDoor door){
	string[] tags = MultiTags(door.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.tag == tags[0] || sector.tag == tags[1])
			sector.doors.Add(door);
	}
}


// ASSIGN LCD //
void AssignLCD(IMyTextPanel lcd){}


// ASSIGN ALARM //
void AssignAlarm(IMySoundBlock alarm){}


// ASSIGN TIMER //
void AssignTimer(IMyTimerBlock timer){}


// ASSIGN CONNECTOR //
void AssignConnector(IMyShipConnector connector){}


// ASSIGN MERGE BLOCK //
void AssignMergeBlock(IMyShipMergeBlock mergeBlock){}


// ASSIGN LIGHT //
void AssignLight(IMyLightingBlock light){}



// INI FUNCTIONS -----------------------------------------------------------------------------------------------------------------------------------

// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
string GetKey(IMyTerminalBlock block, string key, string defaultVal){
	if(!block.CustomData.Contains(INI_HEAD)||!block.CustomData.Contains(key))
		SetIni(block, key, defaultVal);
	
	MyIni blockIni = GetIni(block);
	
	return blockIni.Get(INI_HEAD, key).ToString();
}


// SET INI // Update ini key for block, and write back to custom data.
void SetIni(IMyTerminalBlock block, string key, string arg){
	MyIni blockIni = GetIni(block);
	blockIni.Set(INI_HEAD, key, arg);
	block.CustomData = blockIni.ToString();
}


// GET INI //
MyIni GetIni(IMyTerminalBlock block){
	MyIni iniOuti = new MyIni();
	
	MyIniParseResult result;
	if (!iniOuti.TryParse(block.CustomData, out result)) 
		throw new Exception(result.ToString());
	
	return iniOuti;
}