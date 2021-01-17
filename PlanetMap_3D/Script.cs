//////////////////////
// PLANET MAP 3D //
/////////////////////

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
const int CYCLE_LENGTH = 5; // Number of runs for delayed cycle



// THERE IS NO REASON TO ALTER ANYTHING BELOW THIS LINE! //////////////////////////////////////////////////////////////////////////////////////////////////////////


// OTHER CONSTANTS //
const int BAR_HEIGHT = 20; //Default height of parameter bars
const int TOP_MARGIN = 8; // Margin for top and bottom of frame
const int SIDE_MARGIN = 15; // Margin for sides of frame
const int MAX_VALUE = 1073741824; //General purpose MAX value = 2^30
const int DATA_PAGES = 4;  // Number of Data Display Pages
const string SLASHES = " //////////////////////////////////////////////////////////////";
const string DEFAULT_SETTINGS = "[Map Settings]\nMAP_Tag=[MAP]\nMAP_Index=0\nData_Tag=[Map Data]\nData_Index=0\nReference_Name=[Reference]\nPlanet_List=\nWaypoint_List=\n";
string _defaultDisplay = "[mapDisplay]\nCenter=(0,0,0)\nMode=FREE\nFocalLength="
							+DV_FOCAL+"\nRotationalRadius="+DV_RADIUS +"\nAzimuth=0\nAltitude="
							+DV_ALTITUDE+"\nIndexes=\ndX=0\ndY=0\ndZ=0\ndAz=0\nGPS=True\nNames=True\nShip=True\nInfo=True\nPlanet=";


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
int _cycleStep;
int _sortCounter = 0;
float _brightnessMod;
static string _statusMessage;
string _activePlanet ="";
string _activeWaypoint = "";
string _clipboard = "";
Vector3 _trackSpeed;
Vector3 _origin = new Vector3(0,0,0);
Vector3 _myPos;
List<IMyTerminalBlock> _mapBlocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> _dataBlocks = new List<IMyTerminalBlock>();
List<Planet> _planetList = new List<Planet>();
List<Planet> _unchartedList = new List<Planet>();
List<Waypoint> _waypointList = new List<Waypoint>();
List<StarMap> _mapList = new List<StarMap>();
Planet _nearestPlanet;
MySpriteDrawFrame _frame;

IMyTextSurface _dataSurface;
IMyTerminalBlock _refBlock;
//RectangleF _viewport;


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
	public IMyTextSurface drawingSurface;
	public RectangleF viewport;
	public IMyTerminalBlock block;
	public bool showGPS;
	public bool showNames;
	public bool showShip;
	public bool showInfo;
	public int planetIndex;
	public int waypointIndex;
	public string activePlanetName;
	public Planet activePlanet;

	public StarMap()
	{
		this.azSpeed = 0;
		this.planetIndex = 0;
		this.waypointIndex = 0;
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

	if(!oldData.Contains("[Map Settings]"))
	{
		if(oldData.StartsWith("["))
		{
			newData += oldData;
		}
		else
		{
			newData += "---\n\n" + oldData;
		}
		Me.CustomData = newData;
	}

	// Call the TryParse method on the custom data. This method will
	// return false if the source wasn't compatible with the parser.
	MyIniParseResult result;
	if (!_mapLog.TryParse(Me.CustomData, out result)) 
		throw new Exception(result.ToString());

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

	string planetData = _mapLog.Get("Map Settings", "Planet_List").ToString();

	string [] mapEntries = planetData.Split('\n');
	foreach(string planetString in mapEntries)
	{
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

	if(_mapTag == "" || _mapTag == "<name>")
	{
	   Echo("No LCD specified!!!");
	}
	else
	{
		GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);
		for(int m = 0; m < _mapBlocks.Count; m++)
		{
			IMyTextSurfaceProvider mapBlock = _mapBlocks[m] as IMyTextSurfaceProvider;
			List<StarMap> maps = ParametersToMaps(mapBlock as IMyTerminalBlock);
			
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

	_previousCommand = "NEWLY LOADED";
	
	// Start with indicator light on.
	_lightOn = true;
	_cycleStep = CYCLE_LENGTH;
	
	 // Set the continuous update frequency of this script
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
	_myPos = _refBlock.GetPosition();
	
	Echo("//////// PLANET MAP 3D ////////");
	Echo(_previousCommand);
	Echo(_statusMessage);
	Echo("MAP Count: " + _mapList.Count);

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
		Echo("GPS Count: " + _waypointList.Count);
		Waypoint waypoint = _waypointList[_waypointList.Count - 1];
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
			string command = args[0].ToUpper();
			string cmdArg = "";
			
			_statusMessage = "";
			_activeWaypoint = "";
			_previousCommand = "Command: " + argument;

			// If there are multiple words in the argument. Combine the latter words into the entity name.
			if(args.Length == 1)
			{
				cmdArg = "0";
			}
			else if(args.Length > 1)
			{
				cmdArg = args[1];
				if(args.Length > 2)
				{
					for(int q = 2; q < args.Length;	 q++)
					{
						cmdArg += " " + args[q];
					}
				}
			}
			
			List<StarMap> maps = ArgToMaps(cmdArg);
			
			switch(command)
			{
				case "ZOOM_IN":
					Zoom(maps, true);
					break;
				case "ZOOM_OUT":
					Zoom(maps, false);
					break;
				case "MOVE_LEFT":
					MoveCenter("LEFT", maps);
					break;
				case "MOVE_RIGHT":
					MoveCenter("RIGHT", maps);
					break;
				case "MOVE_UP":
					MoveCenter("UP", maps);
					break;
				case "MOVE_DOWN":
					MoveCenter("DOWN", maps);
					break;
				case "MOVE_FORWARD":
					MoveCenter("FORWARD", maps);
					break;
				case "MOVE_BACKWARD":
					MoveCenter("BACKWARD", maps);
					break;
				case "DEFAULT_VIEW":
					MapsToDefault(maps);
					break;
				case "ROTATE_LEFT":
					RotateMaps("LEFT", maps);
					break;
				case "ROTATE_RIGHT":
					RotateMaps("RIGHT", maps);
					break;
				case "ROTATE_UP":
					RotateMaps("UP", maps);
					break;
				case "ROTATE_DOWN":
					RotateMaps("DOWN", maps);
					break;
				case "GPS_ON":
					Show("GPS", maps, 1);
					break;
				case "GPS_OFF":
					Show("GPS", maps, 0);
					break;
				case "SHOW_GPS":
					Show("GPS", maps, 1);
					break;
				case "HIDE_GPS":
					Show("GPS", maps, 0);
					break;
				case "TOGGLE_GPS":
					Show("GPS", maps, 3);
					break;
				case "NEXT_PLANET":
					CyclePlanets(maps, true);
					break;
				case "PREVIOUS_PLANET":
					CyclePlanets(maps, false);
					break;
				case "NEXT_WAYPOINT":
					CycleWaypoints(maps, true);
					break;
				case "PREVIOUS_WAYPOINT":
					CycleWaypoints(maps, false);
					break;
				case "SPIN_LEFT":
					SpinMaps(maps, ANGLE_STEP/2);
					break;
				case "SPIN_RIGHT":
					SpinMaps(maps, -ANGLE_STEP/2);
					break;
				case "STOP":
					StopMaps(maps);
					break;
				case "TRACK_LEFT":
					TrackCenter(maps, 0, MOVE_STEP);
					break;
				case "TRACK_RIGHT":
					TrackCenter(maps, 0, -MOVE_STEP);
					break;
				case "TRACK_UP":
					TrackCenter(maps, 1, MOVE_STEP);
					break;
				case "TRACK_DOWN":
					TrackCenter(maps, 1, -MOVE_STEP);;
					break;
				case "TRACK_FORWARD":
					TrackCenter(maps, 2, MOVE_STEP);
					break;
				case "TRACK_BACKWARD":
					TrackCenter(maps, 2, -MOVE_STEP);
					break;
				case "SHOW_NAMES":
					Show("NAMES", maps, 1);
					break;
				case "HIDE_NAMES":
					Show("NAMES", maps, 0);
					break;
				case "TOGGLE_NAMES":
					Show("NAMES", maps, 3);
					break;
				case "SHOW_INFO":
					Show("INFO", maps, 1);
					break;
				case "HIDE_INFO":
					Show("INFO", maps, 0);
					break;
				case "TOGGLE_INFO":
					Show("INFO", maps, 3);
					break;
				case "SHOW_SHIP":
					Show("SHIP", maps, 1);
					break;
				case "HIDE_SHIP":
					Show("SHIP", maps, 0);
					break;
				case "TOGGLE_SHIP":
					Show("SHIP", maps, 3);
					break;
				case "WORLD_MODE":
					ChangeMode("WORLD", maps);
					break;
				case "SHIP_MODE":
					ChangeMode("SHIP", maps);
					break;
				case "PLANET_MODE":
					ChangeMode("PLANET", maps);
					break;
				case "FREE_MODE":
					ChangeMode("FREE", maps);
					break;
				case "ORBIT_MODE":
					ChangeMode("ORBIT", maps);
					break;
				case "PREVIOUS_MODE":
					CycleMode(maps, false);
					break;
				case "NEXT_MODE":
					CycleMode(maps, true);
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
				case "WAYPOINT_OFF":
					SetWaypointState(cmdArg, 0);
					break;
				case "WAYPOINT_ON":
					SetWaypointState(cmdArg, 1);
					break;
				case "TOGGLE_WAYPOINT":
					SetWaypointState(cmdArg, 2);
					break;
				case "DELETE_WAYPOINT":
					SetWaypointState(cmdArg, 3);
					break;
				case "LOG_WAYPOINT":
					LogWaypoint(cmdArg, _myPos, "WAYPOINT");
					break;
				case "LOG_BASE":
					LogWaypoint(cmdArg, _myPos, "BASE");
					break;
				case "LOG_STATION":
					LogWaypoint(cmdArg, _myPos, "STATION");
					break;					  
				case "LOG_LANDMARK":
					LogWaypoint(cmdArg, _myPos, "LANDMARK");
					break;
				case "LOG_HAZARD":
					LogWaypoint(cmdArg, _myPos, "HAZARD");
					break;
				case "LOG_ASTEROID":
					LogWaypoint(cmdArg, _myPos, "ASTEROID");
					break;
				case "PASTE_WAYPOINT":
					ClipboardToLog(cmdArg, "WAYPOINT");
					break;
				case "PASTE_BASE":
					ClipboardToLog(cmdArg, "BASE");
					break;
				case "PASTE_STATION":
					ClipboardToLog(cmdArg, "STATION");
					break;
				case "PASTE_LANDMARK":
					ClipboardToLog(cmdArg, "LANDMARK");
					break;
				case "PASTE_ASTEROID":
					ClipboardToLog(cmdArg, "ASTEROID");
					break;
				case "PASTE_HAZARD":
					ClipboardToLog(cmdArg, "HAZARD");
					break;
				case "EXPORT_WAYPOINT":
					_clipboard = LogToClipboard(cmdArg);
					break;
				case "NEW_PLANET":
					NewPlanet(cmdArg);
					break;
				case "LOG_NEXT":
					LogNext(cmdArg);
					break;
				case "DELETE_PLANET":
					DeletePlanet(cmdArg);
					break;
				case "COLOR_PLANET":
					SetPlanetColor(cmdArg);
					break;
				case "PLOT_JUMP":
					PlotJumpPoint(cmdArg);
					break;
				case "PROJECT":
					ProjectPoint(cmdArg);
					break;
				case "NEXT_PAGE":
					NextPage();
					break;
				case "PREVIOUS_PAGE":
					PreviousPage();
					break;
				case "SCROLL_DOWN":
					_scrollIndex++;
					break;
				case "SCROLL_UP":
					_scrollIndex--;
					if(_scrollIndex < 0)
						_scrollIndex = 0;
					break;
				case "SCROLL_HOME":
					_scrollIndex = 0;
					break;
				case "BRIGHTEN":
					if(_brightnessMod < BRIGHTNESS_LIMIT)
						_brightnessMod += 0.25f;
					break;
				case "DARKEN":
					if(_brightnessMod > 1)
						_brightnessMod -= 0.25f;
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
			if(_cycleStep == CYCLE_LENGTH || _previousCommand == "NEWLY LOADED")
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
		}
	}
	else
	{
		_statusMessage = "NO MAP DISPLAY FOUND!\nPlease add tag " + _mapTag + " to desired block.\n";
		GridTerminalSystem.SearchBlocksOfName(_mapTag, _mapBlocks);
		//activateMap();
	}

	if(_dataBlocks.Count > 0)
	{
		DisplayData();
	}
}


// VIEW FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// SHOW //
void Show(string attribute, List<StarMap> maps, int state)
{
	if(NoMaps(maps))
		return;
		
	foreach(StarMap map in maps)
	{
		switch(attribute)
		{
			case "GPS":
				map.showGPS = setState(map.showGPS, state);
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
		}
	}
}


// PARAMETERS TO MAPS //
public List<StarMap> ParametersToMaps(IMyTerminalBlock mapBlock)
{
	List<StarMap> mapsOut = new List<StarMap>();
	
	MyIni lcdIni = new MyIni();
	
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

	MyIniParseResult result;
	if (!lcdIni.TryParse(mapBlock.CustomData, out result)) 
		throw new Exception(result.ToString());

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
	List<string> gpsBools = StringToEntries(lcdIni.Get("mapDisplay","GPS").ToString(), ',', iLength, "true");
	List<string> nameBools = StringToEntries(lcdIni.Get("mapDisplay","Names").ToString(), ',', iLength, "true");
	List<string> shipBools = StringToEntries(lcdIni.Get("mapDisplay","Ship").ToString(), ',', iLength, "true");
	List<string> infoBools = StringToEntries(lcdIni.Get("mapDisplay","Info").ToString(), ',', iLength, "true");
	List<string> planets = StringToEntries(lcdIni.Get("mapDisplay","Planet").ToString(), ',', iLength, "[null]");
	
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
		map.showGPS = ParseBool(gpsBools[i]);
		map.showNames = ParseBool(nameBools[i]);
		map.showShip = ParseBool(shipBools[i]);
		map.showInfo = ParseBool(infoBools[i]);
		
		mapsOut.Add(map);
	}
	
	return mapsOut;
}


// DATA TO LOG //
public void DataToLog()
{
	MyIni mapIni = new MyIni();

	MyIniParseResult result;
	if (!mapIni.TryParse(Me.CustomData, out result)) 
		throw new Exception(result.ToString());

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
	if(_planetList.Count > 0)
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
	MyIni lcdIni = new MyIni();

	MyIniParseResult result;
	if (!lcdIni.TryParse(map.block.CustomData, out result)) 
		throw new Exception(result.ToString());
	
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
	string newGPS = InsertEntry(map.showGPS.ToString(), lcdIni.Get("mapDisplay", "GPS").ToString(), ',', i, entries, "True");
	string newNames = InsertEntry(map.showNames.ToString(), lcdIni.Get("mapDisplay", "Names").ToString(), ',', i, entries, "True");
	string newShip = InsertEntry(map.showGPS.ToString(), lcdIni.Get("mapDisplay", "Ship").ToString(), ',', i, entries, "True");	
	string newInfo = InsertEntry(map.showGPS.ToString(), lcdIni.Get("mapDisplay", "Info").ToString(), ',', i, entries, "True");
	string newPlanets = InsertEntry(map.activePlanetName, lcdIni.Get("mapDisplay", "Planet").ToString(), ',',i, entries, "[null]");
	
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
	
	map.block.CustomData = lcdIni.ToString();
}


// CLIPBOARD TO LOG //
void ClipboardToLog(string clipboard, string markerType)
{
	string[] waypointData = clipboard.Split(':');
	if(waypointData.Length < 5)
	{
		_statusMessage = "Does not match GPS format:/nGPS:<name>:X:Y:Z:<color>:";
		return;
	}
	
	Vector3 position = new Vector3(float.Parse(waypointData[2]),float.Parse(waypointData[3]),float.Parse(waypointData[4]));
	LogWaypoint(waypointData[1], position, markerType);
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
public void LogWaypoint(String waypointName, Vector3 position, String markerType)
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
		_statusMessage = "No waypoint " + waypointName + " found.";
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
	
	LogWaypoint(name + designation, position, "WAYPOINT");
}


// PROJECT POINT//
void ProjectPoint(string arg)
{
	string [] args = arg.Split(' ');
	
	if(args.Length < 3)
	{
		_statusMessage = "INSUFFICIENT ARGUMENT!\nPlease include arguments <MARKER-TYPE> <DISTANCE(in meters)> <WAYPOINT NAME>";
		return;
	}
	
	int distance;
	if(int.TryParse(args[1],out distance))
	{
		string name = "";
		for(int i = 2; i < args.Length; i++)
		{
			name += args[i] + " ";
		}

		Vector3 location = _myPos + _refBlock.WorldMatrix.Forward * distance;

		string marker = args[0];
		
		LogWaypoint(name.Trim(), location, marker);
	
		return;
	}

	_statusMessage = "DISTANCE ARGEMENT FAILED!\nPlease include Distance in meters. Do not include unit.";
}


// PLANET ERROR //
void PlanetError(string name)
{
	_statusMessage = "No planet " + name + " found!";
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
	
	if(_planetList.Count > 0)
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
	}
	foreach(StarMap map in _mapList)
	{
		UpdateMap(map);
	}
	
	DataToLog();
}


// SET COLOR //
void SetPlanetColor(String argument)
{
	String[] args = argument.Split(' ');
	String planetColor = args[0];
	if(args.Length < 2)
	{
		_statusMessage = "Insufficient Argument.  COLOR_PLANET requires COLOR and PLANET NAME.\n";
	}
	else
	{
		String planetName = "";
		for(int p = 1; p < args.Length; p++)
		{
			planetName += args[p] + " ";
		}
		planetName = planetName.Trim(' ').ToUpper();

		Planet planet = GetPlanet(planetName);
		
		if(planet != null)
		{
			planet.color = planetColor;
			_statusMessage = planetName + " color changed to " + planetColor + ".\n";
			DataToLog();
			return;
		}
		
		PlanetError(planetName);
	}
}


// ZOOM // Changes Focal Length of Maps. true => Zoom In / false => Zoom Out
void Zoom(List<StarMap> maps, bool zoomIn)
{
	if(NoMaps(maps))
		return;
	
	foreach(StarMap map in maps)
	{
		int doF = map.focalLength;
		float newScale;
		
		if(zoomIn)
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
void MoveCenter(string movement, List<StarMap> maps)
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
void TrackCenter(List<StarMap> maps, int axis, int speed)
{
	if(NoMaps(maps))
		return;	
		
	foreach(StarMap map in maps)
	{
		switch(axis)
		{
			case 0:
				map.dX += speed;
				break;
			case 1:
				map.dY += speed;
				break;
			case 2:
				map.dZ += speed;
				break;
		}
	}
}


// ROTATE MAPS //
void RotateMaps(string direction, List<StarMap> maps)
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
void SpinMaps(List<StarMap> maps, int deltaAz)
{
	if(NoMaps(maps))
		return;
		
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

	if(_planetList.Count > 0)
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
	map.azimuth = DegreeAdd((int) ToDegrees((float) Math.Atan2(heading.Z, heading.X)), -90);
	map.center = _myPos;
}


// ALIGN ORBIT //
void AlignOrbit(StarMap map)
{
	map.center = _myPos;
	map.altitude = 0;
	Vector3 orbit = _myPos - _nearestPlanet.position;
	map.azimuth = (int) ToDegrees((float)Math.Abs(Math.Atan2(orbit.Z, orbit.X)));
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

	if(_planetList.Count > 0)
	{
		SortByNearest(_planetList);
		_nearestPlanet = _planetList[0];
		ShipToPlanet(map);

		if(map.activePlanet.radius < 30000)
		{
			map.focalLength *= 4;
		}
	}
//	else
//	{
//		map.mode = "FREE";
//	}

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
	if(_planetList.Count > 0)
	{
		if(map.activePlanetName == "" && map.activePlanetName == "[null]");
		{
			map.activePlanet = _nearestPlanet;
			map.activePlanetName = _nearestPlanet.name;
		}
		
		Vector3 shipVector = _myPos - map.activePlanet.position;
		float magnitude = Vector3.Distance(_myPos, map.activePlanet.position);

		float azAngle = (float) Math.Atan2(shipVector.Z,shipVector.X);
		float altAngle = (float) Math.Asin(shipVector.Y/magnitude);

		map.center = map.activePlanet.position;
		map.azimuth = DegreeAdd((int) ToDegrees(azAngle),90);
		map.altitude = (int) ToDegrees(-altAngle);
		
//		if(magnitude > planet.radius * 2)
//		{
//			map.mode = "SHIP";
//		}
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

	if(_planetList.Count > 0)
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
		
		if(map.waypointIndex < 0)
		{
			map.waypointIndex = gpsCount - 1;
		}
		else if(map.waypointIndex >= gpsCount)
		{
			map.waypointIndex = 0;
		}
		
		Waypoint waypoint = _waypointList[map.waypointIndex];
		map.center = waypoint.position;
	}
}


// SELECT PLANET //
void SelectPlanet(Planet planet, StarMap map)
{
	map.center = planet.position;
	map.activePlanetName = planet.name;
	
	if(planet.name != "" && planet.name != "[null]")
		map.activePlanet=GetPlanet(planet.name);

	
	if(planet.radius < 30000)
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

	Vector3 transformedShip = transformVector(_myPos, map);
	Vector2 shipPos = PlotObject(transformedShip,map);
	float shipX = shipPos.X;
	float shipY = shipPos.Y;

	int vertMod = 0;
	if(_showMapParameters)
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
			switch(planetColor.ToUpper())
			{
				case "RED":
					aftColor = new Color(48,0,0);
					bodyColor = new Color(64,0,0);
					plumeColor = aftColor;
					break;
				case "GREEN":
					aftColor = new Color(0,48,0);
					bodyColor = new Color(0,64,0);
					plumeColor = aftColor;
					break;
				case "BLUE":
					aftColor = new Color(0,0,48);
					bodyColor = new Color(0,0,64);
					plumeColor = aftColor;
					break;
				case "YELLOW":
					aftColor = new Color(127,127,39);
					bodyColor = new Color(127,127,51);
					plumeColor = aftColor;
					break;
				case "MAGENTA":
					aftColor = new Color(96,0,96);
					bodyColor = new Color(127,0,127);
					plumeColor = aftColor;
					break;
				case "PURPLE":
					aftColor = new Color(36,0,62);
					bodyColor = new Color(48,0,96);
					plumeColor = aftColor;
					break;
				case "CYAN":
					aftColor = new Color(0,48,48);
					bodyColor = new Color(0,64,64);
					plumeColor = aftColor;
					break;
				case "LIGHTBLUE":
					aftColor = new Color(48,48,144);
					bodyColor = new Color(64,64,192);
					plumeColor = aftColor;
					break;
				case "ORANGE":
					aftColor = new Color(48,24,0);
					bodyColor = new Color(64,32,0);
					plumeColor = aftColor;
					break;
				case "TAN":
					aftColor = new Color(175,115,54);
					bodyColor = new Color(205,133,63);
					plumeColor = aftColor;
					break;
				case "BROWN":
					aftColor = new Color(43,28,13);
					bodyColor = new Color(50,33,15);
					plumeColor = aftColor;
					break;
				case "RUST":
					aftColor = new Color(48,15,12);
					bodyColor = new Color(128,40,32);
					plumeColor = aftColor;
					break;
				case "GRAY":
					aftColor = new Color(48,48,48);
					bodyColor = new Color(64,64,64);
					plumeColor = aftColor;
					break;
				case "GREY":
					aftColor = new Color(48,48,48);
					bodyColor = new Color(64,64,64);
					plumeColor = aftColor;
					break;
				case "WHITE":
					aftColor = new Color(100,150,150);
					bodyColor = new Color(192,192,192);
					plumeColor = aftColor;
					break;
			}
		}

		aftColor *= _brightnessMod;
		bodyColor *= _brightnessMod;
		plumeColor *= _brightnessMod;


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

		// Aft Ellipse
		if(headingZ < 0)
		{
			aftColor = bodyColor;
			plumeColor = bodyColor;
		}

		position -= offset * shipLength/2;
		
		DrawTexture("Circle", position, new Vector2(SHIP_SCALE, aftHeight), shipAngle, aftColor);
		
		position -= offset * shipLength/16;
		position += new Vector2(SHIP_SCALE/6, 0);
		
		DrawTexture("Circle", position, new Vector2(SHIP_SCALE*0.67f, aftHeight*0.67f), shipAngle, plumeColor);
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

		Color surfaceColor = new Color(0,0,0);
		Color lineColor = new Color(16,16,16);
		switch(planet.color.ToUpper())
		{
			case "RED":
				surfaceColor = new Color(32,0,0);
				lineColor = new Color(64,0,0);
				break;
			case "GREEN":
				surfaceColor = new Color(0,32,0);
				lineColor = new Color(0,64,0);
				break;
			case "BLUE":
				surfaceColor = new Color(0,0,32);
				lineColor = new Color(0,0,64);
				break;
			case "YELLOW":
				surfaceColor = new Color(127,127,26);
				lineColor = new Color(127,127,51);
				break;
			case "MAGENTA":
				surfaceColor = new Color(64,0,64);
				lineColor = new Color(127,0,127);
				break;
			case "PURPLE":
				surfaceColor = new Color(24,0,48);
				lineColor = new Color(48,0,96);
				break;
			case "CYAN":
				surfaceColor = new Color(0,32,32);
				lineColor = new Color(0,64,64);
				break;
			case "LIGHTBLUE":
				surfaceColor = new Color(32,32,96);
				lineColor = new Color(64,64,192);
				break;
			case "ORANGE":
				surfaceColor = new Color(32,16,0);
				lineColor = new Color(64,32,0);
				break;
			case "TAN":
				surfaceColor = new Color(153,100,48);
				lineColor = new Color(205,133,63);
				break;
			case "BROWN":
				surfaceColor = new Color(38,25,12);
				lineColor = new Color(50,33,15);
				break;
			case "RUST":
				surfaceColor = new Color(64,20,16);
				lineColor = new Color(128,40,32);
				break;
			case "GRAY":
				surfaceColor = new Color(16,16,16);
				lineColor = new Color(32,32,32);
				break;
			case "GREY":
				surfaceColor = new Color(16,16,16);
				lineColor = new Color(32,32,32);
				break;
			case "WHITE":
				surfaceColor = new Color(64,64,64);
				lineColor = new Color(192,192,192);				   
				break;
		}

		surfaceColor *= _brightnessMod;
		lineColor *= _brightnessMod;

		Vector2 startPosition = map.viewport.Center + planetPosition;

		float diameter = ProjectDiameter(planet, map);

		Vector2 position;
		Vector2 size;

		// Draw Gravity Well
		if(map.mode == "ORBIT" && planet == _nearestPlanet)
		{
			float radMod = 0.83f;
			size = new Vector2(diameter*radMod*2, diameter*radMod*2);
			position = startPosition - new Vector2(diameter*radMod,0);
			
			DrawTexture("CircleHollow", position, size, 0, Color.DarkGreen);
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
		if(diameter > HASH_LIMIT)// && Vector3.Distance(planet.position, map.center) < 2*planet.radius
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
	int markerSize = MARKER_WIDTH;
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
			Color markerColor = Color.White;
			Vector2 markerScale = new Vector2(markerSize,markerSize);

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
					startPosition += new Vector2(0,markerSize);
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
			
			if(waypoint.transformedCoords[map.number].Z > map.focalLength)
			{
				Vector2 position = startPosition - new Vector2(markerSize/2,0);

				markerColor *= _brightnessMod;

				// PRINT MARKER
				
				// Shadow
				DrawTexture(markerShape, position, markerScale, rotationMod, Color.Black);

				// Marker
				position += new Vector2(1,0);
				DrawTexture(markerShape, position, markerScale*1.2f, rotationMod, markerColor);

				position += new Vector2(1,0);
				DrawTexture(markerShape, position, markerScale, rotationMod, markerColor);

				if(waypoint.marker.ToUpper() == "STATION")
				{
					position += new Vector2(markerSize/2 - markerSize/20, 0);
					DrawTexture("SquareSimple", position, new Vector2(markerSize/10 ,markerSize), rotationMod, markerColor);
					position = startPosition;
				}

				if(waypoint.marker.ToUpper() == "HAZARD")
				{
					position += new Vector2(markerSize/2 - markerSize/20, -markerSize*0.85f);
					
					DrawText("!", position, fontSize*1.2f, TextAlignment.CENTER, Color.White);
					position = startPosition;
				}

				if(waypoint.marker.ToUpper() == "BASE")
				{
					position += new Vector2(markerSize/6, -markerSize/12);
					
					DrawTexture("SemiCircle", position, new Vector2(markerSize*1.15f,markerSize*1.15f), rotationMod, new Color(0,64,64)*_brightnessMod);
					position += new Vector2(0.25f * markerSize,-0.33f * markerSize);
				}

				if(waypoint.marker.ToUpper() == "ASTEROID")
				{
					position += new Vector2(markerSize/2 - markerSize/20, 0);
					DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32,32,32)*_brightnessMod);

					position -= new Vector2(markerSize - markerSize/10,0);
					DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32,32,32)*_brightnessMod);

					position = startPosition;					 
				}

				if(map.showNames)
				{
					// PRINT NAME
					position += new Vector2(1.33f * markerSize,-0.75f * markerSize);
					DrawText(waypoint.name, position, fontSize, TextAlignment.LEFT, Color.White*_brightnessMod);
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
	switch(_pageIndex)
	{
		case 0:
			DisplayPlanetData();
			break;
		case 1:
			DisplayWaypointData();
			break;
		case 2:
			DisplayMapData();
			break;
		case 3:
			DisplayClipboard();
			break;
	}
}


// DISPLAY PLANET DATA //
void DisplayPlanetData()
{
	string output = "// PLANET LIST" + SLASHES;
	List<string> planetData = new List<string>();
	planetData.Add("Charted: " + _planetList.Count + "	  Uncharted: " + _unchartedList.Count);

	if(_planetList.Count > 0)
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
	string output = "// MAP DATA";

	List<string> mapData = new List<string>();

	if(_mapBlocks.Count > 0)
	{
		StarMap map = _mapList[0];
		output += ": " + _mapBlocks[0].CustomName + SLASHES;

		if(_statusMessage != "")
		{
			output += "\n" + _statusMessage;
		}

		string hidden = "	Mode: "	 + map.mode + "	   Hidden:";

		if(!_showMapParameters)
		{
			hidden += " Stat-Bars ";
		}

		if(!_gpsActive)
		{
			hidden += " Waypoints ";
		}

		if(!_showNames)
		{
			hidden += " Names";
		}

		mapData.Add(hidden);
		mapData.Add("	Center: " + Vector3ToString(map.center));
		mapData.Add("	Azimuth: " + map.azimuth + "°	Altitude: " + map.altitude * -1 + "°");
		mapData.Add("	Focal Length: " + abbreviateValue(map.focalLength));
		mapData.Add("	Rotational Radius: " + abbreviateValue(map.rotationalRadius));
		mapData.Add("	Brightness: " + _brightnessMod);
		mapData.Add("	Dimensions: " + map.viewport.Height + " x " + map.viewport.Width);

		output += ScrollToString(mapData);
	}
	else
	{
		output += SLASHES + "\n\n NO MAP BLOCKS TO DISPLAY!!!";
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
void NextPage()
{
	_pageIndex++;
	if(_pageIndex >= DATA_PAGES)
	{
		_pageIndex = 0;
	}
}


// PREVIOUS PAGE //
void PreviousPage()
{
	if(_pageIndex == 0)
	{
		_pageIndex = DATA_PAGES;
	}
	_pageIndex--;
}


// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
	output += ";" + activity;

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
		_cycleStep = CYCLE_LENGTH;

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
		
		if(_planetList.Count > 0)
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
	}
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
	Echo("--A--");
	foreach(Planet planet in _planetList)
	{
		Echo("--B: " + planet.name);
		Echo(Vector3ToString(planet.transformedCoords[map.number]));
		if(planet.transformedCoords[map.number].Z > map.focalLength)
		{
			displayPlanets.Add(planet);
		}
	}
	Echo("--C--");	
	DrawPlanets(displayPlanets, map);

	Echo("--D--");

	//DRAW WAYPOINTS & UNCHARTED SURFACE POINTS
	if(map.showGPS)
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
	if(_planetList.Count > 0)
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