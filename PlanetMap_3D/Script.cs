//////////////////////
// PLANET MAP 3D //
///////////////////// 1.3.0

// USER CONSTANTS //  Feel free to alter these as needed.

// Ship
const int SHIP_RED = 127;   //Red RGB value for ship pointer
const int SHIP_GREEN = 127; //Green RGB value for ship pointer
const int SHIP_BLUE = 192;  //Blue RGB value for ship pointer
const int SHIP_SCALE = 24;

// Planets
const float DIAMETER_MIN = 6; //Minimum Diameter For Distant Planets
const int HASH_LIMIT = 125; //Minimum diameter size to print Hashmarks on planet.

// Waypoints
const float JUMP_RATIO = 2; // Ratio of distance from center to radius for viable jump points
const int MARKER_WIDTH = 8; // Width of GPS Markers
const int FOCAL_MOD = 250; // Mod for waypoint scale

// View Controls
const int ANGLE_STEP = 5; // Basic angle in degrees of step rotations.
const int MAX_PITCH = 90; // Maximum (+/-) value of map pitch. [Not recommended above 90]
const int MOVE_STEP = 5000; // Step size for translation (move) commands.
const float ZOOM_STEP = 1.5f; // Factor By which map is zoomed in and out (multiplied).
const int ZOOM_MAX = 1000000000; // Max value for Focal Length

// View Defaults
const int DV_RADIUS = 262144; //Default View Radius
const int DV_FOCAL = 256; //Default Focal Length
const int DV_ALTITUDE = -15; //Default Altitude (angle)
const int BRIGHTNESS_LIMIT = 4;


// THERE IS NO REASON TO ALTER ANYTHING BELOW THIS LINE! //////////////////////////////////////////////////////////////////////////////////////////////////////////


// OTHER CONSTANTS //
const string SYNC_TAG = "[SYNC]"; //Tag used to indicate master sync computer.
const int BAR_HEIGHT = 20; //Default height of parameter bars
const int TOP_MARGIN = 8; // Margin for top and bottom of frame
const int SIDE_MARGIN = 15; // Margin for sides of frame
const int MAX_VALUE = 1073741824; //General purpose MAX value = 2^30
const int DATA_PAGES = 5;  // Number of Data Display Pages
const string SLASHES = " //////////////////////////////////////////////////////////////";
const string GPS_INPUT = "// GPS INPUT ";
const string DEFAULT_SETTINGS = "[Map Settings]\nMAP_Tag=[MAP]\nMAP_Index=0\nData_Tag=[Map Data]\nGrid_ID=\nData_Index=0\nReference_Name=[Reference]\nSlow_Mode=false\nCycle_Step=5\nPlanet_List=\nWaypoint_List=\n";
string _defaultDisplay = "[mapDisplay]\nGrid_ID=\nCenter=(0,0,0)\nMode=FREE\nFocalLength="
							+DV_FOCAL+"\nRotationalRadius="+DV_RADIUS +"\nAzimuth=0\nAltitude="
							+DV_ALTITUDE+"\nIndexes=\ndX=0\ndY=0\ndZ=0\ndAz=0\nGPS=True\nNames=True\nShip=True\nInfo=True\nPlanet=";
							
string[] _cycleSpinner = {"--"," / ", " | ", " \\ "};
						


// GLOBALS //
MyIni _mapLog = new MyIni();
string _mapTag;
string _refName;
string _dataName;
string _previousCommand;
int _mapIndex;
int _dataIndex;
int _pageIndex;
int _scrollIndex = 0;
int _azSpeed;
int _planetIndex;
bool _gpsActive;
bool _showMapParameters;
bool _showShip;
bool _showNames;
bool _lightOn;
bool _planets;
bool _planetToLog;
bool _slowMode = false;
int _cycleLength;
int _cycleStep;
int _sortCounter = 0;
float _brightnessMod;
static string _statusMessage;
string _activePlanet ="";
string _activeWaypoint = "";
string _clipboard = "";
string _gridTag;
Vector3 _trackSpeed;
Vector3 _origin = new Vector3(0,0,0);
Vector3 _myPos;
List<IMyTerminalBlock> _mapBlocks;
List<IMyTerminalBlock> _dataBlocks = new List<IMyTerminalBlock>();
List<Planet> _planetList;
List<Planet> _unchartedList;
List<Waypoint> _waypointList;
List<StarMap> _mapList;
Planet _nearestPlanet;
MySpriteDrawFrame _frame;

IMyTextSurface _dataSurface;
IMyTerminalBlock _refBlock;


// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////

// STAR MAP //
public class StarMap
{
	public Vector3 center;
	public string mode;
	public int altitude;
	public int azimuth;
	public int rotationalRadius;
	public int focalLength;
	public int azSpeed; // Rotational velocity of Azimuth
	public int number;
	public int index;
	public int dX;
	public int dY;
	public int dZ;
	public int dAz;
	public int gpsState;
	public IMyTextSurface drawingSurface;
	public RectangleF viewport;
	public IMyTerminalBlock block;
	public bool showNames;
	public bool showShip;
	public bool showInfo;
	public int planetIndex;
	public int waypointIndex;
	public string activePlanetName;
	public string activeWaypointName;
	public string gpsMode;
	public Planet activePlanet;
	public Waypoint activeWaypoint;

	public StarMap()
	{
		this.azSpeed = 0;
		this.planetIndex = 0;
		this.waypointIndex = -1;
	}

	public void yaw(int angle)
	{
		if(this.mode.ToUpper() == "PLANET" || this.mode.ToUpper() == "CHASE" || this.mode.ToUpper() == "ORBIT")
		{
			_statusMessage = "Yaw controls locked in PLANET, CHASE & ORBIT modes.";
			return;
		}
		this.azimuth = DegreeAdd(this.azimuth, angle); 
	}

	public void pitch(int angle)
	{
		if(this.mode.ToUpper() != "PLANET" || this.mode.ToUpper() == "ORBIT")
		{
			int newAngle = DegreeAdd(this.altitude, angle);

			if(newAngle > MAX_PITCH)
			{
				newAngle = MAX_PITCH;
			}
			else if(newAngle < -MAX_PITCH)
			{
				newAngle = -MAX_PITCH;
			}

			this.altitude = newAngle;
		}
		else
		{
			_statusMessage = "Pitch controls locked in PLANET & ORBIT modes.";
		}
	}
	
	public string gpsStateToMode(){
		switch(this.gpsState){
			case 0:
				this.gpsMode = "OFF";
				break;
			case 1:
				this.gpsMode = "NORMAL";
				break;
			case 2:
				this.gpsMode = "SHOW_ACTIVE";
				break;
			default:
				this.gpsMode = "ERROR";
				break;
		}
		
		return this.gpsMode;
	}
}


// LOCATION //
public class Location
{
	public String name;
	public Vector3 position;
	public List<Vector3> transformedCoords;
	public String color;
	
	public Location(){}
}


// PLANET //
public class Planet : Location
{
	public float radius;
	public Vector3 point1;
	public Vector3 point2;
	public Vector3 point3;
	public Vector3 point4;
	public Vector2 mapPos;
	public bool isCharted;


	public Planet(String planetString)
	{
		string[] planetData = planetString.Split(';');

		this.name = planetData[0];
		
		this.transformedCoords = new List<Vector3>();

		if(planetData.Length < 8)
		{
			return;
		}

		this.color = planetData[3];

		if(planetData[1] != "")
		{
			this.position = StringToVector3(planetData[1]);
		}

		if(planetData[2] != "")
		{
			this.radius = float.Parse(planetData[2]);
			this.isCharted = true;
		}
		else
		{
			this.isCharted = false;
		}

		if(planetData[4] != ""){
			this.SetPoint(1, StringToVector3(planetData[4]));
		}

		if(planetData[5] != "")
		{ 
			this.SetPoint(2, StringToVector3(planetData[5]));
		}

		if(planetData[6] != "")
		{
			this.SetPoint(3, StringToVector3(planetData[6]));
		}

		if(planetData[7] != "")
		{
			this.SetPoint(4, StringToVector3(planetData[7]));
		}
	}

	public void SetPoint(int point, Vector3 vec3)
	{
		switch(point)
		{
			case 1:
				point1 = vec3;
				break;
			case 2:
				point2 = vec3;
				break;
			case 3:
				point3 = vec3;
				break;
			case 4:
				point4 = vec3;
				break;
		}
	}

	public Vector3 GetPoint(int point)
	{
		Vector3 pointN = new Vector3();

		switch(point)
		{
			case 1:
				pointN = point1;
				break;
			case 2:
				pointN = point2;
				break;
			case 3:
				pointN = point3;
				break;
			case 4:
				pointN = point4;
				break;
		}
		return pointN;
	}

	public override String ToString()
	{
		String[] planetData = new String[8];

		planetData[0] = this.name;
		planetData[1] = Vector3ToString(this.position);

		float radius = this.radius;
		if(radius>0)
		{
			planetData[2] = radius.ToString();
		}
		else
		{
			planetData[2] = "";
		}

		planetData[3] = this.color;
		
		for(int c = 4; c<8; c++)
		{
			if(this.GetPoint(c-3) != Vector3.Zero)
			{
				planetData[c] = Vector3ToString(this.GetPoint(c-3));
			}
		}

		String planetString = planetData[0];
		for(int i=1; i<8; i++)
		{
			planetString = planetString + ";" + planetData[i];
		}
		return planetString;
	}

	public void SetMajorAxes()
	{
	//USE RADIUS AND CENTER OF PLANET TO SET POINTS 1, 2 & 3 TO BE ALONG X,Y & Z AXES FROM PLANET CENTER
		float xCenter = position.X;
		float yCenter = position.Y;
		float zCenter = position.Z;

		Vector3 xMajor = new Vector3(xCenter + radius, yCenter, zCenter);
		Vector3 yMajor = new Vector3(xCenter, yCenter + radius, zCenter);
		Vector3 zMajor = new Vector3(xCenter, yCenter, zCenter + radius);

		this.SetPoint(1, xMajor);
		this.SetPoint(2, yMajor);
		this.SetPoint(3, zMajor);
	}

	public void CalculatePlanet()
	{
	//GET TVALUES OF ALL POINTS THEN ADD TO ARRAY
		double t1 = TValue(point1);
		double t2 = TValue(point2);
		double t3 = TValue(point3);
		double t4 = TValue(point4);
		double[] arrT = new double[]{t1,t2,t3,t4};

	//BUILD MATRIX T WITH POINTS 1,2,3 & 4, AND A COLUMN OF 1's
		double[,] matrixT = new double[4,4];
		for(int c = 0; c<3; c++)
		{
			matrixT[0,c] = point1.GetDim(c);
		}
		for(int d = 0; d<3; d++)
		{
			matrixT[1,d] = point2.GetDim(d);
		}
		for(int e = 0; e<3; e++)
		{
			matrixT[2,e] = point3.GetDim(e);
		}
		for(int f = 0; f<3; f++)
		{
			matrixT[3,f] = point4.GetDim(f);
		}
		for(int g = 0; g<4; g++)
		{
			matrixT[g,3] = 1;
		}

		double[,] matrixD = new double [4,4];
				ReplaceColumn(matrixT, matrixD, arrT, 0);

		double[,] matrixE = new double [4,4];
		ReplaceColumn(matrixT, matrixE, arrT, 1);

				double[,] matrixF = new double [4,4];
		ReplaceColumn(matrixT, matrixF, arrT, 2);

		double[,] matrixG = new double [4,4];
		ReplaceColumn(matrixT, matrixG, arrT, 3);

		double detT = Det4(matrixT);
		double detD = Det4(matrixD)/detT;
		double detE = Det4(matrixE)/detT;
		double detF = Det4(matrixF)/detT;
		double detG = Det4(matrixG)/detT;

		Vector3 newCenter = new Vector3(detD/-2, detE/-2, detF/-2);
		this.position = newCenter;

		double newRad =	 Math.Sqrt(detD*detD + detE*detE + detF*detF - 4*detG)/2;
		this.radius = (float) newRad;

		this.SetMajorAxes();
	}
}


// WAYPOINT //
public class Waypoint : Location
{
	public String marker;
	public bool isActive;

	public Waypoint()
	{
		this.transformedCoords = new List<Vector3>();
	}	
}


// PROGRAM ///////////////////////////////////////////////////////////////////////////////////////////////
public Program()
{	
	//Load Saved Variables
	String[] loadData = Storage.Split('\n');
	if(loadData.Length > 8)
	{
	  //Previously Compiled
	  _planetIndex = int.Parse(loadData[0]);
	  _gpsActive = bool.Parse(loadData[1]);
	  _azSpeed = int.Parse(loadData[2]);
	  _trackSpeed = StringToVector3(loadData[3]);
	  _showMapParameters = bool.Parse(loadData[4]);
	  _showNames = bool.Parse(loadData[5]);
	  _pageIndex = int.Parse(loadData[6]);
	  _brightnessMod = float.Parse(loadData[7]);
	  _showShip = bool.Parse(loadData[8]);
	}
	else
	{
		//Newly Compiled
		_planetIndex = 0;
		_gpsActive = true;
		_azSpeed = 0;
		_trackSpeed = new Vector3(0,0,0);
		_showMapParameters = true;
		_showNames = true;
		_pageIndex = 0;
		_brightnessMod = 1;
		_showShip = true;
	}

	string oldData = Me.CustomData;
	string newData = DEFAULT_SETTINGS;

	_statusMessage = "";
	_planetToLog = false;

	if(!oldData.Contains("[Map Settings]")){
		if(oldData.StartsWith("[")){
			newData += oldData;
		}
		else{
			newData += "---\n\n" + oldData;
		}
		Me.CustomData = newData;
	}

	refresh();
	_previousCommand = "NEWLY LOADED";
	
	 // Set the continuous update frequency of this script
	if(_slowMode)
		Runtime.UpdateFrequency = UpdateFrequency.Update100;
	else
		Runtime.UpdateFrequency = UpdateFrequency.Update10;
}


public void Save()
{
	String saveData = _planetIndex.ToString() + "\n" + _gpsActive.ToString() + "\n" + _azSpeed.ToString();
	saveData += "\n" + Vector3ToString(_trackSpeed) + "\n" + _showMapParameters.ToString() + "\n" + _showNames.ToString();
	saveData += "\n" + _pageIndex.ToString() + "\n" + _brightnessMod.ToString() + "\n" + _showShip.ToString();

	Storage = saveData;
}


// MAIN ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
public void Main(string argument)
{
	_planets = _planetList.Count > 0;
	_myPos = _refBlock.GetPosition();
		
	Echo("////// PLANET MAP 3D ////// " + _cycleSpinner[_cycleStep % _cycleSpinner.Length]);
	Echo(_previousCommand);
	Echo(_statusMessage);
	Echo("MAP Count: " + _mapList.Count);
	
	if(_dataSurface == null){
		Echo("Data Screen: Unassigned");
	}else{
		Echo("Data Screen: Active");
	}

	bool hasPlanets = _planetList.Count > 0;

	if(hasPlanets)
	{
		Echo("Planet Count: " + _planetList.Count);
		Planet planet = _planetList[_planetList.Count - 1];
	}
	else
	{
		Echo("No Planets Logged!");
	}

	if(_waypointList.Count > 0)
	{
		Echo("GPS Count: " + _waypointList.Count + "\n");
		//Waypoint waypoint = _waypointList[_waypointList.Count - 1];
	}
	else
	{
		Echo("No Waypoints Logged!");
	}
	
	if(_mapList.Count > 0)
	{
		CycleExecute();
				
		if (argument != "")
		{
			string [] args = argument.Split(' ');
			string [] cmds = args[0].ToUpper().Split('_');
			string command = cmds[0];
			string cmdArg = "";
			if(cmds.Length > 1)
				cmdArg = cmds[1];
			
			// Account for single instance commands with underscores
			if(cmdArg == "RADIUS" || cmdArg == "SHIP" || cmdArg == "JUMP")
				command = args[0];
			
			string argData = "";
			_statusMessage = "";
			_activeWaypoint = "";
			_previousCommand = "Command: " + argument;

			// If there are multiple words in the argument. Combine the latter words into the entity name.
			if(args.Length == 1)
			{
				argData = "0";
			}
			else if(args.Length > 1)
			{
				argData = args[1];
				if(args.Length > 2)
				{
					for(int q = 2; q < args.Length;	 q++)
					{
						argData += " " + args[q];
					}
				}
			}
			
			List<StarMap> maps = ArgToMaps(argData);
			
			switch(command)
			{
				case "ZOOM":
					Zoom(maps, cmdArg);
					break;
				case "MOVE":
					MoveCenter(maps, cmdArg);
					break;
				case "DEFAULT":
					MapsToDefault(maps);
					break;
				case "ROTATE":
					RotateMaps(maps, cmdArg);
					break;
				case "SPIN":
					SpinMaps(maps, cmdArg, ANGLE_STEP/2);
					break;
				case "TRACK":
					TrackCenter(maps, cmdArg);
					break;
				case "STOP":
					StopMaps(maps);
					break;
				case "GPS":
					if(cmdArg == "ON"){
						Show(maps, "GPS", 1);
					}else{
						Show(maps, "GPS", 0);
					}
					break;
				case "HIDE":
					if(cmdArg == "WAYPOINT"){
						SetWaypointState(argData, 0);
					}else{
						Show(maps, cmdArg, 0);
					}
					break;
				case "SHOW":
					if(cmdArg == "WAYPOINT"){
						SetWaypointState(argData, 1);
					}else{
						Show(maps, cmdArg, 1);
					}
					break;
				case "TOGGLE":
					if(cmdArg == "WAYPOINT"){
						SetWaypointState(argData, 2);
					}else{
						Show(maps, cmdArg, 3);
					}
					break;
				case "CYCLE"://GPS
					cycleGPS(maps);
					break;
				case "NEXT":
					nextLast(maps, cmdArg, true);
					break;
				case "PREVIOUS":
					nextLast(maps, cmdArg, false);
					break;
				case "WORLD"://MODE
					ChangeMode("WORLD", maps);
					break;
				case "SHIP"://MODE
					ChangeMode("SHIP", maps);
					break;
				case "CHASE"://MODE
					ChangeMode("CHASE", maps);
					break;
				case "PLANET"://MODE
					ChangeMode("PLANET", maps);
					break;
				case "FREE"://MODE
					ChangeMode("FREE", maps);
					break;
				case "ORBIT"://MODE
					ChangeMode("ORBIT", maps);
					break;
				case "DECREASE_RADIUS":
					AdjustRadius(maps, false);
					break;
				case "INCREASE_RADIUS":
					AdjustRadius(maps, true);
					break;
				case "CENTER_SHIP":
					MapsToShip(maps);
					break;
				case "WAYPOINT":
					waypointCommand(cmdArg, argData);
					break;
				case "PASTE":
					ClipboardToLog(cmdArg, argData);
					break;
				case "EXPORT"://WAYPOINT
					_clipboard = LogToClipboard(argData);
					break;
				case "NEW"://PLANET
					NewPlanet(argData);
					break;
				case "LOG":
					if(cmdArg == "NEXT"){
						LogNext(argData);
					}else if(cmdArg == "BATCH"){
						LogBatch();
					}else{
						LogWaypoint(argData, _myPos, cmdArg, "WHITE");
					}
					break;
				case "COLOR":
					if(cmdArg == "PLANET"){
						SetPlanetColor(argData);
					}else{
						SetWaypointColor(argData);
					}
					break;
				case "MAKE":
					SetWaypointType(cmdArg, argData);
					break;
				case "PLOT_JUMP":
					PlotJumpPoint(argData);
					break;
				case "SCROLL":
					pageScroll(cmdArg);
					break;
				case "BRIGHTEN":
					if(_brightnessMod < BRIGHTNESS_LIMIT)
						_brightnessMod += 0.25f;
					break;
				case "DARKEN":
					if(_brightnessMod > 1)
						_brightnessMod -= 0.25f;
					break;
				case "DELETE":
					if(cmdArg == "PLANET"){
						DeletePlanet(argData);
					}else{
						SetWaypointState(argData, 3);
					}
					break;
				case "SYNC":
					sync(cmdArg, argData);
					break;
				case "REFRESH":
					refresh();
					break;
				case "UPDATE":
					if(cmdArg == "TAGS")
						updateTags();
					break;
				default:
					_statusMessage = "UNRECOGNIZED COMMAND!";
					break;
			}
			
			if(maps.Count > 0)
			{
				foreach(StarMap cmdMap in maps)
				{
					UpdateMap(cmdMap);
					MapToParameters(cmdMap);
				}
			}
		}
		
		if(hasPlanets)
		{
			if(_cycleStep == _cycleLength || _previousCommand == "NEWLY LOADED")
			{
				SortByNearest(_planetList);
			}
			_nearestPlanet = _planetList[0];
		}

		foreach(StarMap map in _mapList)
		{			
			if(map.mode == "CHASE")
			{
				AlignShip(map);
			}
			else if(map.mode == "PLANET" && hasPlanets)
			{
				ShipToPlanet(map);
			}
			else if(map.mode == "ORBIT")
			{
				AlignOrbit(map);
			}
			else
			{
				map.azimuth = DegreeAdd(map.azimuth, map.dAz);
				
				if(map.mode != "SHIP")
				{
					Vector3 deltaC = new Vector3(map.dX, map.dY, map.dZ);
					map.center += rotateMovement(deltaC, map);
				}
				else
				{
					map.center = _myPos;
				}
				
				UpdateMap(map);
			}
			
			// Begin a new frame
			_frame = map.drawingSurface.DrawFrame();
			
			// All sprites must be added to the frame here
			DrawSprites(map);
			
			// We are done with the frame, send all the sprites to the text panel
			_frame.Dispose();
			
			_statusMessage = Me.DefinitionDisplayName;
		}
	}
	else
	{
		updateTags();
		
		if(_mapList.Count < 1)
			_statusMessage = "NO MAP DISPLAY FOUND!\nPlease add tag " + _mapTag + " to desired block.\n";
	}

	if(_dataBlocks.Count > 0)
		DisplayData();
}


// VIEW FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// SHOW //
void Show(List<StarMap> maps, string attribute, int state)
{
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		switch(attribute)
		{
			case "GPS":
				if(state == 3){
					cycleGPS(maps);
				}else{
					map.gpsState = state;
				}
				break;
			case "NAMES":
				map.showNames = setState(map.showNames, state);
				break;
			case "SHIP":
				map.showShip = setState(map.showShip, state);
				break;
			case "INFO":
				map.showInfo = setState(map.showInfo, state);
				break;
			default:
				_statusMessage = "INVALID DISPLAY COMMAND";
				break;
		}
		
		MapToParameters(map);
	}
}


// CYCLE GPS //
void cycleGPS(List<StarMap> maps){
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps){
		map.gpsState++;
		if(map.gpsState > 2)
			map.gpsState = 0;
	}
}


// PARAMETERS TO MAPS //
public List<StarMap> ParametersToMaps(IMyTerminalBlock mapBlock)
{
	List<StarMap> mapsOut = new List<StarMap>();
	
	MyIni lcdIni = DataToIni(mapBlock);
	
	if(!mapBlock.CustomData.Contains("[mapDisplay]"))
	{
		string oldData = mapBlock.CustomData.Trim();
		string newData = _defaultDisplay;

		if(oldData.StartsWith("["))
		{
			newData += "\n\n" + oldData;
		}
		else if(oldData != "")
		{
			newData += "\n---\n"+ oldData;
		}

		mapBlock.CustomData = newData;
	}

	string indexString;

	if(!mapBlock.CustomData.Contains("Indexes"))
	{
		indexString = "";
	}
	else
	{
		indexString = lcdIni.Get("mapDisplay","Indexes").ToString();
	}

	string[] indexStrings = indexString.Split(',');
	int iLength = indexStrings.Length;
	
	//Split Ini parameters into string lists. Insure all lists are the same length.
	List<string> centers = StringToEntries(lcdIni.Get("mapDisplay","Center").ToString(), ';', iLength, "(0,0,0)");
	List<string> fLengths = StringToEntries(lcdIni.Get("mapDisplay","FocalLength").ToString(), ',', iLength, DV_FOCAL.ToString());
	List<string> radii = StringToEntries(lcdIni.Get("mapDisplay","RotationalRadius").ToString(), ',', iLength, DV_RADIUS.ToString());
	List<string> azimuths = StringToEntries(lcdIni.Get("mapDisplay", "Azimuth").ToString(), ',', iLength, "0");
	List<string> altitudes = StringToEntries(lcdIni.Get("mapDisplay", "Altitude").ToString(), ',', iLength, DV_ALTITUDE.ToString());
	List<string> modes = StringToEntries(lcdIni.Get("mapDisplay","Mode").ToString(), ',', iLength, "FREE");
	List<string> gpsModes = StringToEntries(lcdIni.Get("mapDisplay","GPS").ToString(), ',', iLength, "true");
	List<string> nameBools = StringToEntries(lcdIni.Get("mapDisplay","Names").ToString(), ',', iLength, "true");
	List<string> shipBools = StringToEntries(lcdIni.Get("mapDisplay","Ship").ToString(), ',', iLength, "true");
	List<string> infoBools = StringToEntries(lcdIni.Get("mapDisplay","Info").ToString(), ',', iLength, "true");
	List<string> planets = StringToEntries(lcdIni.Get("mapDisplay","Planet").ToString(), ',', iLength, "[null]");
	List<string> waypoints = StringToEntries(lcdIni.Get("mapDisplay","Waypoint").ToString(), ',', iLength, "[null]");
	
	//assemble maps by position in string lists.
	for(int i = 0; i < iLength; i++)
	{	
		StarMap map = new StarMap();
		if(indexString == "")
		{
			map.index = _mapIndex;
		}
		else
		{
			map.index = int.Parse(indexStrings[i]);
		}

		map.block = mapBlock;
		map.center = StringToVector3(centers[i]);
		map.focalLength = int.Parse(fLengths[i]);
		map.rotationalRadius = int.Parse(radii[i]);
		map.azimuth = int.Parse(azimuths[i]);	
		map.altitude = int.Parse(altitudes[i]);
		map.mode = modes[i];
		
		map.gpsMode = gpsModes[i];
		switch(map.gpsMode.ToUpper()){
			case "OFF":
			case "FALSE":
				map.gpsState = 0;
				break;
			case "SHOW_ACTIVE":
				map.gpsState = 2;
				break;
			default:
				map.gpsState = 1;
				break;
		}
		
		map.showNames = ParseBool(nameBools[i]);
		map.showShip = ParseBool(shipBools[i]);
		map.showInfo = ParseBool(infoBools[i]);
		map.activePlanetName = planets[i];
		map.activePlanet = GetPlanet(map.activePlanetName);
		map.activeWaypointName = waypoints[i];
		map.activeWaypoint = GetWaypoint(map.activeWaypointName);
		
		mapsOut.Add(map);
	}
	
	return mapsOut;
}


// DATA TO LOG //
public void DataToLog()
{
	MyIni mapIni = DataToIni(Me);

	if(_waypointList.Count > 0)
	{
		String waypointData = "";
		foreach(Waypoint waypoint in _waypointList)
		{
			waypointData += WaypointToString(waypoint) + "\n";
		}
		mapIni.Set("Map Settings", "Waypoint_List",waypointData);
	}

	String planetData = "";
	if(_planets)
	{
		foreach(Planet planet in _planetList)
		{
			planetData += planet.ToString() + "\n";
		}
	}

	if(_unchartedList.Count > 0)
	{
		foreach(Planet uncharted in _unchartedList)
		{
			planetData += uncharted.ToString() + "\n";
		}
	}

	if(planetData != "")
	{
		mapIni.Set("Map Settings", "Planet_List", planetData);
	}

	Me.CustomData = mapIni.ToString();
}


// MAP TO PARAMETERS // Writes map object to CustomData of Display Block
public void MapToParameters(StarMap map)
{
	MyIni lcdIni = DataToIni(map.block);
	
	int i = 0;
	
	string blockIndex = lcdIni.Get("mapDisplay", "Indexes").ToString();
	string[] indexes = blockIndex.Split(',');
	
	int entries = indexes.Length;
	
	if(entries > 0)
	{
		for(int j = 0; j < entries; j++)
		{
			if(map.index.ToString() == indexes[j])
			{
				i = j;  //This is the array position of the screen index for this map.
			}
		}
	}
	
	// Read the old Ini Data and split into string arrays. Insert the new data into the arrays.
	string newIndexes = InsertEntry(map.index.ToString(), blockIndex, ',', i, entries, "0");
	string newCenters = InsertEntry(Vector3ToString(map.center), lcdIni.Get("mapDisplay", "Center").ToString(), ';', i, entries, "(0,0,0)");
	string newModes = InsertEntry(map.mode, lcdIni.Get("mapDisplay", "Mode").ToString(), ',', i, entries, "FREE");
	string newFocal = InsertEntry(map.focalLength.ToString(), lcdIni.Get("mapDisplay", "FocalLength").ToString(), ',', i, entries, DV_FOCAL.ToString());
	string newRadius = InsertEntry(map.rotationalRadius.ToString(), lcdIni.Get("mapDisplay", "RotationalRadius").ToString(), ',', i, entries, DV_RADIUS.ToString());
	string newAzimuth = InsertEntry(map.azimuth.ToString(), lcdIni.Get("mapDisplay", "Azimuth").ToString(), ',', i, entries, "0");
	string newAltitude = InsertEntry(map.altitude.ToString(), lcdIni.Get("mapDisplay", "Altitude").ToString(), ',', i, entries, DV_ALTITUDE.ToString());
	string newDX = InsertEntry(map.dX.ToString(), lcdIni.Get("mapDisplay", "dX").ToString(), ',', i, entries, "0");
	string newDY = InsertEntry(map.dY.ToString(), lcdIni.Get("mapDisplay", "dY").ToString(), ',', i, entries, "0");
	string newDZ = InsertEntry(map.dZ.ToString(), lcdIni.Get("mapDisplay", "dZ").ToString(), ',', i, entries, "0");
	string newDAz = InsertEntry(map.dAz.ToString(), lcdIni.Get("mapDisplay", "dAz").ToString(), ',', i, entries, "0");
	string newGPS = InsertEntry(map.gpsStateToMode(), lcdIni.Get("mapDisplay", "GPS").ToString(), ',', i, entries, "True");
	string newNames = InsertEntry(map.showNames.ToString(), lcdIni.Get("mapDisplay", "Names").ToString(), ',', i, entries, "True");
	string newShip = InsertEntry(map.showShip.ToString(), lcdIni.Get("mapDisplay", "Ship").ToString(), ',', i, entries, "True");	
	string newInfo = InsertEntry(map.showInfo.ToString(), lcdIni.Get("mapDisplay", "Info").ToString(), ',', i, entries, "True");
	string newPlanets = InsertEntry(map.activePlanetName, lcdIni.Get("mapDisplay", "Planet").ToString(), ',',i, entries, "[null]");
	string newWaypoints = InsertEntry(map.activeWaypointName, lcdIni.Get("mapDisplay", "Waypoint").ToString(), ',',i, entries, "[null]");
	
	// Update the Ini Data.
	lcdIni.Set("mapDisplay", "Center", newCenters);
	lcdIni.Set("mapDisplay", "Mode", newModes);
	lcdIni.Set("mapDisplay", "FocalLength", newFocal);
	lcdIni.Set("mapDisplay", "RotationalRadius", newRadius);
	lcdIni.Set("mapDisplay", "Azimuth", newAzimuth);
	lcdIni.Set("mapDisplay", "Altitude", newAltitude);
	lcdIni.Set("mapDisplay", "Indexes", newIndexes);
	lcdIni.Set("mapDisplay", "dX", newDX);
	lcdIni.Set("mapDisplay", "dY", newDY);
	lcdIni.Set("mapDisplay", "dZ", newDZ);
	lcdIni.Set("mapDisplay", "dAz", newDAz);
	lcdIni.Set("mapDisplay", "GPS", newGPS);
	lcdIni.Set("mapDisplay", "Names", newNames);
	lcdIni.Set("mapDisplay", "Ship", newShip);
	lcdIni.Set("mapDisplay", "Info", newInfo);
	lcdIni.Set("mapDisplay", "Planet", newPlanets);
	lcdIni.Set("mapDisplay", "Waypoint", newWaypoints);
	
	map.block.CustomData = lcdIni.ToString();
}


// CLIPBOARD TO LOG //
void ClipboardToLog(string markerType, string clipboard)
{
	string[] waypointData = clipboard.Split(':');
	if(waypointData.Length < 6)
	{
		_statusMessage = "Does not match GPS format:/nGPS:<name>:X:Y:Z:<color>:";
		return;
	}
	
	Vector3 position = new Vector3(float.Parse(waypointData[2]),float.Parse(waypointData[3]),float.Parse(waypointData[4]));
	LogWaypoint(waypointData[1], position, markerType, waypointData[5]);
}


// LOG TO CLIPBOARD //
string LogToClipboard(string waypointName)
{
	Waypoint waypoint = GetWaypoint(waypointName);
	if(waypoint == null)
	{
		_statusMessage = "No waypoint " + waypointName + " found!";
		return _statusMessage;
	}

	Vector3 location = waypoint.position;
	string output = "GPS:" + waypoint.name + ":" + location.X + ":" + location.Y + ":" + location.Z + ":#FF75C9F1:";

	return output;
}


// LOG WAYPOINT //
public void LogWaypoint(String waypointName, Vector3 position, String markerType, String waypointColor)
{
	if(waypointName == "")
	{
		_statusMessage = "No Waypoint Name Provided! Please Try Again.\n";
		return;
	}

	Waypoint waypoint = GetWaypoint(waypointName);

	if(waypoint != null)
	{
		_statusMessage = "Waypoint " + waypointName + " already exists! Please choose different name.\n";
		return;
	}	 
	
	waypoint = new Waypoint();
	waypoint.name = waypointName;
	waypoint.position = position;
	waypoint.marker = markerType;
	waypoint.isActive = true;
	waypoint.color = waypointColor;

	_waypointList.Add(waypoint);
	DataToLog();
	foreach(StarMap map in _mapList)
	{
		UpdateMap(map);
	}
}


// SET WAYPOINT STATE //
public void SetWaypointState(String waypointName, int state)
{
	Waypoint waypoint = GetWaypoint(waypointName);
	
	if(waypoint == null)
	{
		WaypointError(waypointName);
		return;
	}

	//State Switch: 0 => Deactivate, 1 => Activate, 2 => Toggle
	switch(state)
	{
		case 0:
			waypoint.isActive = false;
			break;
		case 1:
			waypoint.isActive = true;
			break;
		case 2:
			waypoint.isActive = !waypoint.isActive;
			break;
		case 3:
			_waypointList.Remove(waypoint);
			_statusMessage = "Waypoint deleted: " + waypointName + " \n";
			break;
		default:
			_statusMessage = "Invalid waypoint state int!\n";
			break;
	}

	DataToLog();
}


// PLOT JUMP POINT //
void PlotJumpPoint(string planetName)
{
	Planet planet = GetPlanet(planetName);
	{
		if(planet == null)
		{
			PlanetError(planetName);
		} 
	}
	
	int designation = 1;
	string name = planet.name + " Orbit ";
	Waypoint jumpPoint = GetWaypoint(name + designation);
	
	while(jumpPoint != null)
	{
		designation++;
		jumpPoint = GetWaypoint(name + designation);
	}
	
	Vector3 position = planet.position + (_myPos - planet.position)/Vector3.Distance(_myPos, planet.position)* planet.radius * JUMP_RATIO;
	
	LogWaypoint(name + designation, position, "WAYPOINT", "WHITE");
}


// PROJECT POINT//
void ProjectPoint(string marker, string arg)
{
	string [] args = arg.Split(' ');
	
	if(args.Length < 2){
		_statusMessage = "INSUFFICIENT ARGUMENT!\nPlease include arguments <DISTANCE(in meters)> <WAYPOINT NAME>";
		return;
	}
	
	int distance;
	if(int.TryParse(args[0],out distance)){
		string name = "";
		for(int i = 1; i < args.Length; i++)
		{
			name += args[i] + " ";
		}

		Vector3 location = _myPos + _refBlock.WorldMatrix.Forward * distance;
		
		LogWaypoint(name.Trim(), location, marker, "WHITE");
	
		return;
	}

	_statusMessage = "DISTANCE ARGEMENT FAILED!\nPlease include Distance in meters. Do not include unit.";
}


// PLANET ERROR //
void PlanetError(string name){
	_statusMessage = "No planet " + name + " found!";
}


// WAYPOINT ERROR //
void WaypointError(string name){
	_statusMessage = "No waypoint " + name + " found!";
}


// NEW PLANET //
public void NewPlanet(String planetName)
{
	Planet planet = GetPlanet(planetName);
	
	if(planet != null)
	{
		_statusMessage = "Planet " + planetName + " already exists! Please choose different name.\n";
		return;
	}

	planetName += ";;;;;;;";
	planet = new Planet(planetName);
	planet.SetPoint(1, _myPos);

	_unchartedList.Add(planet);
	DataToLog();
}


// DELETE PLANET //
public void DeletePlanet(String planetName)
{
	Planet alderaan = GetPlanet(planetName);

	if(alderaan == null)
	{
		PlanetError(planetName);
		return;
	}

	_unchartedList.Remove(alderaan);
	_planetList.Remove(alderaan);
	DataToLog();
	_statusMessage = "PLANET DELETED: " + planetName +"\n\nDon't be too proud of this TECHNOLOGICAL TERROR you have constructed. The ability to DESTROY a PLANET is insignificant next to the POWER of the FORCE.\n";
	
	if(_planets)
		_nearestPlanet = _planetList[0];
}


// LOG NEXT //
public void LogNext(String planetName)
{
	Planet planet = GetPlanet(planetName);
	
	if(planet == null)
	{
		PlanetError(planetName);
		return;
	}

	String[] planetData = planet.ToString().Split(';');

	if(planetData[4] == "")
	{
		planet.SetPoint(1, _myPos);
	}
	else if(planetData[5] == "")
	{
		planet.SetPoint(2, _myPos);
	}
	else if(planetData[6] == "")
	{
		planet.SetPoint(3, _myPos);
	}
	else
	{
		planet.SetPoint(4, _myPos);
		
		if(!planet.isCharted)
		{
			_planetList.Add(planet);
			_unchartedList.Remove(planet);
		}

		planet.CalculatePlanet();
		_planetToLog = true; // Specify that DataToLog needs to be called in CycleExecute.

		foreach(StarMap map in _mapList)
		{
			UpdateMap(map);
		}
	}

	DataToLog();
}


// SET PLANET COLOR //
void SetPlanetColor(String argument){
	String[] args = argument.Split(' ');
	String planetColor = args[0];
	
	if(args.Length < 2){
		_statusMessage = "Insufficient Argument.  COLOR_PLANET requires COLOR and PLANET NAME.\n";
	}else{
		String planetName = "";
		for(int p = 1; p < args.Length; p++)
		{
			planetName += args[p] + " ";
		}
		planetName = planetName.Trim(' ').ToUpper();

		Planet planet = GetPlanet(planetName);
		
		if(planet != null){
			planet.color = planetColor;
			_statusMessage = planetName + " color changed to " + planetColor + ".\n";
			DataToLog();
			return;
		}
		
		PlanetError(planetName);
	}
}


// SET WAYPOINT COLOR //
void SetWaypointColor(String argument){
	String[] args = argument.Split(' ');
	String waypointColor = args[0];
	
	if(args.Length < 2){
		_statusMessage = "Insufficient Argument.  COLOR_WAYPOINT requires COLOR and WAYPOINT NAME.\n";
	}else{
		String waypointName = "";
		for(int w = 1; w < args.Length; w++)
		{
			waypointName += args[w] + " ";
		}
		waypointName = waypointName.Trim(' ').ToUpper();

		Waypoint waypoint = GetWaypoint(waypointName);
		
		if(waypoint != null){
			waypoint.color = waypointColor;
			_statusMessage = waypointName + " color changed to " + waypointColor + ".\n";
			DataToLog();
			return;
		}
		
		WaypointError(waypointName);
	}
}


// SET WAYPOINT TYPE //
void SetWaypointType(string arg, string waypointName){
	Waypoint waypoint = GetWaypoint(waypointName);
	
	if(waypoint == null){
		WaypointError(waypointName);
		return;
	}
	
	waypoint.marker = arg;
	DataToLog();
}

// ZOOM // Changes Focal Length of Maps. true => Zoom In / false => Zoom Out
void Zoom(List<StarMap> maps, string arg)
{
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps)
	{
		int doF = map.focalLength;
		float newScale;
		
		if(arg == "IN")
		{
			newScale = doF*ZOOM_STEP;
		}
		else
		{
			newScale = doF/ZOOM_STEP;
		}
		
		
		if(newScale > ZOOM_MAX)
		{
			doF = ZOOM_MAX;
		}
		else if(newScale < 1)
		{
			doF = 1;
		}
		else
		{
			doF = (int) newScale;
		}

		map.focalLength = doF;
	}
}


// ADJUST RADIUS //
void AdjustRadius(List<StarMap> maps, bool increase)
{
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		int radius = map.rotationalRadius;
		
		if(increase)
		{
			radius *= 2;
		}
		else
		{
			radius /= 2;
		}
		
		if(radius < map.focalLength)
		{
			radius = map.focalLength;
		}
		else if(radius > MAX_VALUE)
		{
			radius = MAX_VALUE;
		}
		
		map.rotationalRadius = radius;	
	}
}


// MOVE CENTER //
void MoveCenter(List<StarMap> maps, string movement)
{
	if(NoMaps(maps))
		return;
	
	float step = (float) MOVE_STEP;
	float x = 0;
	float y = 0;
	float z = 0;

	switch(movement)
	{
		case "LEFT":
			x = step;
			break;
		case "RIGHT":
			x = -step;
			break;
		case "UP":
			y = step;
			break;
		case "DOWN":
			y = -step;
			break;
		case "FORWARD":
			z = step;
			break;
		case "BACKWARD":
			z = -step;
			break;
		}
		Vector3 moveVector = new Vector3(x,y,z);
	
	foreach(StarMap map in maps)	
	{
		if(map.mode == "FREE" || map.mode == "WORLD")
		{
			map.center += rotateMovement(moveVector, map);
		}
		else
		{
			_statusMessage = "Translation controls only available in FREE & WORLD modes.";
		}
	}
}


// TRACK CENTER //		Adjust translational speed of map.
void TrackCenter(List<StarMap> maps, string direction)
{
	if(NoMaps(maps))
		return;	
		
	foreach(StarMap map in maps)
	{
		switch(direction)
		{
			case "LEFT":
				map.dX += MOVE_STEP;
				break;
			case "RIGHT":
				map.dX -= MOVE_STEP;
				break;
			case "UP":
				map.dY += MOVE_STEP;
				break;
			case "DOWN":
				map.dY -= MOVE_STEP;
				break;
			case "FORWARD":
				map.dZ += MOVE_STEP;
				break;
			case "BACKWARD":
				map.dZ -= MOVE_STEP;
				break;
			default:
				_statusMessage = "Error with Track Command";
				break;
		}
	}
}


// ROTATE MAPS //
void RotateMaps(List<StarMap> maps, string direction)
{
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps)
	{
		switch(direction)
		{
			case "LEFT":
				map.yaw(ANGLE_STEP);
				break;
			case "RIGHT":
				map.yaw(-ANGLE_STEP);
				break;
			case "UP":
				map.pitch(-ANGLE_STEP);
				break;
			case "DOWN":
				map.pitch(ANGLE_STEP);
				break;
		}
	}
}


// SPIN MAPS //		Adjust azimuth speed of maps.
void SpinMaps(List<StarMap> maps, string direction, int deltaAz)
{
	if(NoMaps(maps))
		return;
		
	if(direction == "RIGHT")
		deltaAz *= -1;
		
	foreach(StarMap map in maps)
	{
		map.dAz += deltaAz;
	}
}


// STOP MAPS //		Halt all translational and azimuthal speed in maps.
void StopMaps(List<StarMap> maps)
{
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		map.dX = 0;
		map.dY = 0;
		map.dZ = 0;
		map.dAz = 0;
	}
}


// MAPS TO SHIP //		Centers maps on ship
void MapsToShip(List<StarMap> maps)
{
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps)
	{
		map.center = _myPos;
	}
}


// CENTER WORLD //	   Updates Map Center to the Average of all charted Planets
void CenterWorld(StarMap map)
{
	map.altitude = -15;
	map.azimuth = 45;
	map.focalLength = 256;
	map.rotationalRadius = 4194304;
	Vector3 worldCenter = new Vector3(0,0,0);

	if(_planets)
	{
		foreach(Planet planet in _planetList)
		{
			worldCenter += planet.position;
		}

		worldCenter /= _planetList.Count;
	}

	map.center = worldCenter;
}


// CENTER SHIP //
void CenterShip(StarMap map)
{
	DefaultView(map);
	map.center = _myPos;
}


// ALIGN SHIP //
void AlignShip(StarMap map)
{	
	Vector3 heading = _refBlock.WorldMatrix.Forward;
	int newAz = DegreeAdd((int) ToDegrees((float) Math.Atan2(heading.Z, heading.X)), -90);
	
	int newAlt = (int) ToDegrees((float) Math.Asin(heading.Y));// -25;
	if(newAlt < -90){
		newAlt = DegreeAdd(newAlt, 180);
		newAz = DegreeAdd(newAz, 180);
	}
	
	map.altitude = newAlt;
	map.azimuth = newAz;
	map.center = _myPos;
}


// ALIGN ORBIT //
void AlignOrbit(StarMap map)
{
	if(_planetList.Count < 1){
		return;
	}
	
	if(map.activePlanet == null){
		if(_nearestPlanet == null){
			Echo("No Nearest Planet Set!");
			return;
		}
		
		SelectPlanet(_nearestPlanet, map);
	}

	Vector3 planetPos = map.activePlanet.position;

	map.center = (_myPos + planetPos)/2;
	map.altitude = 0;
	Vector3 orbit = _myPos - planetPos;
	map.azimuth = (int) ToDegrees((float)Math.Abs(Math.Atan2(orbit.Z, orbit.X)+ (float)Math.PI*0.75f)); //  )
	
	//Get largest component distance between ship and planet.
	//double span = Math.Sqrt(orbit.LengthSquared() - Math.Pow(orbit.Y,2));
	float span = orbit.Length();
	/*
	if(orbit.Y > span)
	{
		span = orbit.Y * 2;
		_statusMessage = "WHOA MAN, THAT's DEEP!";
	}
	*/
	double newRadius = 1.25f * map.focalLength * span / map.viewport.Height;

	if(newRadius > MAX_VALUE || newRadius < 0)
	{
		newRadius = MAX_VALUE;
		double newZoom = 0.8f * map.viewport.Height * (MAX_VALUE / span);
		map.focalLength = (int) newZoom;
	}
	
	map.rotationalRadius = (int) newRadius;
}


// PLANET MODE //
void PlanetMode(StarMap map)
{
	map.focalLength = DV_FOCAL;
	map.dAz = 0;
	map.dX = 0;
	map.dY = 0;
	map.dZ = 0;
	
	if(map.viewport.Width > 500)
	{
		map.focalLength *= 4;
	}

	if(_planets)
	{
		SortByNearest(_planetList);
		map.activePlanet = _planetList[0];
		ShipToPlanet(map);

		if(map.activePlanet.radius < 30000)
		{
			map.focalLength *= 4;
		}
	}

	map.rotationalRadius = DV_RADIUS;
	map.mode = "PLANET";
}


// SET MAP MODE //
void SetMapMode(StarMap map, string mapMode)
{
	if (mapMode == "WORLD")
	{
		CenterWorld(map);
	}
	else
	{
		CenterShip(map);
	}
	
	if(mapMode == "PLANET")
	{
		PlanetMode(map);
	}
	else if(mapMode == "ORBIT")
	{
		AlignOrbit(map);
	}
	else if(mapMode == "CHASE")
	{
		AlignShip(map);
	}
	
	map.mode = mapMode;
}


// CHANGE MODE //
void ChangeMode(string mapMode, List<StarMap> maps)
{
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps)
	{
		SetMapMode(map, mapMode);
	}
}


// CYCLE MODE //
void CycleMode(List<StarMap> maps, bool cycleUp)
{
	if(NoMaps(maps))
		return;
		
	_activePlanet = "";
	string[] modes = {"FREE", "SHIP", "CHASE", "PLANET", "ORBIT", "WORLD"};
	int length = modes.Length;

	foreach(StarMap map in maps)
	{
		int modeIndex = 0;
		for(int i = 0; i < length; i++)
		{
			// Find Current Map Mode
			if(map.mode.ToUpper() == modes[i])
			{
				modeIndex = i;
			}
		}
		
		// Cycle Mode Up/Down by 1
		if(cycleUp)
		{
			modeIndex++;
		}
		else
		{
			modeIndex--;
		}
		
		if(modeIndex >= length)
		{
			modeIndex = 0;
		}
		else if(modeIndex < 0)
		{
			modeIndex = length - 1;
		}
		
		SetMapMode(map, modes[modeIndex]);
	}
}


// SHIP TO PLANET //   Aligns the map so that the ship appears above the center of the planet.
void ShipToPlanet(StarMap map)
{
	if(_planets)
	{
		Planet planet = _nearestPlanet;

		Vector3 shipVector = _myPos - planet.position;
		float magnitude = Vector3.Distance(_myPos, planet.position);

		float azAngle = (float) Math.Atan2(shipVector.Z,shipVector.X);
		float altAngle = (float) Math.Asin(shipVector.Y/magnitude);

		map.center = planet.position;
		map.azimuth = DegreeAdd((int) ToDegrees(azAngle),90);
		map.altitude = (int) ToDegrees(-altAngle);
	}
}


// DEFAULT VIEW //
void DefaultView(StarMap map)
{
	map.mode = "FREE";

	map.center = new Vector3(0,0,0);
	map.focalLength = DV_FOCAL;

	if(map.viewport.Width > 500)
	{
		map.focalLength *= 3;
	}

	map.rotationalRadius = DV_RADIUS;
	map.azimuth = 0;
	map.altitude = DV_ALTITUDE;
}


// MAPS TO DEFAULT //
void MapsToDefault(List<StarMap> maps)
{	
	if(maps.Count < 1)
		return;
	
	foreach(StarMap map in maps)
	{
		DefaultView(map);
	}
}


// CYCLE PLANETS //
void CyclePlanets(List<StarMap> maps, bool next)
{
	int planetCount = _planetList.Count;
	
	if(planetCount < 1)
	{
		_statusMessage = "No Planets Logged!";
		return;
	}
	
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		DefaultView(map);
		
		if(next)
		{
			map.planetIndex++;
		}
		else
		{
			map.planetIndex--;
		}
		
		if(map.planetIndex < 0)
		{
			map.planetIndex = planetCount - 1;
		}
		else if(map.planetIndex >= planetCount)
		{
			map.planetIndex = 0;
		}
		
		SelectPlanet(_planetList[map.planetIndex], map);
	}
}


// GET PLANET //
Planet GetPlanet(string planetName)
{
	if(planetName == "" || planetName == "[null]")
		return null;
	
	if(_unchartedList.Count > 0)
	{
		foreach(Planet uncharted in _unchartedList)
		{
			if(uncharted.name.ToUpper() == planetName.ToUpper())
			{
				return uncharted;
			}
		}
	}

	if(_planets)
	{
		foreach(Planet planet in _planetList)
		{
			if(planet.name.ToUpper()== planetName.ToUpper())
			{
				return planet;
			}
		}
	}

	return null;
}


// GET WAYPOINT //
Waypoint GetWaypoint(string waypointName)
{
	if(_waypointList.Count > 0)
	{
		foreach(Waypoint waypoint in _waypointList)
		{
			if(waypoint.name.ToUpper() == waypointName.ToUpper())
			{
				return waypoint;
			}
		}
	}

	return null;
}


// CYCLE WAYPOINTS //
void CycleWaypoints(List<StarMap> maps, bool next)
{
	int gpsCount = _waypointList.Count;
	
	if(gpsCount < 1)
	{
		_statusMessage = "No Waypoints Logged!";
		return;
	}
	
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		DefaultView(map);
		
		if(next)
		{
			map.waypointIndex++;
		}
		else
		{
			map.waypointIndex--;
		}
		
		
		if(map.waypointIndex == -1){
			map.activeWaypoint = null;
			map.activeWaypointName = "";
			MapToParameters(map);
			return;
		}
		else if(map.waypointIndex < -1){
			map.waypointIndex = gpsCount - 1;
		}
		else if(map.waypointIndex >= gpsCount){
			map.waypointIndex = -1;
			map.activeWaypoint = null;
			map.activeWaypointName = "";
			MapToParameters(map);
			return;
		}
		
		Waypoint waypoint = _waypointList[map.waypointIndex];
		map.center = waypoint.position;
		map.activeWaypoint = waypoint;
		map.activeWaypointName = waypoint.name;
		MapToParameters(map);
	}
}


// SELECT PLANET //
void SelectPlanet(Planet planet, StarMap map)
{
	map.center = planet.position;
	map.activePlanetName = planet.name;
	
	if(planet.name != "" && planet.name != "[null]")
		map.activePlanet=GetPlanet(planet.name);

	
	if(planet.radius < 27000)
	{
		map.focalLength *= 4;
	}
	else if(planet.radius < 40000)
	{
		map.focalLength *= 3;
		map.focalLength /= 2;
	}
}


// DRAWING FUNCTIONS //////////////////////////////////////////////////////////////////////////////////////////////////////////

// DRAW SHIP //
public void DrawShip(StarMap map, List<Planet> displayPlanets)
{
	// SHIP COLORS
	Color bodyColor = new Color(SHIP_RED, SHIP_GREEN, SHIP_BLUE);
	Color aftColor = new Color(180,60,0);
	Color plumeColor = Color.Yellow;
	Color canopyColor = Color.DarkBlue;

	Vector3 transformedShip = transformVector(_myPos, map);
	Vector2 shipPos = PlotObject(transformedShip,map);
	float shipX = shipPos.X;
	float shipY = shipPos.Y;

	int vertMod = 0;
	if(map.showInfo)
	{
		vertMod = BAR_HEIGHT;

		if(map.viewport.Width > 500)
		{
			vertMod *= 2; 
		}
	}

	bool offZ = transformedShip.Z < map.focalLength;
	bool leftX = shipX < -map.viewport.Width/2 || (offZ && shipX < 0);
	bool rightX = shipX > map.viewport.Width/2 || (offZ && shipX >= 0);
	bool aboveY = shipY < -map.viewport.Height/2 + vertMod || (offZ && shipY < 0);
	bool belowY = shipY > map.viewport.Height/2 - vertMod || (offZ && shipX >= 0);
	bool offX = leftX || rightX;
	bool offY = aboveY || belowY;

	if(offZ || offX || offY)
	{
		float posX;
		float posY;
		float rotation = 0;
		int pointerScale = SHIP_SCALE/2;


		if(offZ)
		{
			bodyColor = Color.DarkRed;
			//shipX *= -1;
			//shipY *= -1;
		}
		else
		{
			bodyColor = Color.DodgerBlue;
		}

		if(leftX)
		{
			posX = 0;
			rotation = (float) Math.PI*3/2;
		}
		else if(rightX)
		{
			posX = map.viewport.Width - pointerScale;
			rotation = (float) Math.PI/2;
		}
		else
		{
			posX = map.viewport.Width/2 + shipX - pointerScale/2;
		}

		if(aboveY)
		{
			posY = vertMod + TOP_MARGIN	 + map.viewport.Center.Y - map.viewport.Height/2;
			rotation = 0;
		}
		else if(belowY)
		{
			posY = map.viewport.Center.Y + map.viewport.Height/2 - vertMod - TOP_MARGIN;
			rotation = (float) Math.PI;
		}
		else
		{
			posY = map.viewport.Height/2 + shipY + (map.viewport.Width - map.viewport.Height)/2;
		}

		if(offX && offY)
		{
			rotation = (float) Math.Atan2(shipY, shipX);
		}


		//OFF MAP POINTER
		DrawTexture("Triangle", new Vector2(posX - 2, posY), new Vector2(pointerScale + 4, pointerScale + 4), rotation, Color.Black);

		DrawTexture("Triangle", new Vector2(posX, posY), new Vector2(pointerScale, pointerScale), rotation, bodyColor);
	}
	else
	{
		Vector2 position = shipPos;

		// Get ships Direction Vector and align it with map
		Vector3 heading = rotateVector(_refBlock.WorldMatrix.Forward, map);

		if(displayPlanets.Count > 0)
		{
			String planetColor = obscureShip(position, displayPlanets, map);
			
			if(planetColor != "NONE"){
				bodyColor = ColorSwitch(planetColor, false) * 2 * _brightnessMod;
				aftColor = bodyColor * 0.75f;
				plumeColor = aftColor;
				canopyColor = aftColor;
			}
		}


		// Ship Heading
		float headingX = heading.X;
		float headingY = heading.Y;
		float headingZ = heading.Z;

		//Get the Ratio of direction Vector's apparent vs actual magnitudes.
		float shipLength = (float) 1.33 * SHIP_SCALE *(float) Math.Sqrt(headingX * headingX + headingY * headingY) / (float) Math.Sqrt (headingX * headingX + headingY * headingY + headingZ * headingZ);

		float shipAngle = (float) Math.Atan2(headingX, headingY) * -1; 
		
		position += map.viewport.Center;

		Vector2 offset = new Vector2( (float) Math.Sin(shipAngle), (float) Math.Cos(shipAngle) * -1);
		position += offset * shipLength/4;
		position -= new Vector2(SHIP_SCALE/2 ,0);
		Vector2 startPosition = position;

		position -= new Vector2(2,0);

		//Outline
		DrawTexture("Triangle", position, new Vector2(SHIP_SCALE+4, shipLength+4), shipAngle, Color.Black);

		float aftHeight = SHIP_SCALE - shipLength/(float) 1.33;

		position = startPosition;
		position -= offset * shipLength/2;
		position -= new Vector2(2,0);
		
		DrawTexture("Circle", position, new Vector2(SHIP_SCALE+4, aftHeight+4), shipAngle, Color.Black);
		
		position = startPosition;
		
		// Ship Body
		DrawTexture("Triangle", position, new Vector2(SHIP_SCALE, shipLength), shipAngle, bodyColor);

		if(headingZ < 0){
			position = startPosition;
			position -= offset * shipLength/2;
			
			DrawTexture("Circle", position, new Vector2(SHIP_SCALE, aftHeight), shipAngle, bodyColor);
		}

		// Canopy
		position = startPosition;
		position += offset * shipLength/8;
		position += new Vector2(SHIP_SCALE/4,0);
		DrawTexture("Triangle", position, new Vector2(SHIP_SCALE*0.5f, shipLength*0.5f), shipAngle, canopyColor);
		

		
		// Canopy Mask Variables
		Vector3 shipUp = rotateVector(_refBlock.WorldMatrix.Up, map);
		Vector3 shipRight = rotateVector(_refBlock.WorldMatrix.Right, map);
		
		float rollInput = (float) Math.Atan2(shipRight.Z, shipUp.Z) + (float) Math.PI;

		//Echo("Roll Angle: " + ToDegrees(rollInput) + "°");

		float rollAngle =  (float) Math.Cos(rollInput/2) * (float) Math.Atan2(SHIP_SCALE, 2* shipLength) *0.9f;//(float) Math.Atan2(shipLength, 2 * SHIP_SCALE) * (1 - (float) Math.Cos(rollInput))/2;//
		
		float rollScale = (float) Math.Sin(rollInput/2) * 0.7f * shipLength/SHIP_SCALE;// + (float) Math.Abs(headingZ)/10;
		Vector2 rollOffset = new Vector2 ((float) Math.Sin(shipAngle - rollAngle), (float) Math.Cos(shipAngle - rollAngle) *-1);
		
		float maskMod = 0.95f;
		if(rollInput < 0.35f || rollInput > 5.93f)
			maskMod = 0.6f;
			
		// Canopy Edge
		var edgeColor = bodyColor;
		int edgeMod = 1;
		
		if(headingZ < 0){
			edgeColor = canopyColor;
			edgeMod = -1;
		}
		
		position = startPosition;
		position -= offset * shipLength/8;
		position += new Vector2(SHIP_SCALE/4,0);
		DrawTexture("SemiCircle", position, new Vector2(SHIP_SCALE*0.5f, aftHeight*0.5f*edgeMod), shipAngle, edgeColor);
			
		// Canopy Mask
		position = startPosition;
		position += new Vector2((1- rollScale)*SHIP_SCALE/2, 0);
		position += offset * shipLength * 0.25f;
		position -= rollOffset * 0.7f * shipLength/3;
		
		DrawTexture("Triangle", position, new Vector2(SHIP_SCALE*rollScale, shipLength*maskMod), shipAngle - rollAngle, bodyColor);	

		//Aft Plume
		if(headingZ >= 0){
			position = startPosition;
			position -= offset * shipLength/2;
			
			DrawTexture("Circle", position, new Vector2(SHIP_SCALE, aftHeight), shipAngle, aftColor);
			
			position -= offset * shipLength/16;
			position += new Vector2(SHIP_SCALE/6, 0);
			
			DrawTexture("Circle", position, new Vector2(SHIP_SCALE*0.67f, aftHeight*0.67f), shipAngle, plumeColor);

			/* //CHASE ROLL INDICATOR
			if(map.mode == "CHASE"){
				float chaseAngle = (float) Math.PI - rollInput;
				position = startPosition + new Vector2(SHIP_SCALE/3, 0);
				position += new Vector2((float)Math.Sin(chaseAngle), (float) Math.Cos(chaseAngle) *-1)* SHIP_SCALE/6;
				DrawTexture("Triangle", position, new Vector2(SHIP_SCALE*0.33f, SHIP_SCALE*0.33f), chaseAngle, aftColor);
			}*/
		}
	}
}


// PLOT OBJECT //
public Vector2 PlotObject(Vector3 pos, StarMap map)
{
	float zFactor = map.focalLength/pos.Z;

	float plotX = pos.X*zFactor;
	float plotY = pos.Y*zFactor;

	Vector2 mapPos = new Vector2(-plotX, -plotY);
	return mapPos;
}


// DRAW PLANETS //
public void DrawPlanets(List<Planet> displayPlanets, StarMap map)
{
	PlanetSort(displayPlanets, map);

	string drawnPlanets = "Displayed Planets:";
	foreach(Planet planet in displayPlanets)
	{
		drawnPlanets += " " + planet.name + ",";

		Vector2 planetPosition = PlotObject(planet.transformedCoords[map.number], map);
		planet.mapPos = planetPosition;

		Color surfaceColor = ColorSwitch(planet.color, false) * _brightnessMod;
		Color lineColor = surfaceColor * 2;

		Vector2 startPosition = map.viewport.Center + planetPosition;

		float diameter = ProjectDiameter(planet, map);

		Vector2 position;
		Vector2 size;

		// Draw Gravity Well
		if(map.mode == "ORBIT" && planet == map.activePlanet)
		{
			float radMod = 0.83f;
			size = new Vector2(diameter*radMod*2, diameter*radMod*2);
			position = startPosition - new Vector2(diameter*radMod,0);
			
			DrawTexture("CircleHollow", position, size, 0, Color.Yellow);
			radMod *= 0.99f;
			position = startPosition - new Vector2(diameter*radMod, 0);
			size = new Vector2(diameter*radMod*2, diameter*radMod*2);

			DrawTexture("CircleHollow", position, size, 0, Color.Black);
		}

		// Planet Body
		position = startPosition - new Vector2(diameter/2,0);
		DrawTexture("Circle", position, new Vector2(diameter,diameter), 0, surfaceColor);
		  
		// Equator
		double radAngle = (float) map.altitude * Math.PI/180;
		float pitchMod = (float) Math.Sin(radAngle)*diameter;
		DrawTexture("CircleHollow", position, new Vector2(diameter,pitchMod), 0, lineColor);

		// Mask
		int scaleMod = -1;
		if(map.altitude < 0)
		{
			scaleMod *= -1;
		}
		DrawTexture("SemiCircle", position, new Vector2(diameter, diameter*scaleMod), 0, surfaceColor);

		// Border
		DrawTexture("CircleHollow", position, new Vector2(diameter, diameter), 0, lineColor);


		// HashMarks
		if(diameter > HASH_LIMIT && map.mode != "CHASE")// && Vector3.Distance(planet.position, map.center) < 2*planet.radius
		{
			DrawHashMarks(planet, diameter, lineColor, map);
		}

		if(map.showNames)
		{
			// PLANET NAME
			float fontMod = 1;

			if(diameter < 50)
			{
				fontMod =(float)0.5;
			}

			// Name Shadow
			position = startPosition;
			DrawText(planet.name, position, fontMod*0.8f, TextAlignment.CENTER, Color.Black);

			// Name
			position += new Vector2(-2,2);
			DrawText(planet.name, position, fontMod*0.8f, TextAlignment.CENTER, Color.Yellow*_brightnessMod);
		}
	}
	
	Echo(drawnPlanets.Trim(',') + "\n");
}


// DRAW TEXTURE //
public void DrawTexture(string shape, Vector2 position, Vector2 size, float rotation, Color color)
{
	var sprite = new MySprite()
	{
		Type = SpriteType.TEXTURE,
		Data = shape,
		Position = position,
		Size = size,
		RotationOrScale=rotation,
		Color = color
	};
	_frame.Add(sprite);
}

// DRAW TEXT //
void DrawText(string text, Vector2 position, float scale, TextAlignment alignment, Color color)
{
	var sprite = new MySprite()
	{
		Type = SpriteType.TEXT,
		Data = text,
		Position = position,
		RotationOrScale = scale,
		Color = color,
		Alignment = alignment,
		FontId = "White"
	};
	_frame.Add(sprite);
}


// DRAW HASHMARKS //   Makes series of low-profile waypoints to denote latitude and longitude on planets.
public void DrawHashMarks(Planet planet, float diameter, Color lineColor, StarMap map)
{
	List<Waypoint> hashMarks = new List<Waypoint>();
	
	float planetDepth = planet.transformedCoords[map.number].Z;

	//North Pole
	Waypoint north = new Waypoint();
	north.name = "N -";
	north.position = planet.position + new Vector3(0, (float) planet.radius, 0);
	north.transformedCoords.Add(transformVector(north.position, map));
	if(north.transformedCoords[0].Z < planetDepth)
	{
		hashMarks.Add(north);
	}

	//South Pole
	Waypoint south = new Waypoint();
	south.name = "S -";
	south.position = planet.position - new Vector3(0, (float) planet.radius, 0);
	south.transformedCoords.Add(transformVector(south.position, map));
	if(south.transformedCoords[0].Z < planetDepth)
	{
		hashMarks.Add(south);
	}

	float r1 = planet.radius * 0.95f;
	float r2 = (float) Math.Sqrt(2)/2*r1;

	float r3 = r1/2;
	
	String[] latitudes = new String[]{"+", "|", "+"};
	String[] longitudes = new String[]{"135°E","90°E","45°E","0°","45°W","90°W","135°W","180°"};

	float[] yCoords = new float[] {-r2,0,r2};
	float[,] xCoords = new float[,]{{-r3,-r2,-r3,  0, r3,r2,r3, 0},{-r2,-r1,-r2, 0, r2,r1,r2,0},{-r3,-r2,-r3,  0, r3,r2,r3, 0}};
	float[,] zCoords = new float[,]{{ r3,  0,-r3,-r2,-r3, 0,r3,r2},{ r2, 0,-r2,-r1,-r2,0,r2,r1},{ r3,  0,-r3,-r2,-r3, 0,r3,r2}};

	for(int m = 0; m < 3; m++)
	{
		String latitude = latitudes[m];
		float yCoord = yCoords[m];
		for(int n = 0; n < 8; n++)
		{
			Waypoint hashMark = new Waypoint();
			hashMark.name = latitude + " " +longitudes[n]; 

			float xCoord = xCoords[m,n];
			float zCoord = zCoords[m,n];

			hashMark.position = planet.position + new Vector3(xCoord, yCoord, zCoord);
			hashMark.transformedCoords.Add(transformVector(hashMark.position, map));

			if(hashMark.transformedCoords[0].Z < planetDepth)
			{
				hashMarks.Add(hashMark);
			}
		}
	}

	foreach(Waypoint hash in hashMarks)
	{
		Vector2 position = map.viewport.Center + PlotObject(hash.transformedCoords[0], map);		

		// Print more detail for closer planets
		if(diameter > 2 * HASH_LIMIT)
		{

			String[] hashLabels = hash.name.Split(' ');
			float textMod = 1;
			int pitchMod = 1;
			if(map.altitude > 0)
			{
				pitchMod = -1;
			}

			if(diameter > 3 * HASH_LIMIT)
			{
				textMod = 1.5f;
			}

			Vector2 hashOffset = new Vector2(0,10*textMod*pitchMod);
			position -= hashOffset;

			DrawText(hashLabels[0], position, 0.5f * textMod, TextAlignment.CENTER, lineColor);

			position += hashOffset; 

			DrawText(hashLabels[1], position, 0.4f * textMod, TextAlignment.CENTER, lineColor);		   
		}
		else
		{
			position += new Vector2(-2,2);
			
			DrawTexture("Circle", position, new Vector2(4,4), 0, lineColor);
		}
	}
}


// DRAW WAYPOINTS //
public void DrawWaypoints(StarMap map)
{
	float fontSize = 0.5f;
	float markerSize = MARKER_WIDTH;
	
	// focal radius for modifying waypoint scale
	int focalRadius = map.rotationalRadius - map.focalLength;
	
	if(map.viewport.Width > 500)
	{
		fontSize *= 1.5f;
		markerSize *= 2;
	}
	foreach(Waypoint waypoint in _waypointList)
	{
		if(waypoint.isActive)
		{
			float rotationMod = 0;
			Color markerColor = ColorSwitch(waypoint.color, true);
			
			float coordZ = waypoint.transformedCoords[map.number].Z;
			float gpsScale = 1;
			
			bool activePoint = (map.activeWaypointName != "") && (waypoint.name == map.activeWaypointName);

			
			if(map.gpsState == 1 && !activePoint)
				gpsScale = FOCAL_MOD * map.focalLength /coordZ;//coordZ / (-2 * focalRadius) + 1.5f;
			
			
				
			float iconSize = markerSize * gpsScale;
			
			Vector2 markerScale = new Vector2(iconSize,iconSize);

			Vector2 waypointPosition = PlotObject(waypoint.transformedCoords[map.number], map);
			Vector2 startPosition = map.viewport.Center + waypointPosition;

			String markerShape = "";
			switch(waypoint.marker.ToUpper())
			{
				case "STATION":
					markerShape = "CircleHollow";
					break;
				case "BASE":
					markerShape = "SemiCircle";
					markerScale *= 1.25f;
					//startPosition += new Vector2(0,iconSize);
					break;
				case "LANDMARK":
					markerShape = "Triangle";
					markerColor = new Color(48,48,48);
					break;
				case "HAZARD":
					markerShape = "SquareTapered";
					markerColor = Color.Red;
					rotationMod = (float)Math.PI/4;
					break;
				case "ASTEROID":
					markerShape = "SquareTapered";
					markerColor = new Color(48,32,32);
					markerScale *= 0.9f;
					rotationMod = (float)Math.PI/4;
					break;
				default:
					markerShape = "SquareHollow";
					break;
			}
			

			
			if(coordZ > map.focalLength)
			{
				
				Vector2 position = startPosition - new Vector2(iconSize/2,0);

				markerColor *= _brightnessMod;

				// PRINT MARKER
				
				// Shadow
				DrawTexture(markerShape, position, markerScale, rotationMod, Color.Black);

				// Marker
				position += new Vector2(1,0);
				DrawTexture(markerShape, position, markerScale*1.2f, rotationMod, markerColor);

				position += new Vector2(1,0);
				DrawTexture(markerShape, position, markerScale, rotationMod, markerColor);

				// Draw secondary features for special markers
				switch(waypoint.marker.ToUpper()){
					case "STATION":
						position += new Vector2(iconSize/2 - iconSize/20, 0);
						DrawTexture("SquareSimple", position, new Vector2(iconSize/10 ,iconSize), rotationMod, markerColor);
						break;
					case "HAZARD":
						position += new Vector2(iconSize/2 - iconSize/20, -iconSize*0.85f);
						DrawText("!", position, fontSize*1.2f*gpsScale, TextAlignment.CENTER, Color.White);
						break;
					case "BASE":
						position += new Vector2(iconSize/6, -iconSize/12);
						DrawTexture("SemiCircle", position, new Vector2(iconSize*1.15f,iconSize*1.15f), rotationMod, new Color(0,64,64)*_brightnessMod);
						startPosition -= new Vector2(0,iconSize * 0.4f);
						break;
					case "ASTEROID":
						position += new Vector2(iconSize/2 - iconSize/20, 0);
						DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32,32,32)*_brightnessMod);
						position -= new Vector2(iconSize - iconSize/10,0);
						DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32,32,32)*_brightnessMod);
						break;
					default:
						break;
				}
				
				if(activePoint){
					position = startPosition - new Vector2(0.9f * markerSize, 0.33f * markerSize);
					DrawText("|________", position, fontSize, TextAlignment.LEFT, Color.White);
				}

				// PRINT NAME
				if(map.showNames){					
					position = startPosition + new Vector2(1.33f * iconSize,-0.75f * iconSize);
					DrawText(waypoint.name, position, fontSize * gpsScale, TextAlignment.LEFT, markerColor*_brightnessMod);
				}
			}
		}
	}
}


// DRAW SURFACE POINTS //
public void DrawSurfacePoint(Vector3 surfacePoint, int pointNumber, StarMap map)
{
	Color pointColor;
	switch(pointNumber)
	{
		case 1:
			pointColor = new Color(32,0,48);
			break;
		case 2:
			pointColor = new Color(64,0,96);
			break;
		case 3:
			pointColor = new Color(128,0,192);
			break;
		default:
			pointColor = Color.Red;
			break;
	}

	Vector3 pointTransformed = transformVector(surfacePoint, map);
	if(pointTransformed.Z > map.focalLength)
	{
		float markerScale = MARKER_WIDTH * 2;
		float textSize = 0.6f;

		if(map.viewport.Width > 500)
		{
			markerScale *= 1.5f;
			textSize *= 1.5f;
		}

		Vector2 startPosition = map.viewport.Center + PlotObject(pointTransformed, map);
		Vector2 position =	startPosition + new Vector2(-markerScale/2,0);

		Vector2 markerSize = new Vector2(markerScale, markerScale);
		
		position += new Vector2(-markerScale * 0.025f, markerScale *0.05f);

		DrawTexture("Circle", position, markerSize*1.1f, 0, Color.Black);

		DrawTexture("CircleHollow", position, markerSize, 0, pointColor);

		position = startPosition - new Vector2(0, markerScale/2);

		DrawText("?", position, textSize, TextAlignment.CENTER, pointColor);	
	}
}


// PLOT UNCHARTED //
public void PlotUncharted(StarMap map)
{
	if(_unchartedList.Count > 0)
	{
		foreach(Planet planet in _unchartedList)
		{
			String[] planetData = planet.ToString().Split(';');

			for(int p = 4; p < 8; p++)
			if(planetData[p] != "")
			{
				int pointNumber = p-3;
				DrawSurfacePoint(planet.GetPoint(pointNumber),pointNumber, map);
			}
		}
	}
}


// PROJECT DIAMETER //
float ProjectDiameter(Planet planet, StarMap map)
{
	float viewAngle = (float) Math.Asin(planet.radius/planet.transformedCoords[map.number].Z);

	float diameter = (float) Math.Tan(Math.Abs(viewAngle)) * 2 * map.focalLength;

	if(diameter < DIAMETER_MIN)
	{
		diameter = DIAMETER_MIN;
	}

	return diameter;
}


// OBSCURE SHIP //
public String obscureShip(Vector2 shipPos, List<Planet> planets, StarMap map)
{
	//Get Nearest Planet on Screen
	Planet closest = planets[0];
	foreach(Planet planet in planets)
	{
		if(Vector2.Distance(shipPos, planet.mapPos) < Vector2.Distance(shipPos, closest.mapPos))
		{
			closest = planet;
		}
	}

	String color = "NONE";
	float distance = Vector2.Distance(shipPos, closest.mapPos);
	float radius = 0.95f * closest.radius*map.focalLength/closest.transformedCoords[map.number].Z;

	if(distance < radius && closest.transformedCoords[map.number].Z < transformVector(_myPos, map).Z)
	{
		color = closest.color;
	}

	return color;
}


// DATA DISPLAY FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////

// DISPLAY DATA // Page selection interface for Data Display
void DisplayData()
{
	if(_dataSurface == null)
		return;
	
	switch(_pageIndex)
	{
		case 0:
			DisplayGPSInput();
			break;
		case 1:
			DisplayPlanetData();
			break;
		case 2:
			DisplayWaypointData();
			break;
		case 3:
			DisplayMapData();
			break;
		case 4:
			DisplayClipboard();
			break;
	}
}


// DISPLAY GPS INPUT //
void DisplayGPSInput(){
	if(_cycleStep > 0 || _dataSurface == null)
		return;
	
	StringBuilder menuText = new StringBuilder();
	_dataSurface.ReadText(menuText, false);
	
	string output = menuText.ToString();

	if(!output.StartsWith(GPS_INPUT))
		output = GPS_INPUT + SLASHES + "\n~Input Terminal Coords Below This Line~";
	
	_dataSurface.WriteText(output);
}


// DISPLAY PLANET DATA //
void DisplayPlanetData()
{
	string output = "// PLANET LIST" + SLASHES;
	List<string> planetData = new List<string>();
	planetData.Add("Charted: " + _planetList.Count + "	  Uncharted: " + _unchartedList.Count);

	if(_planets)
	{
		foreach(Planet planet in _planetList)
		{
			float surfaceDistance = (Vector3.Distance(planet.position, _myPos) - planet.radius)/1000;
			if(surfaceDistance < 0)
			{
				surfaceDistance = 0;
			}
			string planetEntry;
			if(planet.name.ToUpper() == _activePlanet.ToUpper())
			{
				planetEntry = ">>> ";
			}
			else
			{
				planetEntry = "		   ";
			}

			planetEntry += planet.name + "	  R: " + abbreviateValue(planet.radius) + "m    dist: " + surfaceDistance.ToString("N1") + "km";

			planetData.Add(planetEntry);
		}
	}

	if(_unchartedList.Count > 0)
	{
		string unchartedHeader = "\n----Uncharted Planets----";
		planetData.Add(unchartedHeader);
		foreach(Planet uncharted in _unchartedList)
		{
			float unchartedDistance = Vector3.Distance(_myPos, uncharted.GetPoint(1))/1000;

			string unchartedEntry = "  " + uncharted.name + "	 dist: " + unchartedDistance.ToString("N1") + "km";

			planetData.Add(unchartedEntry);
		}
	}

	output += ScrollToString(planetData);

	_dataSurface.WriteText(output);
}


// DISPLAY WAYPOINT DATA //
void DisplayWaypointData()
{
	string output = "// WAYPOINT LIST" + SLASHES;

	List<string> waypointData = new List<string>();
	waypointData.Add("Count: " + _waypointList.Count);

	if(_waypointList.Count > 0)
	{
		foreach(Waypoint waypoint in _waypointList)
		{
			float distance = Vector3.Distance(_myPos, waypoint.position)/1000;
			string status = "Active";
			if(!waypoint.isActive)
			{
				status = "Inactive";
			}
			string waypointEntry = "		";
			if(waypoint.name.ToUpper()==_activeWaypoint.ToUpper())
			{
				waypointEntry = ">>> ";
			}
			waypointEntry += waypoint.name + "	  " + waypoint.marker  + "	  "+status + "	  dist: " + distance.ToString("N1") + "km";
			waypointData.Add(waypointEntry);
		}
	}

	output += ScrollToString(waypointData);

	_dataSurface.WriteText(output);
}


// DISPLAY MAP DATA //
void DisplayMapData()
{
	string output = "// MAP DATA" + SLASHES;

	if(_statusMessage != "")
		output += "\n" + _statusMessage;

	if(_mapBlocks.Count > 0)
	{
		List<string> mapData = new List<string>();
		foreach(StarMap map in _mapList)
		{
			mapData.Add("MAP " + map.number + " --- "  + map.viewport.Width.ToString("N0") + " x " + map.viewport.Height.ToString("N0") + " --- "+ map.mode + " Mode");
			mapData.Add("   On: " + map.block.CustomName + "   Screen: " + map.index);
			
			if(map.activePlanetName != "")
				mapData.Add("   Selected Planet: " + map.activePlanetName);

			string hidden = "   Hidden:";
			if(!map.showInfo)
				hidden += " Stat-Bars ";

			if(map.gpsState == 0)
				hidden += " GPS ";

			if(!map.showNames)
				hidden += " Names ";

			if(!map.showShip)
				hidden += " Ship";

			if(map.gpsState == 0 || !map.showInfo || !map.showShip && !map.showNames)
				mapData.Add(hidden);

			mapData.Add("   Center: " + Vector3ToString(map.center));
			mapData.Add("   Azimuth: " + map.azimuth + "°   Altitude: " + map.altitude * -1 + "°");
			mapData.Add("   Focal Length: " + abbreviateValue(map.focalLength) + "   Radius: " + abbreviateValue(map.rotationalRadius) +"\n");
		}

		output += ScrollToString(mapData);
	}
	else
	{
		output += "\n\n NO MAP BLOCKS TO DISPLAY!!!";
	}

	_dataSurface.WriteText(output);
}


// DISPLAY CLIPBOARD //
void DisplayClipboard()
{
	string output = "// CLIPBOARD" + SLASHES + "\n\n" + _clipboard;
	_dataSurface.WriteText(output);
}


// SCROLL TO STRING // Returns string from List based on ScrollIndex
string ScrollToString(List<string> dataList)
{
	string output = "";

	if(dataList.Count > 0 && _scrollIndex < dataList.Count)
	{
		for(int d = _scrollIndex; d < dataList.Count; d++)
		{
			output += "\n" + dataList[d];
		}
	}

	return output;
}


// NEXT PAGE //
void NextPage(bool next)
{
	if(next){
		_pageIndex++;
		if(_pageIndex >= DATA_PAGES)
			_pageIndex = 0;
		return;
	}
	
	if(_pageIndex == 0)
		_pageIndex = DATA_PAGES;
	
	_pageIndex--;
}


// LOG BATCH //  Logs multiple pasted terminal coordinates.
void LogBatch(){
	if(_dataSurface == null){
		_statusMessage = "No DATA DISPLAY Screen Designated!";
		return;
	}
	
	if(_pageIndex != 0){
		_statusMessage = "Please navigate to GPS INPUT page before running LOG_BATCH command.";
	}
	
	StringBuilder inputText = new StringBuilder();
	_dataSurface.ReadText(inputText, false);
	string[] inputs = inputText.ToString().Split('\n');
	
	List<string> outputs = new List<string>();
	
	foreach(string entry in inputs){
		if(entry.StartsWith("GPS:")){
			ClipboardToLog("WAYPOINT", entry);
		}else{
			outputs.Add(entry);
		}
	}
	
	string output = "";
	foreach(string item in outputs){
		output += item + "\n";
	}
	
	_dataSurface.WriteText(output);
}


// SYNC // Switch function for all sync commands
void sync(string cmdArg, string argData){
	bool syncTo = argData == "OVERWRITE";

	if(cmdArg == "MASTER"){
		syncMaster(syncTo);
	}else if(cmdArg == "NEAREST"){
		syncNearest(syncTo);
	}else{
		_statusMessage = "Invalid Sync Command!";
	}
}


// SYNC MASTER // Finds Master Sync Block on station and syncs to or from depending on bool.
void syncMaster(bool syncTo){

	if(Me.CustomName.Contains(SYNC_TAG)){
		_statusMessage = "SYNC Requests cannot be made from SYNC terminal!\n";
		return;
	}
	
	List<IMyTerminalBlock> syncBlocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(SYNC_TAG, syncBlocks);
	
	if(syncBlocks.Count < 1){
		_statusMessage = "NO MAP MASTER FOUND.\nPlease add tag '" + SYNC_TAG + "' to the map computer's name on your station or capital ship.\n";
		return;
	}
	
	if(syncBlocks.Count > 1){
		_statusMessage = "Multiple blocks found with tag '" + SYNC_TAG + "'! Please resolve conflict before syncing.\n";
		return;
	}
	
	syncWith(syncBlocks[0] as IMyProgrammableBlock, syncTo);
}


// SYNC NEAREST // Finds Nearest other mapping program and syncs to or from depending on bool.
void syncNearest(bool syncTo){
	List<IMyProgrammableBlock> computers = new List<IMyProgrammableBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(computers);
	
	IMyProgrammableBlock syncBlock = null;
	float nearest = float.MaxValue;
	
	foreach(IMyProgrammableBlock computer in computers){
		if(computer.CustomData.Contains("[Map Settings]") && !(computer == Me)){
			float distance = Vector3.Distance(_myPos, computer.GetPosition());
			if(distance < nearest){
				nearest = distance;
				syncBlock = computer;
			}
		}
	}
	
	if(!(syncBlock == null)){
		syncWith(syncBlock, syncTo);
		return;
	}
	
	_statusMessage = "No other mapping computers available to sync!";
}


// SYNC WITH // Sync map data with master sync computer.
void syncWith(IMyProgrammableBlock syncBlock, bool syncTo){

	IMyTerminalBlock blockA = syncBlock as IMyTerminalBlock;
	IMyTerminalBlock blockB = Me;
	
	if(syncTo){
		blockA = Me;
		blockB = syncBlock as IMyTerminalBlock;
	}
	
	int[] pSync = mapSync(blockA, blockB, "Planet_List");
	int[] wSync = mapSync(blockA, blockB, "Waypoint_List");
	
	if(syncTo){
		pSync = syncReverse(pSync);
		wSync = syncReverse(wSync);
	}
	
	syncBlock.TryRun("SYNC_ALERT " + Me.CustomName);
	refresh();
	
	_statusMessage = "MAP DATA SYNCED\n-- Planets --\nDownloaded: " + pSync[0] + "\nUploaded: " + pSync[1] + "\n\n--Waypoints--\nDownloaded: " + wSync[0] + "\nUploaded: " + wSync[1] + "\n";
}


// SYNC REVERSE // reverses 2 entry int array
int[] syncReverse(int[] input){
	int[] output = new int[] {input[1], input[0]};
	return output;
}


// MAP SYNC //  Syncs Data for specific ini entry between two blocks, int array [from, to] mapA.
int[] mapSync(IMyTerminalBlock mapA, IMyTerminalBlock mapB, string listName){
	int[] downUp = new int[] {0,0};
	
	if(syncBlockError(mapA))
		return downUp;
	if(syncBlockError(mapB))
		return downUp;
	
	MyIni iniA = DataToIni(mapA);
	MyIni iniB = DataToIni(mapB);
	
	string dataA = iniA.Get("Map Settings", listName).ToString();
	string dataB = iniB.Get("Map Settings", listName).ToString();
	string newData = "";
	
	if(dataA == ""){
		newData = dataB;
	}else if(dataB == ""){
		newData = dataA;
	}else{
		List<string> outputs = dataA.Split('\n').ToList();
		List<string> inputs = dataB.Split('\n').ToList();
		
		int startCount = outputs.Count;
		int matchCount = 0;
		
		foreach(string input in inputs){
			string name = input.Split(';')[0];
			bool matched = false;
			
			foreach(string output in outputs){
				if(output.StartsWith(name)){
					matched = true;
				}
			}

			if(matched)
				matchCount++;
			else
				outputs.Add(input);
		}
		
		downUp[0] = startCount - matchCount;
		downUp[1] = inputs.Count - matchCount;
		
		foreach(string entry in outputs){
			newData += entry + "\n";	
		}
	}
	
	iniA.Set("Map Settings", listName, newData.Trim());
	mapA.CustomData = iniA.ToString();
	
	iniB.Set("Map Settings", listName, newData.Trim());
	mapB.CustomData = iniB.ToString();
	
	return downUp;
}


// SYNC BLOCK ERROR // Returns false and sets error message if Sync Block has no Map Data
bool syncBlockError(IMyTerminalBlock sync){
	if(sync.CustomData.Contains("[Map Settings]")){
		return false;
	}
	
	_statusMessage = "SYNC Block '" + sync.CustomName +"' contains no map settings! Please ensure that SYNC Block is also running this script!\n";
	return true;
}


// SYNC ALERT // Refreshes then sets status to alert message.
void syncAlert(string name){
	refresh();
	string senderData;
	IMyTerminalBlock sender;
	
	try{
		sender = GridTerminalSystem.GetBlockWithName(name);
		senderData = getIni(sender, "Map Settings", "Grid_ID", sender.CubeGrid.EntityId.ToString());
	}catch{
		senderData = "UNKNOWN";
	}
	_statusMessage = "Origin Grid ID: " + senderData + "\n";
}


// PAGE SCROLL //
void pageScroll(string arg){
	switch(arg){
		case "UP":
			_scrollIndex--;
			if(_scrollIndex < 0)
				_scrollIndex = 0;								
			break;
		case "DOWN":
			_scrollIndex++;
			break;
		case "HOME":
			_scrollIndex = 0;	
			break;
		default:
			_statusMessage = "INVALID SCROLL COMMAND";
			break;
	}
}


// NEXT LAST //
void nextLast(List<StarMap> maps, string arg, bool state){
	switch(arg){
		case "PLANET":
			CyclePlanets(maps, state);
			break;
		case "WAYPOINT":
			CycleWaypoints(maps, state);
			break;
		case "MODE":
			CycleMode(maps, state);
			break;
		case "PAGE":
			NextPage(state);
			break;
	}
}


// BRIDGE FUNCTIONS // Ensure that commands from old switch are backwards compatible /////////////////////////////////////////////

// WAYPOINT COMMAND // Bridge function to eliminate old switch cases.
void waypointCommand(string arg, string waypointName){
	int state = 0;
	if(arg == "ON")
		state = 1;
	
	SetWaypointState(waypointName, state);	
}


// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


// UPDATE TAGS // Add Grid Tag to all map displays on grid
void updateTags(){
	_gridTag = Me.CubeGrid.EntityId.ToString();
	setIni(Me, "Map Settings", "Grid_ID", _gridTag);
	
	if(_mapBlocks.Count < 1)
		return;
	
	foreach(IMyTerminalBlock mapBlock in _mapBlocks){
		if(mapBlock.IsSameConstructAs(Me))
			setIni(mapBlock, "mapDisplay", "Grid_ID", _gridTag);
	}
	
	refresh();
}


// ON GRID // Checks if map blocks are part of current ship, even if merged
bool onGrid(IMyTerminalBlock mapTerminal){
	if(!mapTerminal.IsSameConstructAs(Me))
		return false;
	
	string iniGrid = getIni(mapTerminal, "mapDisplay", "Grid_ID", "Unassigned");
	
	if(iniGrid == _gridTag)
		return true;
	else
		return false;
}

// DATA TO INI //
MyIni DataToIni(IMyTerminalBlock block){
	MyIni iniOuti = new MyIni();
	
	MyIniParseResult result;
	if (!iniOuti.TryParse(block.CustomData, out result)) 
		throw new Exception(result.ToString());
	
	return iniOuti;
}

// COLOR SWITCH //
Color ColorSwitch(string colorString, bool isWaypoint){
	colorString = colorString.ToUpper();
	
	if(colorString.StartsWith("#"))
		return HexToColor(colorString);
	
	Color colorOut = new Color(8,8,8);
	if(isWaypoint)
		colorOut = Color.White;
		
	switch(colorString){
			case "RED":
				colorOut = new Color(32,0,0);
				break;
			case "GREEN":
				colorOut = new Color(0,32,0);
				break;
			case "BLUE":
				colorOut = new Color(0,0,32);
				break;
			case "YELLOW":
				colorOut = new Color(127,127,26);
				break;
			case "MAGENTA":
				colorOut = new Color(64,0,64);
				break;
			case "PURPLE":
				colorOut = new Color(24,0,48);
				break;
			case "CYAN":
				colorOut = new Color(0,32,32);
				break;
			case "LIGHTBLUE":
				colorOut = new Color(32,32,96);
				break;
			case "ORANGE":
				colorOut = new Color(32,16,0);
				break;
			case "TAN":
				colorOut = new Color(153,100,48);
				break;
			case "BROWN":
				colorOut = new Color(38,25,12);
				break;
			case "RUST":
				colorOut = new Color(64,20,16);
				break;
			case "GRAY":
				colorOut = new Color(16,16,16);
				break;
			case "GREY":
				colorOut = new Color(16,16,16);
				break;
			case "WHITE":
				colorOut = new Color(64,64,64);
				if(isWaypoint)
					colorOut = Color.White;
				break;
			default:
				colorOut = new Color(8,8,8);
				break;
	}
	
	return colorOut;
}

// HEX TO COLOR //
Color HexToColor(string hexString){
	
	if(hexString.Length != 9 && hexString.Length != 7)
		return Color.White;
	int i = 3; // Starting index for ARGB format.
	
	if(hexString.Length == 7)
			i = 1; // Starting index for normal HEX.
	
	int r,g,b = 0;

	r = Convert.ToUInt16(hexString.Substring(i, 2),16);
	g = Convert.ToUInt16(hexString.Substring(i+2, 2),16);
	b = Convert.ToUInt16(hexString.Substring(i+4, 2),16); 

	return new Color(r, g, b);
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


// SET STATE // Sets specified boolean to true, false, or toggle
bool setState(bool attribute, int state)
{
	switch(state)
	{
		case 0:
			attribute = false;
			break;
		case 1:
			attribute = true;
			break;
		case 3:
			attribute = !attribute;
			break;
	}
	
	return attribute;
}


// ARG TO MAPS //
public List<StarMap> ArgToMaps(string arg)
{
	if(arg.ToUpper() == "ALL")
	{
		return _mapList;
	}
	
	List<StarMap> mapsToEdit = new List<StarMap>();
	
	string[] args = arg.Split(',');
	
	foreach(string argValue in args)
	{
		int number;
		
		bool success = Int32.TryParse(argValue, out number);
		if(success)
		{
			if(number < _mapList.Count)
			{
				mapsToEdit.Add(_mapList[number]);
			}
			else
			{
				_statusMessage = "screenID " + argValue + " outside range of Map List!";
			}
		}
	}
	
	return mapsToEdit;
}


public bool NoMaps(List<StarMap> maps)
{
	if(maps.Count < 1)
	{
		_statusMessage = "No relevant maps found! Check arguments!";
		return true;
	}
	
	return false;
}


// INSERT ENTRY //
public string InsertEntry(string entry, string oldString, char separator, int index, int length, string placeHolder)
{
	string newString;
	
	List<string> entries = StringToEntries(oldString, separator, length, placeHolder);
	
	// If there's only one entry in the string return entry.
	if(entries.Count == 1 && length == 0)
	{
		return entry;
	}
	
	//Insert entry into the old string.
	entries[index] = entry;
	
	newString = entries[0];
	for(int n = 1; n < entries.Count; n++)
	{
		newString += separator + entries[n];
	}
	
	return newString;	
}


// STRING TO ENTRIES //		Splits string into a list of variable length, by a separator character.  If the list is shorter than 
					//		the desired length,the remainder is filled with copies of the place holder.
public List<string> StringToEntries(string arg, char separator, int length, string placeHolder)
{
	List<string> entries = new List<string>();
	string[] args = arg.Split(separator);
	
	foreach(string argument in args)
	{
		entries.Add(argument);
	}
	
	while(entries.Count < length)
	{
		entries.Add(placeHolder);
	}
	
	return entries;
}


// STRING TO WAYPOINT //
public Waypoint StringToWaypoint(String argument)
{
	Waypoint waypoint = new Waypoint();
	String[] wayPointData = argument.Split(';');
	if(wayPointData.Length > 3)
	{
		waypoint.name = wayPointData[0];
		waypoint.position = StringToVector3(wayPointData[1]);
		waypoint.marker = wayPointData[2];	
		waypoint.isActive = wayPointData[3].ToUpper() == "ACTIVE";
	}
	
	if(wayPointData.Length < 5){
		waypoint.color = "WHITE";
	} else {
		waypoint.color = wayPointData[4];
	}	
	return waypoint;
}


// WAYPOINT TO STRING //
public String WaypointToString(Waypoint waypoint)
{
	String output = waypoint.name + ";" + Vector3ToString(waypoint.position) + ";" + waypoint.marker;

	String activity = "INACTIVE";
	if(waypoint.isActive)
	{
		activity = "ACTIVE";
	}
	output += ";" + activity + ";" + waypoint.color;

	return output;		  
}


// VECTOR3 TO STRING //
public static string Vector3ToString(Vector3 vec3)
{
	String newData = "(" + vec3.X+"," + vec3.Y + "," + vec3.Z + ")";
	return newData;
}


// STRING TO VECTOR3 //
public static Vector3 StringToVector3(string sVector)
{
	// Remove the parentheses
	if (sVector.StartsWith ("(") && sVector.EndsWith (")"))
	{
		sVector = sVector.Substring(1, sVector.Length-2);
	}

	// split the items
	string[] sArray = sVector.Split(',');

	// store as a Vector3
	Vector3 result = new Vector3(
		float.Parse(sArray[0]),
		float.Parse(sArray[1]),
		float.Parse(sArray[2]));

	return result;
}


// ABBREVIATE VALUE //	 Abbreviates float value to k/M/G notation (i.e. 1000 = 1k). Returns string.
public string abbreviateValue(float valueIn)
{
	string abbreviatedValue;
	if(valueIn <= -1000000000 || valueIn >= 1000000000)
	{
		valueIn = valueIn/1000000000;
		abbreviatedValue = valueIn.ToString("0.0")+"G";
	}
	else if(valueIn <= -1000000 || valueIn >= 1000000)
	{
		valueIn = valueIn/1000000;
		abbreviatedValue = valueIn.ToString("0.0")+"M";
	}
	else if(valueIn <= -1000 || valueIn >= 1000)
	{
		valueIn = valueIn/1000;
		abbreviatedValue = valueIn.ToString("0.0")+"k";
	}
	else
	{	
		abbreviatedValue = valueIn.ToString("F0");
	}
	return abbreviatedValue;
}


// REPLACE COLUMN //
static void ReplaceColumn(double[,] matrix1, double[,] matrix2, double[] t, int column)
{
	for (int i = 0; i<4; i++)
	{
		for(int j = 0; j<4; j++)
		{
			matrix2[i,j] = matrix1[i,j];
		}
	}
	matrix2[0,column] = t[0];
	matrix2[1,column] = t[1];
	matrix2[2,column] = t[2];
	matrix2[3,column] = t[3];
}


// T-VALUE //
public static double TValue(Vector3 vec3)
{
	double result;
	double x = vec3.X;
	double y = vec3.Y;
	double z = vec3.Z;

	result = -1*(x*x + y*y + z*z);
	return result;
}


// Det4 // Gets determinant of a 4x4 Matrix
public static double Det4(double[,] q)
{
	double a = q[0,0];
	double b = q[0,1];
	double c = q[0,2];
	double d = q[0,3];
	double e = q[1,0];
	double f = q[1,1];
	double g = q[1,2];
	double h = q[1,3];
	double i = q[2,0];
	double j = q[2,1];
	double k = q[2,2];
	double l = q[2,3];
	double m = q[3,0];
	double n = q[3,1];
	double o = q[3,2];
	double p = q[3,3];

	double determinant = a*(f*(k*p-l*o)-g*(j*p-l*n)+h*(j*o-k*n))
						-b*(e*(k*p-l*o)-g*(i*p-l*m)+h*(i*o-k*m))
						+c*(e*(j*p-l*n)-f*(i*p-l*m)+h*(i*n-j*m))
						-d*(e*(j*o-k*n)-f*(i*o-k*m)+g*(i*n-j*m));


	return determinant;
}


// PLANET SORT //  Sorts Planets List by Transformed Z-Coordinate from largest to smallest. For the purpose of sprite printing.
public void PlanetSort(List<Planet> planets, StarMap map)
{
	int length = planets.Count;

	for(int i = 0; i < planets.Count - 1; i++)
	{
		for(int p = 1; p < length; p++)
		{
			Planet planetA = planets[p-1];
			Planet planetB = planets[p];

			if(planetA.transformedCoords[map.number].Z < planetB.transformedCoords[map.number].Z)
			{
				planets[p-1] = planetB;
				planets[p] = planetA;
			}
		}

		length--;
		if (length < 2)
		{
			return;
		}
	}
}


// SORT BY NEAREST //	 Sorts Planets by nearest to farthest.
public void SortByNearest(List<Planet> planets)
{
	int length = planets.Count;
	if(length > 1)
	{
		for(int i = 0; i < length - 1; i++)
		{
			for(int p = 1; p < length; p++)
			{
				Planet planetA = planets[p-1];
				Planet planetB = planets[p];

				float distA = Vector3.Distance(planetA.position, _myPos);
				float distB = Vector3.Distance(planetB.position, _myPos);

				if(distB < distA)
				{
					planets[p-1] = planetB;
					planets[p] = planetA;
				}
			}

			length--;
			if (length < 2)
			{
				return;
			}
		}
	}
}


// SORT WAYPOINTS //
void SortWaypoints(List<Waypoint> waypoints)
{
	int length = waypoints.Count;

	for(int i = 0; i < length - 1; i++)
	{
		for(int w = 1; w < length; w++)
		{
			Waypoint pointA = waypoints[w-1];
			Waypoint pointB = waypoints[w];

			float distA = Vector3.Distance(pointA.position, _myPos);
			float distB = Vector3.Distance(pointB.position, _myPos);

			if(distB < distA)
			{
				waypoints[w-1] = pointB;
				waypoints[w] = pointA;
			}
		}

		length--;
		if (length < 2)
		{
			return;
		}
	}
}


// TRANSFORM VECTOR //	   Transforms vector location of planet or waypoint for StarMap view.
 public Vector3 transformVector(Vector3 vectorIn, StarMap map)
 {	   
	double xS = vectorIn.X - map.center.X; //Vector X - Map X
	double yS = vectorIn.Y - map.center.Y; //Vector Y - Map Y
	double zS = vectorIn.Z - map.center.Z; //Vector Z - Map Z 

	double r = map.rotationalRadius;

	double cosAz = Math.Cos(ToRadians(map.azimuth));
	double sinAz = Math.Sin(ToRadians(map.azimuth));

	double cosAlt = Math.Cos(ToRadians(map.altitude));
	double sinAlt = Math.Sin(ToRadians(map.altitude));

	// Transformation Formulas from Matrix Calculations
	double xT = cosAz * xS + sinAz * zS;
	double yT = sinAz*sinAlt * xS + cosAlt * yS - sinAlt * cosAz * zS;
	double zT = -sinAz * cosAlt * xS + sinAlt * yS + cosAz * cosAlt *zS + r;

	Vector3 vectorOut = new Vector3(xT,yT,zT);
	return vectorOut;
 }
 
 
// ROTATE VECTOR //
public Vector3 rotateVector(Vector3 vecIn, StarMap map)
{
	float x = vecIn.X;
	float y = vecIn.Y;
	float z = vecIn.Z;

	float cosAz = (float) Math.Cos(ToRadians(map.azimuth));
	float sinAz = (float) Math.Sin(ToRadians(map.azimuth));

	float cosAlt = (float) Math.Cos(ToRadians(map.altitude));
	float sinAlt = (float) Math.Sin(ToRadians(map.altitude));
  
	float xT = cosAz * x + sinAz * z;
	float yT = sinAz * sinAlt * x + cosAlt * y -sinAlt * cosAz * z;
	float zT = -sinAz * cosAlt * x + sinAlt * y + cosAz * cosAlt * z;

	Vector3 vecOut = new Vector3(xT, yT, zT);
	return vecOut;
}


// ROTATE MOVEMENT //	 Rotates Movement Vector for purpose of translation.
public Vector3 rotateMovement(Vector3 vecIn, StarMap map)
{
	float x = vecIn.X;
	float y = vecIn.Y;
	float z = vecIn.Z;

	float cosAz = (float) Math.Cos(ToRadians(-map.azimuth));
	float sinAz = (float) Math.Sin(ToRadians(-map.azimuth));

	float cosAlt = (float) Math.Cos(ToRadians(-map.altitude));
	float sinAlt = (float) Math.Sin(ToRadians(-map.altitude));

	float xT = cosAz * x + sinAz * sinAlt * y + sinAz * cosAlt * z;
	float yT = cosAlt * y - sinAlt * z;
	float zT = -sinAz * x + cosAz * sinAlt * y + cosAz * cosAlt * z;

	Vector3 vecOut = new Vector3(xT, yT, zT);
	return vecOut;	  
}


// CYCLE EXECUTE // Wait specified number of cycles to execute cyclial commands
public void CycleExecute()
{
	_cycleStep--;

	// EXECUTE CYCLE DELAY FUNCTIONS
	if(_cycleStep < 0)
	{
		_cycleStep = _cycleLength;

		//Toggle Indicator LightGray
		_lightOn = !_lightOn;

		if(_waypointList.Count > 0)
		{
			_sortCounter++;
			if(_sortCounter >= 10)
			{
				SortWaypoints(_waypointList);
				_sortCounter = 0;
			}
		}
		
		if(_planets)
		{
			//Sort Planets by proximity to ship.
			SortByNearest(_planetList);
			_nearestPlanet = _planetList[0];
			
			if(_mapList.Count < 1)
				return;
			
			foreach(StarMap map in _mapList)
			{
				if(map.mode == "PLANET" || map.mode == "CHASE" || map.mode == "ORBIT")
				{
					UpdateMap(map);
				}
			}
		}
		
		if(_planetToLog)
		{
			DataToLog();
			_planetToLog = false;
		}
	}
}


// LIST TO NAMES // Builds multi-line string of names from list entries
string listToStrings(List<string> inputs){
	string output = "";

	if(inputs.Count < 1)
		return output;
	
	foreach(string input in inputs){
		output += input + "\n";
	}
	
	return output.Trim();
}


// SET INI // Update ini parameter for block, and write back to custom data.
void setIni(IMyTerminalBlock block, string entry, string parameter, string arg){
	MyIni blockIni = DataToIni(block);
	blockIni.Set(entry, parameter, arg);
	block.CustomData = blockIni.ToString();
}


// GET INI // Gets ini value from block.  Returns default argument if doesn't exist.
string getIni(IMyTerminalBlock block, string entry, string parameter, string defaultVal){
	MyIni blockIni = DataToIni(block);
	
	if(!block.CustomData.Contains(entry)||!block.CustomData.Contains(parameter))
		setIni(block, entry, parameter, defaultVal);
	
	return blockIni.Get(entry, parameter).ToString();
}


// REFRESH // - Updates map info from map's custom data
void refresh(){
	_planetList = new List<Planet>();
	_unchartedList = new List<Planet>();
	_waypointList = new List<Waypoint>();
	_mapList = new List<StarMap>();
	_mapBlocks = new List<IMyTerminalBlock>();
	
		_mapLog = DataToIni(Me);

	//Name of screen to print map to.
	_mapTag =  _mapLog.Get("Map Settings", "MAP_Tag").ToString();

	//Index of screen to print map to.
	_mapIndex = _mapLog.Get("Map Settings","MAP_Index").ToUInt16();

	//Index of screen to print map data to.
	_dataIndex = _mapLog.Get("Map Settings","Data_Index").ToUInt16();

	//Name of reference block
	_refName = _mapLog.Get("Map Settings", "Reference_Name").ToString();

	//Name of Data Display Block
	_dataName = _mapLog.Get("Map Settings", "Data_Tag").ToString();
	
	//Slow Mode
	_slowMode = ParseBool(getIni(Me, "Map Settings", "Slow_Mode", "false"));
	
	//Grid Tag
	_gridTag = getIni(Me, "Map Settings", "Grid_ID", Me.CubeGrid.EntityId.ToString());
	
	if(_gridTag == ""){
		_gridTag = Me.CubeGrid.EntityId.ToString();
		_mapLog.Set("Map Settings", "Grid_ID", _gridTag);
		Me.CustomData = _mapLog.ToString();
	}
	
	//Cycle Step Length
	try{
		_cycleLength = Int16.Parse(getIni(Me, "Map Settings", "Cycle_Step", "5"));
	}catch{
		_cycleLength = 5;
	}
	_cycleStep = _cycleLength;

	if(_mapTag == "" || _mapTag == "<name>")
	{
	   Echo("No LCD specified!!!");
	}
	else
	{
		GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);
		foreach(IMyTerminalBlock mapBlock in _mapBlocks){
			if(onGrid(mapBlock)){
				List<StarMap> maps = ParametersToMaps(mapBlock);
				
				if(maps.Count > 0)
				{
					foreach(StarMap map in maps)
					{
						activateMap(map);
						MapToParameters(map);
					}
				}
			}
		}
	}

	if(_dataName != "" || _dataName != "<name>")
	{
		GridTerminalSystem.SearchBlocksOfName(_dataName, _dataBlocks);
		if(_dataBlocks.Count > 0)
		{
			IMyTextSurfaceProvider dataBlock = _dataBlocks[0] as IMyTextSurfaceProvider;
			_dataSurface = dataBlock.GetSurface(_dataIndex);
			_dataSurface.ContentType = ContentType.TEXT_AND_IMAGE;
		}
	}

	if(_refName == "" || _refName == "<name>")
	{
		_statusMessage = "WARNING: No Reference Block Name Specified!\nMay result in false orientation!";
		_refBlock = Me as IMyTerminalBlock;
	}
	else
	{
		List<IMyTerminalBlock> refBlocks = new List<IMyTerminalBlock>();
		GridTerminalSystem.SearchBlocksOfName(_refName, refBlocks);
		if(refBlocks.Count > 0)
		{
			_refBlock = refBlocks[0] as IMyTerminalBlock;
			Echo("Reference: " + _refBlock.CustomName);
		}
		else
		{
			_statusMessage = "WARNING: No Block containing " + _refName + " found.\nMay result in false orientation!";
			_refBlock = Me as IMyTerminalBlock;
		}
	}



	string planetData = _mapLog.Get("Map Settings", "Planet_List").ToString();

	string [] mapEntries = planetData.Split('\n');
	foreach(string planetString in mapEntries){
		if(planetString.Contains(";"))
		{
			Planet planet = new Planet(planetString);
			if(planet.isCharted)
			{
				_planetList.Add(planet);
			}
			else
			{
				_unchartedList.Add(planet);
			}
		}
	}
	_planets = _planetList.Count > 0;

	string waypointData = _mapLog.Get("Map Settings", "Waypoint_List").ToString();
	string [] gpsEntries = waypointData.Split('\n');
	
	foreach(string waypointString in gpsEntries)
	{
		if(waypointString.Contains(";"))
		{
		   Waypoint waypoint = StringToWaypoint(waypointString);
		   _waypointList.Add(waypoint);
		}
	}
	
	// Start with indicator light on.
	_lightOn = true;
}


// BORROWED FUNCTIONS /////////////////////////////////////////////////////////////////////////////////////////////////////////


// PREPARE TEXT SURFACE FOR SPRITES //
public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
{
	// Set the sprite display mode
	textSurface.ContentType = ContentType.SCRIPT;
	textSurface.ScriptBackgroundColor = new Color(0,0,0);
	// Make sure no built-in script has been selected
	textSurface.Script = "";
}


// DRAW SPRITES //
public void DrawSprites(StarMap map)
{
	Echo("[MAP " + map.number + "]");
	Vector3 mapCenter = map.center;

	// Create background sprite
	Color gridColor = new Color(0,64,0);
	DrawTexture("Grid", new Vector2(0,map.viewport.Width/2), map.viewport.Size, 0, gridColor);

	//DRAW PLANETS
	List<Planet> displayPlanets = new List<Planet>();
	
	foreach(Planet planet in _planetList)
	{
		if(planet.transformedCoords.Count == _mapList.Count && planet.transformedCoords[map.number].Z > map.focalLength)
		{
			displayPlanets.Add(planet);
		}
	}
	
	DrawPlanets(displayPlanets, map);
	//DRAW WAYPOINTS & UNCHARTED SURFACE POINTS
	if(map.gpsState > 0)
	{
		DrawWaypoints(map);
		PlotUncharted(map);
	}

	// DRAW SHIP
	if(map.showShip)
	{
		DrawShip(map, displayPlanets);
	}

	// MAP INFO
	if(map.showInfo)
	{
		DrawMapInfo(map);
	}
}


// DRAW MAP INFO //
public void DrawMapInfo(StarMap map)
{
	//DEFAULT SIZING / STRINGS
	float fontSize = 0.6f;
	int barHeight = BAR_HEIGHT;
	String angleReading = map.altitude*-1 + "° " + map.azimuth + "°";
	String shipMode = "S";
	String planetMode = "P";
	String freeMode = "F";
	String worldMode = "W";
	String chaseMode = "C";
	String orbitMode = "O";

	if(map.viewport.Width > 500)
	{
		fontSize *= 1.5f;
		barHeight *= 2;
		angleReading = "Alt:" + map.altitude*-1 + "°  Az:" + map.azimuth + "°";
		shipMode = "SHIP";
		planetMode = "PLANET";
		freeMode = "FREE";
		worldMode = "WORLD";
		chaseMode = "CHASE";
		orbitMode = "ORBIT";
	}

	//TOP BAR
	var position = map.viewport.Center;
	position -= new Vector2(map.viewport.Width/ 2, map.viewport.Height/2 - barHeight/2);
	DrawTexture("SquareSimple", position, new Vector2(map.viewport.Width, barHeight), 0, Color.Black);
  
	//MODE	  
	position += new Vector2(SIDE_MARGIN,-TOP_MARGIN);

	string modeReading = "";
	switch(map.mode)
	{
		case "SHIP":
			modeReading = shipMode;
			break;
		case "PLANET":
			modeReading = planetMode;
			break;
		case "WORLD":
			modeReading = worldMode;
			break;
		case "CHASE":
			modeReading = chaseMode;
			break;
		case "ORBIT":
			modeReading = orbitMode;
			break;
		default:
			modeReading = freeMode;
			break;		  
	}

	DrawText(modeReading, position, fontSize, TextAlignment.LEFT, Color.White);

	// CENTER READING
	string xCenter = abbreviateValue(map.center.X);
	string yCenter = abbreviateValue(map.center.Y);
	string zCenter = abbreviateValue(map.center.Z);
	string centerReading = "[" + xCenter + ", " + yCenter + ", " + zCenter + "]";

	position += new Vector2(map.viewport.Width/2 - SIDE_MARGIN, 0);

	DrawText(centerReading, position, fontSize, TextAlignment.CENTER, Color.White);

	// RUNNING INDICATOR
	position += new Vector2(map.viewport.Width/2 - SIDE_MARGIN, TOP_MARGIN);

	Color lightColor = new Color(0,8,0);

	if(_lightOn)
	{
		DrawTexture("Circle", position,new Vector2(7,7), 0, lightColor);
	}
	
	// MAP ID
	position -= new Vector2(5,7);
	
	string mapID = "[" + map.number + "]";
	
	DrawText(mapID, position, fontSize, TextAlignment.RIGHT, Color.White);

	// BOTTOM BAR
	position = map.viewport.Center;
	position -= new Vector2(map.viewport.Width/ 2, barHeight/2 - map.viewport.Height/2);

	if(map.viewport.Width == 1024)
	{
		position = new Vector2(0, map.viewport.Height - barHeight/2);
	}
	DrawTexture("SquareSimple", position, new Vector2(map.viewport.Width, barHeight), 0, Color.Black);

	// FOCAL LENGTH READING
	position += new Vector2(SIDE_MARGIN,-TOP_MARGIN);

	string dofReading = "FL:" + abbreviateValue((float)map.focalLength);

	DrawText(dofReading, position, fontSize, TextAlignment.LEFT, Color.White);

	// ANGLE READING
	position += new Vector2(map.viewport.Width/2 - SIDE_MARGIN, 0);

	DrawText(angleReading, position, fontSize, TextAlignment.CENTER, Color.White);

	// RADIUS READING
	string radius = "R:"+ abbreviateValue((float)map.rotationalRadius);
	position += new Vector2(map.viewport.Width/2 - SIDE_MARGIN, 0);

	DrawText(radius, position, fontSize, TextAlignment.RIGHT, Color.White);
}


// TO RADIANS //  Converts Degree Value to Radians
public double ToRadians(int angle)
{
	double radianValue = (double) angle * Math.PI/180;
	return radianValue;
}


// TO DEGREES //
public float ToDegrees(float angle)
{
	float degreeValue = angle * 180/(float) Math.PI;
	return degreeValue;
}


// DEGREE ADD //	Adds two degree angles.	 Sets Rollover at +/- 180°
public static int DegreeAdd(int angle_A, int angle_B)
{
	int angleOut = angle_A + angle_B;

	if(angleOut > 180)
	{
		angleOut -= 360;
	}
	else if (angleOut < -179)
	{
		angleOut += 360;
	}

	return angleOut;
}


// UPDATE MAP // Transform logged locations based on map parameters
public void UpdateMap(StarMap map)
{
	if(_mapList.Count == 0)
		return;

	if(_planets)
	{
		foreach(Planet planet in _planetList)
		{
			Vector3 newCenter = transformVector(planet.position, map);

			if(planet.transformedCoords.Count < _mapList.Count)
			{
				planet.transformedCoords.Add(newCenter);
			}
			else
			{
				planet.transformedCoords[map.number] = newCenter;
			}
		}
	}

	if(_waypointList.Count > 0)
	{
		foreach(Waypoint waypoint in _waypointList)
		{
			Vector3 newPos = transformVector(waypoint.position, map);
			if(waypoint.transformedCoords.Count < _mapList.Count)
			{
				waypoint.transformedCoords.Add(newPos);
			}
			else
			{
				waypoint.transformedCoords[map.number] = newPos;
			}
		}
	}
}

// ACTIVATE MAP // activates tagged map screen without having to recompile.
void activateMap(StarMap map)
{
	IMyTextSurfaceProvider mapBlock = map.block as IMyTextSurfaceProvider;
	map.drawingSurface = mapBlock.GetSurface(map.index);
	PrepareTextSurfaceForSprites(map.drawingSurface);

	// Calculate the viewport offset by centering the surface size onto the texture size
	map.viewport = new RectangleF(
	(map.drawingSurface.TextureSize - map.drawingSurface.SurfaceSize) / 2f,
		map.drawingSurface.SurfaceSize
	);
	map.number = _mapList.Count;
	_mapList.Add(map);
	UpdateMap(map);
}