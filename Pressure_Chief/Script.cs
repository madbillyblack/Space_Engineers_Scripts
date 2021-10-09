//////////////////////////////////
// PRESSURE CHIEF			  	//
// Pressure management script	//
// Author: SJ_Omega				//
//////////////////////////////v1.0

// USER CONSTANTS -  Feel free to change as needed:



// AVOID CHANGING ANYTHING BELOW THIS LINE!!!!!------------------------------------------------------------------------------------------------
const string OPENER = "[|";
const string CLOSER = "|]";
const string SPLITTER = "|";

// Globals //
string _statusMessage;

List<IMyAirVent> _vents;
List<IMyDoor> _doors;
List<IMyTextPanel> _lcds;
List<IMySoundBlock> _lockAlarms;
List<IMyTimerBlock> _lockTimers;
List<IMyShipConnector> _connectors;
List<IMyShipMergeBlock> _mergeBlocks;
List<IMyLightingBlock> _lights;
List<Sector> _sectors;



public class Sector{
	
	public string tag;
	public IMyAirVent vent;
	public List<IMyDoor> doors;
	public List<IMyLightingBlock> lights;
	public string type; // Room, Lock, or Dock
	public IMyTimerBlock lockTimer;
	public IMySoundBlock lockAlarm;
	public IMyShipConnector connector;
	
	public Sector(IMyAirVent airVent){
		this.vent = airVent;
		this.tag = TagsFromName(airVent.CustomName);
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

public void Main(string argument, UpdateType updateSource)
{
	if(_sectors.Count < 1){
		Echo("NOTHING TO SEE");
		return;
	}
		
	foreach(Sector sector in _sectors){
		Echo(sector.tag);
	}
}


// TAGS FROM NAME //
public static string TagsFromName(string name){
	
	int start = name.IndexOf(OPENER) + OPENER.Length; //Start index of tag substring
	int length = name.IndexOf(CLOSER) - start; //Length of tag
	
	return name.Substring(start, length);
}


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
			_sectors.Add(sector);
		}
	}
	

}