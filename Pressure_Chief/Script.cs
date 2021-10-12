//////////////////////////////////
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
static string _previosCommand;
int _currentSector;
static bool _autoCheck;
static bool _autoClose;


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
	public string Status;
	public IMyTimerBlock LockTimer;
	public IMySoundBlock LockAlarm;
	
	public Sector(IMyAirVent airVent){
		this.Vent = airVent;
		this.Tag = TagFromName(airVent.CustomName);
		this.NormalColor = GetKey(airVent, "Normal_Color", NORMAL);
		this.EmergencyColor = GetKey(airVent, "Emergency_Color", EMERGENCY);
		this.Status = GetKey(airVent, "Status", airVent.Status.ToString());

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
					myLight.Color = ColorFromString(GetKey(myLight, "Emergency_Color", this.EmergencyColor));
				else
					myLight.Color = ColorFromString(GetKey(myLight, "Normal_Color", this.NormalColor));
			}
		}
		
		if(_autoClose)
			this.UpdateStatus();
	}
	
	public void CloseDoors(){
		if(this.Doors.Count < 1)
			return;
		
		foreach(IMyDoor myDoor in this.Doors){
			myDoor.CloseDoor();
		}
	}
	
	public void UpdateStatus(){
		IMyAirVent airVent = this.Vent;
		string oldStatus = GetKey(airVent, "Status", "Depressurized");
		
		if(this.Type=="Room"){

			if(oldStatus=="Pressurized" && airVent.Status.ToString() != "Pressurized")
				this.CloseDoors();
		}else if((this.Type=="Lock" || this.Type=="Dock") && this.LockAlarm != null){
			if(oldStatus != "Pressurized" && airVent.Status.ToString() == "Pressurized"){
				this.LockAlarm.SelectedSound = "SoundBlockAlert2";
				this.LockAlarm.LoopPeriod = 1.1f;
				this.LockAlarm.Play();
			}
		}
		
		SetKey(airVent, "Status", airVent.Status.ToString());
	}
}


// BULKHEAD //   Wrapper class for doors so that they can directly access their sectors.
public class Bulkhead{
	public List<IMyDoor> Doors;
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
		this.Doors = new List<IMyDoor>();
		this.Doors.Add(myDoor);
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
		this.Override = ParseBool(GetKey(this.Doors[0], "Override", "false"));
		
		if(this.Override || Math.Abs(pressureA - pressureB) < 0.1){
			foreach(IMyDoor door in this.Doors)
				door.GetActionWithName("OnOff_On").Apply(door);
		}else{
			foreach(IMyDoor door in this.Doors)
				door.GetActionWithName("OnOff_Off").Apply(door);
		}
	}
	
	public void SetOverride(bool overrided){
		this.Override = overrided;
		SetKey(this.Doors[0], "Override", overrided.ToString());
		_statusMessage = this.Doors[0].CustomName + " Override status set to " + overrided.ToString();
	}
	
	public void Open(){
		foreach(IMyDoor myDoor in this.Doors)
			myDoor.OpenDoor();
	}
	
}


// PROGRAM /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public Program(){
	_previosCommand = "NEWLY LOADED";
	_statusMessage = "";
	_currentSector = 0;

	Build();
	
	string updateFactor = GetKey(Me, "Refresh_Rate", "10");
	if(updateFactor == "1")
		Runtime.UpdateFrequency = UpdateFrequency.Update1;
	else if(updateFactor == "100")
		Runtime.UpdateFrequency = UpdateFrequency.Update100;
	else
		Runtime.UpdateFrequency = UpdateFrequency.Update10;
}


// SAVE ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Save(){}


// MAIN /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Main(string arg){
	if(_vents.Count < 1){
		Echo("No Vents Found to Build Network!  Please add sector tags to vent names then recompile!");
		return;
	}
	Echo(_previosCommand);
	Echo(_statusMessage);
	
	if(arg != ""){
		_previosCommand = arg;
		
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
		return;
	}
	
	

	
	_currentSector++;
	if(_currentSector >= _sectors.Count)
		_currentSector = 0;


	Sector sector = _sectors[_currentSector];
	Echo(sector.Type + " " + sector.Tag);
	sector.Monitor();
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
static bool ParseBool(string val)
{
	string uVal = val.ToUpper();
	if(uVal == "TRUE" || uVal == "T" || uVal == "1")
	{
		return true;
	}
	
	return false;
}


// GET SECTOR //
Sector GetSector(string tag){
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag)
			return sector;
	}
	
	return null;
}


// GET BULKHEAD //
Bulkhead GetBulkhead(string tag){
	if(_bulkheads.Count < 1)
		return null;
	
	foreach(Bulkhead bulkhead in _bulkheads){
		if(tag.Contains(bulkhead.TagA) && tag.Contains(bulkhead.TagB))
			return bulkhead;
	}
	
	return null;
}


// COLOR FROM STRING //
static Color ColorFromString(string rgb){
	string[] values = rgb.Split(',');
	if(values.Length < 3)
		return Color.Black;
	
	byte[] outputs = new byte[3];
	for(int i=0; i<3; i++){
		bool success = byte.TryParse(values[i], out outputs[i]);
		if(!success)
			outputs[i] = 0;
	}
	
	return new Color(outputs[0],outputs[1],outputs[2]);
}


// TIMER CALL //
void TimerCall(string tag){
	Sector sector = GetSector(tag);
	
	if(sector == null || (sector.Type != "Lock" && sector.Type != "Dock")){
		_statusMessage = "INVALID TIMER CALL!";
		return;
	}
	
	foreach(Bulkhead bulkhead in sector.Bulkheads){
		if(bulkhead.TagA == VAC_TAG || bulkhead.TagB == VAC_TAG){
			bulkhead.SetOverride(true);
			bulkhead.Open();
		}
	}
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
	
	_autoCheck = ParseBool(GetKey(Me, "Auto-Check", "true"));
	_autoClose = ParseBool(GetKey(Me, "Auto-Close", "true"));
	
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(OPENER, blocks);
	
	if(blocks.Count > 0){
		foreach(IMyTerminalBlock block in blocks){
			switch(block.BlockDefinition.TypeIdString){
				case "MyObjectBuilder_AirVent":
					_vents.Add(block as IMyAirVent);
					break;
				case "MyObjectBuilder_Door":
				case "MyObjectBuilder_AirtightSlideDoor":
				case "MyObjectBuilder_AirtightHangarDoor":
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
		Bulkhead bulkhead = GetBulkhead(tags[0]+SPLITTER+tags[1]);
		if(bulkhead == null){
			Echo("A");
			bulkhead = new Bulkhead(door);
			_bulkheads.Add(bulkhead);
		}else{
			bulkhead.Doors.Add(door);
		}
		
		Echo("B");
		bulkhead.Override = ParseBool(GetKey(door, "Override", "false"));
		SetKey(door, "Vent_A", "");
		SetKey(door, "Vent_B", "");
		
		foreach(Sector sector in _sectors){
			if(sector.Tag == tags[0]){
				sector.Doors.Add(door);
				sector.Bulkheads.Add(bulkhead);
				bulkhead.SectorA = sector;
				bulkhead.VentA = sector.Vent;
				SetKey(door, "Vent_A", sector.Vent.CustomName);
			}else if(sector.Tag == tags[1]){
				sector.Doors.Add(door);
				sector.Bulkheads.Add(bulkhead);
				bulkhead.SectorB = sector;
				bulkhead.VentB = sector.Vent;
				SetKey(door, "Vent_B", sector.Vent.CustomName);
			}
		}
		
		if(bulkhead.SectorA == null)
			_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[0];
		else if(bulkhead.SectorB == null)
			_statusMessage += "\nDOOR ERROR: " + door.CustomName + "\nNo Sector Found with tag " + tags[1];
		//else
		//	_bulkheads.Add(bulkhead);
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
			string doorName = bulkhead.Doors[0].CustomName;
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
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			sector.Lights.Add(light);
			
			if(GetKey(light, "Normal_Color", sector.NormalColor) == "");
				SetKey(light, "Normal_Color", sector.NormalColor);
			if(GetKey(light, "Emergency_Color", sector.EmergencyColor) == "");
				SetKey(light, "Emergency_Color", sector.EmergencyColor);
			

			return;
		}
	}
}


// ASSIGN TIMER //
void AssignTimer(IMyTimerBlock timer){
	string tag = TagFromName(timer.CustomName);
	
	foreach(Sector sector in _sectors){
		if(sector.Tag == tag){
			string delayString = GetKey(timer, "delay", "5");
			UInt16 delay;
			
			if(UInt16.TryParse(delayString, out delay))
				timer.TriggerDelay = delay;
			else
				timer.TriggerDelay = 5;
			
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
		SetKey(block, key, defaultVal);
}


// GET KEY // Gets ini value from block.  Returns default argument if doesn't exist.
static string GetKey(IMyTerminalBlock block, string key, string defaultVal){
	EnsureKey(block, key, defaultVal);
	MyIni blockIni = GetIni(block);
	return blockIni.Get(INI_HEAD, key).ToString();
}


// SET KEY // Update ini key for block, and write back to custom data.
static void SetKey(IMyTerminalBlock block, string key, string arg){
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