MyIni _mapLog = new MyIni();
string _lcdName;
string _refName;
string _previousCommand;
int _lcdIndex;
int _azSpeed;
int _planetIndex;
bool _gpsActive;
bool _showMapParameters;
bool _showPlanetNames;
bool _lightOn;
int _cycleStep;
Vector3 _trackSpeed;
Vector3 _origin = new Vector3(0,0,0);
//string _planetLog;
//string _gpsLog;
//string _unchartedLog;
List<IMyTerminalBlock> _mapBlocks = new List<IMyTerminalBlock>();
List<Planet> _planetList = new List<Planet>();
List<Planet> _unchartedList = new List<Planet>();
List<Waypoint> _waypointList = new List<Waypoint>();

IMyTextSurface _drawingSurface;
IMyTerminalBlock _refBlock;
RectangleF _viewport;


const int MIN_SCALE = 125;
const int SHIP_SCALE = 24;
const int MAX_PITCH = 90; // Maximum (+/-) value of map pitch.
const int ANGLE_STEP = 5; // Temporary step for rotation commands.
const float MOVE_STEP = 5000; // Temporary step for translation (move) commands.
const int ZOOM_MAX = 1000000000; // Max value for Depth of Field
const int ZOOM_STEP = 2; // Factor By which map is zoomed in and out.
const int MARKER_WIDTH = 8; // Width of GPS Markers
const float DIAMETER_MIN = 6; //Minimum Diameter For Distant Planets
const int HASH_LIMIT = 125; //Minimum diameter size to print Hashmarks on planet.
const int DV_RADIUS = 262144; //Default View Radius
const int DV_DOF = 256; //Default Depth of Field
const int DV_ALTITUDE = -15; //Default Altitude (angle)
const int CYCLE_LENGTH = 5; // Number of runs for delayed cycle
const int BAR_HEIGHT = 20; //Default height of parameter bars
const int TOP_MARGIN = 8; // Margin for top and bottom of frame
const int SIDE_MARGIN = 15; // Margin for sides of frame
const int MAX_VALUE = 1073741824; //General purpose MAX value = 2^30

const string DEFAULT_SETTINGS = "[Map Settings]\nLCD_Name=[MAP]\nLCD_Index=0\nReference_Name=[Reference]\nPlanet_List=\nWaypoint_List=\n";
//const string DEFAULT_SETTINGS = "[Map Settings]\nLCD_Name=<name>\nLCD_Index=0\nReference_Name=<name>\n";
string _defaultDisplay = "[mapDisplay]\nCenter=(0,0,0)\nMode=WORLD\nDepthOfField="+DV_DOF+"\nRotationalRadius="+DV_RADIUS
                                +"\nAzimuth=0\nAltitude="+DV_ALTITUDE;


readonly String[,] AXIS = new String[6,6] {{"E","-X -Y", "-X -Z", "E", "-X Y", "-X Z"},
                                                                    {"-Y -X", "E", "-Y -Z", "-Y X", "E", "-Y Z"},
                                                                    {"-Z -X", "-Z -Y", "E", "-Z X", "-Z Y", "E"},
                                                                    {"E", "X -Y", "X -Z", "E", "X Y", "X Z"},
                                                                    {"Y -X", "E", "Y -Z", "Y X", "E", "Y Z"},
                                                                    {"Z -X", "Z -Y", "E", "Z X", "Z Y", "E"}};

// CLASSES //////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////
// STAR MAP //
////////////////
public class StarMap
{
    public Vector3 center;
    public string mode;
    public int altitude;
    public int azimuth;
    public int rotationalRadius;
    public int depthOfField;
    public int azSpeed; // Rotational velocity of Azimuth
    
    public StarMap()
    {
        this.azSpeed = 0;
    }
    
    public void SetCenter(Vector3 coordinate)
    {
        this.center = coordinate;
    }
    
    public Vector3 GetCenter()
    {
        return this.center;
    }
    
    public void SetMode(string displayMode)
    {
        this.mode = displayMode;
    }
    
    public string GetMode()
    {
        return this.mode;
    }
    
    public void SetRotationalRadius(int value)
    {
        this.rotationalRadius = value;
    }
    
    public int GetRotationalRadius()
    {
        return this.rotationalRadius;
    }
    
    public void SetDepthOfField(int value)
    {
        this.depthOfField = value;
    }
    
    public int GetDepthOfField()
    {
        return this.depthOfField;
    }
    
    public void SetAzimuth(int value)
    {
        this.azimuth = value;
    }
    
    public int GetAzimuth()
    {
        return this.azimuth;
    }
    
    public void SetAltitude(int value)
    {
        this.altitude = value;
    }
    
    public int GetAltitude()
    {
        return this.altitude;
    }
    
    public void SetAzSpeed(int value)
    {
        this.azSpeed = value;
    }
    
    public int GetAzSpeed()
    {
        return this.azSpeed;
    }
    
    public void yaw(int angle)
    {
        if(this.mode.ToUpper() != "PLANET")
        {
           this.azimuth = degreeAdd(this.azimuth, angle); 
        }  
    }
    
    public void pitch(int angle)
    {
        if(this.mode.ToUpper() != "PLANET")
        {
            int newAngle = degreeAdd(this.altitude, angle);
        
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
    }
}


/////////////
// PLANET //
/////////////
public class Planet
{
    //public int designation;
    public String name;
    public Vector3 center;
    public Vector3 transformedCenter;
    public float radius;
    public String color;
    public Vector3 point1;
    public Vector3 point2;
    public Vector3 point3;
    public Vector3 point4;
    public Vector2 mapPos;
    public bool isCharted;
    

    public Planet(String planetString)
    {
        

        //this.designation = planetList.Count()+1;
        string[] planetData = planetString.Split(';');
        
        this.SetName(planetData[0]);
        
        if(planetData.Length < 8)
        {
            return;
        }
		        

        this.SetColor(planetData[3]);
	        
        if(planetData[1] != "")
        {
            this.SetCenter(StringToVector3(planetData[1]));
        }
        
        if(planetData[2] != "")
        {
            this.SetRadius(float.Parse(planetData[2]));
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
    /*
    public void SetDesignation(int des)
    {
        	designation = des;
    }
    */
    public void SetName(String arg)
    {
        name = arg;
    }
    
    public void SetCenter(Vector3 cent)
    {
        center = cent;
    }

    public void SetRadius(float rad)
    {
        radius = rad;
    }
    
    public void SetTransformedCenter(Vector3 vec)
    {
        this.transformedCenter = vec;
    }

    public void SetColor(String col)
    {
        color = col;
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
	
    public void SetMapPos(Vector2 position)
    {
        this.mapPos = position;
    }
    /*
    public int GetDesignation()
    {
        return designation;
    }
    */
	
    public String GetName()
    {
        return name;
    }

    public Vector3 GetCenter()
    {
        return center;
    }
    
    public Vector3 GetTransformedCenter()
    {
        return this.transformedCenter;
    }

    public float GetRadius()
    {
        return radius;
    }

    public String GetColor()
    {
        return color;
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
    
    public Vector2 GetMapPos()
    {
        return this.mapPos;
    }

    public override String ToString()
    {
        String[] planetData = new String[8];
		
        planetData[0] = this.GetName();
        planetData[1] = Vector3ToString(this.GetCenter());
	
        float radius = this.GetRadius();
        if(radius>0)
        {
            planetData[2] = radius.ToString();
           // planetData[0] = "P" + this.GetDesignation();
        }
        else
        {
            planetData[2] = "";
           // planetData[0] = "U" + this.GetDesignation();
        }

        planetData[3] = this.GetColor();
        
		for(int c = 4; c<8; c++)
        {
            if(this.GetPoint(c-3) != Vector3.Zero)
            {
                planetData[c] = Vector3ToString(this.GetPoint(c-3));
            }
        }

        //String planetString = BuildEntries(planetData, 0, 9, ";");
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
        float xCenter = center.GetDim(0);
        float yCenter = center.GetDim(1);
        float zCenter = center.GetDim(2);

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
        this.center = newCenter;

        double newRad =  Math.Sqrt(detD*detD + detE*detE + detF*detF - 4*detG)/2;
        this.radius = (float) newRad;

        this.SetMajorAxes();
    }
}

////////////////
// WAYPOINT //
////////////////
public class Waypoint
{
	//public int designation;
    public String name;
    public Vector3 location;
    public Vector3 transformedLocation;
    public String marker;
    public bool isActive;

    public Waypoint()
    {

    }	
	/*
    public void SetDesignation(int des)
    {
        designation = des;
    }
	*/
    public void SetName(String arg)
    {
        name = arg;
    }

    public void SetLocation(Vector3 coord)
    {
        location = coord;
    }
    
    public void SetMarker(String arg)
    {
        marker = arg;
    }
    
    public void Activate()
    {
        isActive =true;
    }
    
    public void Deactivate()
    {
        isActive =false;
    }
    /*
    public int GetDesignation()
    {
        return designation;
    }
	*/
    public String GetName()
    {
        return name;
    }

    public Vector3 GetLocation()
    {
        return location;
    }
    
    public String GetMarker()
    {
        return marker;
    }
    /*
    public override String ToString()
    {
        String wpString = "G" + this.GetDesignation() + "|" + this.GetName() + "|" + Vector3ToString(this.GetLocation());
        return wpString;
    }
    */
}




// PROGRAM ///////////////////////////////////////////////////////////////////////////////////////////////

public Program()
{
    //Load Saved Variables
    if(Storage.Length > 0)
    {
      //Previously Compiled
      String[] loadData = Storage.Split('\n');
      _planetIndex = int.Parse(loadData[0]);
      _gpsActive = bool.Parse(loadData[1]);
      _azSpeed = int.Parse(loadData[2]);
      _trackSpeed = StringToVector3(loadData[3]);
      _showMapParameters = bool.Parse(loadData[4]);
      _showPlanetNames = bool.Parse(loadData[5]);
    }
    else
    {
        //Newly Compiled
        _planetIndex = 0;
        _gpsActive = true;
        _azSpeed = 0;
        _trackSpeed = new Vector3(0,0,0);
        _showMapParameters = true;
        _showPlanetNames = true;
    }

    // Start with indicator light on.
    _lightOn = true;
    _cycleStep = CYCLE_LENGTH;
    
    string oldData = Me.CustomData;
    string newData = DEFAULT_SETTINGS;

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
    _lcdName =  _mapLog.Get("Map Settings", "LCD_Name" ).ToString();

    //Index of screen to print map to.
    _lcdIndex = _mapLog.Get("Map Settings","LCD_Index").ToUInt16();
    
    //Name of reference block
    _refName = _mapLog.Get("Map Settings", "Reference_Name").ToString();

    string planetData = _mapLog.Get("Map Settings", "Planet_List").ToString();
    //Echo(planetData);
    string [] mapEntries = planetData.Split('\n');
    for(int p = 0; p < mapEntries.Length; p++)
    {
        if(mapEntries[p].Contains(";"))
        {
            Planet planet = new Planet(mapEntries[p]);
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
    for(int g = 0; g < gpsEntries.Length; g++)
    {
        if(gpsEntries[g].Contains(";"))
        {
           Waypoint waypoint = StringToWaypoint(gpsEntries[g]);
           _waypointList.Add(waypoint);
        }
    }


    if(_lcdName == "" || _lcdName == "<name>")
    {
       Echo("No LCD specified!!!");
    }
    else
    {
        GridTerminalSystem.SearchBlocksOfName(_lcdName, _mapBlocks);
        IMyTextSurfaceProvider mapBlock = _mapBlocks[0] as IMyTextSurfaceProvider;
        _drawingSurface = mapBlock.GetSurface(_lcdIndex);
        Echo("MAP BLOCKS:");
        Echo(_mapBlocks[0].CustomName);
        
        StarMap myMap = parametersToMap(_mapBlocks[0]);
        updateMap(myMap);
    }
    
    if(_refName == "" || _refName == "<name>")
    {
        Echo("WARNING: No Reference Block Specified!\nMay Result in false orientation!");
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
            Echo("No Block containing " + _refName + " found.");
            _refBlock = Me as IMyTerminalBlock;
        }
    }
    
    // Calculate the viewport offset by centering the surface size onto the texture size
    _viewport = new RectangleF(
        (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
        _drawingSurface.SurfaceSize
    );



    _previousCommand = "NEWLY LOADED";


    

     // Set the continuous update frequency of this script
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save()
{
    String saveData = _planetIndex.ToString() + "\n" + _gpsActive.ToString() + "\n" + _azSpeed.ToString();
    saveData = saveData + "\n" + Vector3ToString(_trackSpeed) + "\n" + _showMapParameters.ToString() + "\n" + _showPlanetNames.ToString();
    
    Storage = saveData;
}

// MAIN ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public void Main(string argument)
{
    Echo("Active LCD: " + _lcdName);
    Echo("LCD Index: " + _lcdIndex);
    Echo(_previousCommand);

    if(_planetList.Count > 0)
    {
        Echo("Planet Count: " + _planetList.Count);
        Planet planet = _planetList[_planetList.Count - 1];
    //    Echo("C: " + Vector3ToString(planet.center));
    //    Echo("R: " + planet.radius.ToString());
    }
    else
    {
        Echo("No Planets Logged!");
    }
    
    if(_waypointList.Count > 0)
    {
        Echo("GPS Count: " + _waypointList.Count);
        Waypoint waypoint = _waypointList[_waypointList.Count - 1];
    //    Echo("C: " + Vector3ToString(planet.center));
    //    Echo("R: " + planet.radius.ToString());
    }
    else
    {
        Echo("No Waypoints Logged!");
    }  
   


    if(_mapBlocks.Count >0)
    {
        // Begin a new frame
        MySpriteDrawFrame frame = _drawingSurface.DrawFrame();
        
        
        IMyTerminalBlock mapBlock = _mapBlocks[0] as IMyTerminalBlock;
        StarMap myMap = parametersToMap(mapBlock);

        //Add rotational Speed
        if(myMap.mode.ToUpper() != "PLANET")
        {
            myMap.azimuth = degreeAdd(myMap.azimuth, _azSpeed);
        }
        
        //Add Translational Speed
        if(myMap.mode.ToUpper() == "WORLD")
        {
            myMap.center += rotateMovement(_trackSpeed, myMap);
        }
        
        
        cycleExecute(myMap);
        
        if (_previousCommand == "NEWLY LOADED" || _azSpeed != 0 || _trackSpeed != _origin)
        {
            updateMap(myMap);
            mapToParameters(myMap, mapBlock);
        }

        if (argument != "")
        {
            string [] args = argument.Split(' ');
            string command = args[0].ToUpper();
            string entityName = "";

            _previousCommand = "Command: " + argument;
            
            // If there are multiple words in the argument. Combine the latter words into the entity name.
            if(args.Length > 1)
            {
                entityName = args[1];
                if(args.Length > 2)
                {
                    for(int q = 2; q < args.Length;  q++)
                    {
                        entityName += " " + args[q];
                    }
                }
            }
            
            
            switch(command)
            {
                case "ZOOM_IN":
                    zoomIn(myMap);
                    break;
                case "ZOOM_OUT":
                    zoomOut(myMap);
                    break;
                case "MOVE_LEFT":
                    moveLeft(myMap);
                    break;
                case "MOVE_RIGHT":
                    moveRight(myMap);
                    break;
                case "MOVE_UP":
                    moveUp(myMap);
                    break;
                case "MOVE_DOWN":
                    moveDown(myMap);
                    break;
                case "MOVE_FORWARD":
                    moveForward(myMap);
                    break;
                case "MOVE_BACKWARD":
                    moveBackward(myMap);
                    break;
                case "DEFAULT_VIEW":
                    DefaultView(myMap);
                    break;
                case "ROTATE_LEFT":
                    myMap.yaw(ANGLE_STEP);
                    break;
                case "ROTATE_RIGHT":
                    myMap.yaw(-ANGLE_STEP);
                    break;
                case "ROTATE_UP":
                    myMap.pitch(-ANGLE_STEP);
                    break;
                case "ROTATE_DOWN":
                    myMap.pitch(ANGLE_STEP);
                    break;
                case "GPS_ON":
                    _gpsActive = true;
                    break;
                case "GPS_OFF":
                    _gpsActive = false;
                    break;
                case "TOGGLE_GPS":
                    _gpsActive = !_gpsActive;
                    break;
                case "NEXT_PLANET":
                    NextPlanet(myMap);
                    break;
                case "PREVIOUS_PLANET":
                    PreviousPlanet(myMap);
                    break;
                case "SPIN_LEFT":
                    _azSpeed += ANGLE_STEP;
                    break;
                case "SPIN_RIGHT":
                    _azSpeed -= ANGLE_STEP;
                    break;
                case "STOP":
                    _azSpeed = 0;
                    _trackSpeed = _origin;
                    break;
                case "TRACK_LEFT":
                    _trackSpeed += new Vector3(MOVE_STEP, 0, 0);
                    break;
                case "TRACK_RIGHT":
                    _trackSpeed -= new Vector3(MOVE_STEP, 0, 0);
                    break;
                case "TRACK_UP":
                    _trackSpeed += new Vector3(0,MOVE_STEP,0);
                    break;
                case "TRACK_DOWN":
                    _trackSpeed -= new Vector3(0,MOVE_STEP,0);
                    break;
                case "TRACK_FORWARD":
                    _trackSpeed += new Vector3(0,0,MOVE_STEP);
                    break;
                case "TRACK_BACKWARD":
                    _trackSpeed -= new Vector3(0,0,MOVE_STEP);
                    break;
                case "SHOW_NAMES":
                    _showPlanetNames = true;
                    break;
                case "HIDE_NAMES":
                    _showPlanetNames = false;
                    break;
                case "TOGGLE_NAMES":
                    _showPlanetNames = !_showPlanetNames;
                    break;
                case "SHOW_INFO":
                    _showMapParameters = true;
                    break;
                case "HIDE_INFO":
                    _showMapParameters = false;
                    break;
                case "TOGGLE_INFO":
                    _showMapParameters = !_showMapParameters;
                    break;
                case "WORLD_MODE":
                    myMap.mode = "WORLD";
                    break;
                case "SHIP_MODE":
                    myMap.mode = "SHIP";
                    break;
                case "PLANET_MODE":
                    myMap.mode = "PLANET";
                    break;
                case "PREVIOUS_MODE":
                    PreviousMode(myMap);
                    break;
                case "NEXT_MODE":
                    NextMode(myMap);
                    break;
                case "DECREASE_RADIUS":
                    reduceRadius(myMap);
                    break;
                case "INCREASE_RADIUS":
                    increaseRadius(myMap);
                    break;
                case "CENTER":
                    myMap.center = _refBlock.GetPosition();
                    break;
                case "DEACTIVATE":
                    SetWaypointState(entityName, 0);
                    break;
                case "ACTIVATE":
                    SetWaypointState(entityName, 1);
                    break;
                case "TOGGLE":
                    SetWaypointState(entityName, 2);
                    break;
                case "DELETE":
                    SetWaypointState(entityName, 3);
                    break;
                case "LOG_GPS":
                    LogWaypoint(entityName, "GPS");
                    break;
                case "LOG_BASE":
                    LogWaypoint(entityName, "BASE");
                    break;
                case "LOG_STATION":
                    LogWaypoint(entityName, "STATION");
                    break;                    
                case "LOG_LANDMARK":
                    LogWaypoint(entityName, "LANDMARK");
                    break;
                case "LOG_HAZARD":
                    LogWaypoint(entityName, "HAZARD");
                    break;
                case "NEW_PLANET":
                    NewPlanet(entityName);
                    break;
                case "LOG_NEXT":
                    LogNext(entityName);
                    break;
                default:
                    _previousCommand = "UNRECOGNIZED COMMAND!";
                    break;
            }
            
            updateMap(myMap);
            mapToParameters(myMap, mapBlock);
        }


        // All sprites must be added to the frame here
        DrawSprites(ref frame, myMap);


        // We are done with the frame, send all the sprites to the text panel
        frame.Dispose();
        
    }
    else
    {
        Echo("No Displays found!");
    }   
}


// VIEW FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////
// PARAMETERS TO MAP //
////////////////////////////
public StarMap parametersToMap(IMyTerminalBlock mapBlock)
{
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

    StarMap map = new StarMap();

    map.center = StringToVector3(lcdIni.Get("mapDisplay","Center").ToString());
    map.depthOfField = lcdIni.Get("mapDisplay","DepthOfField").ToInt32();
    map.rotationalRadius = lcdIni.Get("mapDisplay","RotationalRadius").ToInt32();
    map.azimuth = lcdIni.Get("mapDisplay", "Azimuth").ToInt32();
    map.altitude = lcdIni.Get("mapDisplay", "Altitude").ToInt32();
    map.mode = lcdIni.Get("mapDisplay","Mode").ToString();

    
    return map;
}


////////////////////
// DATA TO LOG //
////////////////////
public void DataToLog()
{
    MyIni mapIni = new MyIni();
    
    MyIniParseResult result;
    if (!mapIni.TryParse(Me.CustomData, out result)) 
        throw new Exception(result.ToString());
    
    if(_waypointList.Count > 0)
    {
        String waypointData = "";
        for(int w = 0; w < _waypointList.Count; w++)
        {
            waypointData += WaypointToString(_waypointList[w]) + "\n";
        }
        mapIni.Set("Map Settings", "Waypoint_List",waypointData);
    }
    
    String planetData = "";
    if(_planetList.Count > 0)
    {
        for(int p = 0; p < _planetList.Count; p++)
        {
            planetData += _planetList[p].ToString() + "\n";
        }
    }
    
    if(_unchartedList.Count > 0)
    {
        for(int u = 0; u < _unchartedList.Count; u++)
        {
            planetData += _unchartedList[u].ToString() + "\n";
        }
    }
    
    if(planetData != "")
    {
        mapIni.Set("Map Settings", "Planet_List", planetData);
    }
    
    Me.CustomData = mapIni.ToString();
}


////////////////////////////
// MAP TO PARAMETERS // Writes map object to CustomData of Display Block
////////////////////////////
public void mapToParameters(StarMap map, IMyTerminalBlock mapBlock)
{
    MyIni lcdIni = new MyIni();
    
    MyIniParseResult result;
    if (!lcdIni.TryParse(mapBlock.CustomData, out result)) 
        throw new Exception(result.ToString());
    
    lcdIni.Set("mapDisplay", "Center", Vector3ToString(map.center));
    lcdIni.Set("mapDisplay", "DepthOfField", map.depthOfField);
    lcdIni.Set("mapDisplay", "RotationalRadius", map.rotationalRadius);
    lcdIni.Set("mapDisplay", "Azimuth", map.azimuth);
    lcdIni.Set("mapDisplay", "Altitude", map.altitude);
    lcdIni.Set("mapDisplay", "Mode", map.mode);

    
    mapBlock.CustomData = lcdIni.ToString();
}


/////////////////////
// LOG WAYPOINT //
/////////////////////
public void LogWaypoint(String waypointName, String markerType)
{
    if(waypointName == "")
    {
        Echo("No Waypoint Name Provided! Please Try Again.");
        return;
    }
    
    Waypoint waypoint = new Waypoint();
    waypoint.name = waypointName;
    waypoint.location = _refBlock.GetPosition();
    waypoint.marker = markerType;
    waypoint.isActive = true;
    
    _waypointList.Add(waypoint);
    DataToLog();
}


///////////////////////////
// SET WAYPOINT STATE //
///////////////////////////
public void SetWaypointState(String waypointName, int state)
{
    if(waypointName == "")
    {
        Echo("No waypoint name provided for activation command!");
        return;
    }
    
    List<Waypoint> nameList = new List<Waypoint>();
    for(int w = 0; w < _waypointList.Count; w++)
    {
        if(_waypointList[w].name.ToUpper() == waypointName.ToUpper())
        {
            nameList.Add(_waypointList[w]);
        }        
    }
    
    if(nameList.Count == 0)
    {
        Echo("No Waypoints found with name: " + waypointName + "!");
        return;
    }
    
    if(nameList.Count > 1)
    {
        Echo("Multiple Waypoints with name: " + waypointName + "!\nPlease rename additional waypoints!");
    }
    
    Waypoint waypoint = nameList[0];
    
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
            break;
        default:
            Echo("Invalide waypoint state int!");
            break;
    }
    
    DataToLog();
}


///////////////////
// NEW PLANET // 
///////////////////
public void NewPlanet(String planetName)
{
    planetName += ";;;;;;;";
    Planet planet = new Planet(planetName);
    planet.SetPoint(1, _refBlock.GetPosition());
    
    _unchartedList.Add(planet);
    DataToLog();
}


/////////////////
// LOG NEXT //
/////////////////
public void LogNext(String planetName)
{
    List<Planet> nameList = new List<Planet>();
    if(_unchartedList.Count > 0)
    {
        for(int u = 0; u < _unchartedList.Count; u++)
        {
            if(_unchartedList[u].name.ToUpper() == planetName.ToUpper())
            {
                nameList.Add(_unchartedList[u]);
            }
        }
        
        if(nameList.Count == 0)
        {
            Echo("No Planet " + planetName + " Found!");
            return;
        }
        
        if(nameList.Count > 1)
        {
            Echo("Multiple instances of " + planetName + "Found!\nConsider renaming or deleting.");
        }
        
        Planet planet = nameList[0];
        String[] planetData = planet.ToString().Split(';');
        
        if(planetData[4] == "")
        {
            planet.SetPoint(1, _refBlock.GetPosition());
        }
        else if(planetData[5] == "")
        {
            planet.SetPoint(2, _refBlock.GetPosition());
        }
        else if(planetData[6] == "")
        {
            planet.SetPoint(3, _refBlock.GetPosition());
        }
        else
        {
            planet.SetPoint(4, _refBlock.GetPosition());
            planet.CalculatePlanet();
            _planetList.Add(planet);
            _unchartedList.Remove(planet);
        }
        
        DataToLog();
    }
}


////////////////
// ZOOM IN //      Increase Map's Depth of Field
////////////////

void zoomIn(StarMap map)
{ 
    int doF = map.depthOfField;
    int newScale = doF*ZOOM_STEP;
    if (newScale < ZOOM_MAX)
    {
        doF = newScale;
    }
    else
    {
        doF = ZOOM_MAX;
    }

    map.depthOfField = doF;
}


////////////////
// ZOOM OUT //    Decrease Map's Depth of Field
////////////////
void zoomOut(StarMap map)
{ 
    int doF = map.depthOfField;
    int newScale = doF/ZOOM_STEP;
    
    // Make sure no rollover error.
    if (newScale > 1)
    {
        doF = newScale;
    }
    else
    {
        doF = 1;
    }

    map.depthOfField = doF;
}


//////////////////////
// REDUCE RADIUS //
//////////////////////
void reduceRadius(StarMap map)
{
    map.rotationalRadius /= 2;
    
    if(map.rotationalRadius < map.depthOfField)
    {
        map.rotationalRadius = map.depthOfField;
    }
}


////////////////////////
// INCREASE RADIUS //
////////////////////////
void increaseRadius(StarMap map)
{
    map.rotationalRadius *= 2;
    
    if(map.rotationalRadius > MAX_VALUE)
    {
        map.rotationalRadius =  MAX_VALUE;
    }
}


/////////////////
// MOVE LEFT //
/////////////////
void moveLeft(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {
        Vector3 moveVector = new Vector3(MOVE_STEP,0,0);
        map.center += rotateMovement(moveVector, map);
    }
}


//////////////////
// MOVE RIGHT //
//////////////////
void moveRight(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {
        Vector3 moveVector = new Vector3(MOVE_STEP,0,0);
        map.center -= rotateMovement(moveVector, map);
    }
}


//////////////////
// MOVE UP //
//////////////////
void moveUp(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {
        Vector3 moveVector = new Vector3(0,MOVE_STEP,0);
        map.center += rotateMovement(moveVector, map);
    }
}


//////////////////
// MOVE DOWN //
//////////////////
void moveDown(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {
        Vector3 moveVector = new Vector3(0,MOVE_STEP,0);
        map.center -= rotateMovement(moveVector, map);
    }
}


/////////////////////
// MOVE FORWARD //
/////////////////////
void moveForward(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {    
        Vector3 moveVector = new Vector3(0,0,MOVE_STEP);
        map.center += rotateMovement(moveVector, map);
    }
}


//////////////////////
// MOVE BACKWARD //
//////////////////////
void moveBackward(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {    
        Vector3 moveVector = new Vector3(0,0,MOVE_STEP);
        map.center -= rotateMovement(moveVector, map);
    }
}


/////////////////
// NEXT MODE //
/////////////////
void NextMode(StarMap map)
{
    if(map.mode.ToUpper() == "WORLD")
    {
        map.mode = "SHIP";
    }
    else if(map.mode.ToUpper() == "SHIP")
    {
        map.mode = "PLANET";
    }
    else
    {
        map.mode = "WORLD";
    }
}


//////////////////////
// PREVIOUS MODE //
//////////////////////
void PreviousMode(StarMap map)
{
    if(map.mode.ToUpper() == "PLANET")
    {
        map.mode = "SHIP";
    }
    else if(map.mode.ToUpper() == "WORLD")
    {
        map.mode = "PLANET";
    }
    else
    {
        map.mode = "WORLD";
    }
}


//////////////////////
// SHIP TO PLANET //   Aligns the map so that the ship appears above the center of the planet.
//////////////////////
void ShipToPlanet(Planet planet, StarMap map)
{    
    Vector3 shipVector = _refBlock.GetPosition() - planet.center;
    float magnitude = Vector3.Distance(_refBlock.GetPosition(), planet.center);
    
    float azAngle = (float) Math.Atan2(shipVector.GetDim(2),shipVector.GetDim(0));
    float altAngle = (float) Math.Asin(shipVector.GetDim(1)/magnitude);
    
    map.center = planet.center;
    map.azimuth = degreeAdd((int) toDegrees(azAngle),90);
    map.altitude = (int) toDegrees(-altAngle);
}


////////////////////
// DEFAULT VIEW //
////////////////////
void DefaultView(StarMap map)
{
    map.mode = "WORLD";
    
    map.center = new Vector3(0,0,0);
    map.depthOfField = DV_DOF;
    
    if(_viewport.Width > 500)
    {
        map.depthOfField *= 4;
    }
    
    map.rotationalRadius = DV_RADIUS;
    map.azimuth = 0;
    map.altitude = DV_ALTITUDE;
}


////////////////////
// NEXT PLANET //
////////////////////
void NextPlanet(StarMap map)
{
    if(_planetList.Count > 1)
    {
        DefaultView(map);
        
        _planetIndex++;
        if(_planetIndex >= _planetList.Count)
        {
            _planetIndex = 0;
        }
    }
    
    Planet planet = _planetList[_planetIndex];
    map.center = planet.center;
    if(planet.radius < 40000)
    {
        map.depthOfField *= 4;
    }
}


////////////////////////
// PREVIOUS PLANET //
////////////////////////
void PreviousPlanet(StarMap map)
{
    if(_planetList.Count > 1)
    {
        DefaultView(map);
        
        _planetIndex--;
        if(_planetIndex < 0)
        {
            _planetIndex = _planetList.Count - 1;
        }

    }
    
    Planet planet = _planetList[_planetIndex];
    map.center = planet.center;
    if(planet.radius < 40000)
    {
        map.depthOfField *= 4;
    }
}



// DRAWING FUNCTIONS //////////////////////////////////////////////////////////////////////////////////////////////////////////


/////////////////
// DRAW SHIP //
/////////////////
public void DrawShip(ref MySpriteDrawFrame frame, StarMap map, List<Planet> displayPlanets)
{
    // SHIP COLORS
    Color bodyColor = Color.LightGray;
    Color aftColor = new Color(180,60,0);
    Color plumeColor = Color.Yellow;
    
    
    Vector3 transformedShip = transformVector(_refBlock.GetPosition(), map);
    Vector2 shipPos = PlotObject(transformedShip,map);
    float shipX = shipPos.X;
    float shipY = shipPos.Y;
    
    int vertMod = 0;
    if(_showMapParameters)
    {
        vertMod = BAR_HEIGHT;
        
        if(_viewport.Width > 500)
        {
            vertMod *= 2; 
        }
    }
    
    
    bool offZ = transformedShip.GetDim(2) < map.depthOfField;
    bool leftX = shipX < -_viewport.Width/2 || (offZ && shipX < 0);
    bool rightX = shipX > _viewport.Width/2 || (offZ && shipX >= 0);
    bool aboveY = shipY < -_viewport.Height/2 + vertMod || (offZ && shipY < 0);
    bool belowY = shipY > _viewport.Height/2 - vertMod || (offZ && shipX >= 0);
    bool offX = leftX || rightX;
    bool offY = aboveY || belowY;
    

    
    if(offZ || offX || offY)
    {
        float posX;
        float posY;
        float rotation = 0;
        int pointerScale = SHIP_SCALE/2;
        
        if(leftX)
        {
            posX = 0;
            rotation = (float) Math.PI*3/2;
        }
        else if(rightX)
        {
            posX = _viewport.Width - pointerScale;
            rotation = (float) Math.PI/2;
        }
        else
        {
            posX = _viewport.Width/2 + shipX - pointerScale/2;
        }
        
        if(aboveY)
        {
            posY = vertMod + TOP_MARGIN  + (_viewport.Width - _viewport.Height)/2; //1.75f * SHIP_SCALE + 
            rotation = 0;
        }
        else if(belowY)
        {
            posY = _viewport.Height - vertMod - TOP_MARGIN  + (_viewport.Width - _viewport.Height)/2; //+ 1.25f * SHIP_SCALE 
            rotation = (float) Math.PI;
        }
        else
        {
            posY = _viewport.Height/2 + shipY + (_viewport.Width - _viewport.Height)/2; //2
        }
        
        if(offX && offY)
        {
            rotation = (float) Math.Atan2(shipY, shipX);
        }
        
        if(offZ)
        {
            bodyColor = Color.DarkRed;
        }
        else
        {
            bodyColor = Color.DodgerBlue;
        }
        
        //OFF MAP POINTER
        var sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Triangle",
            Position = new Vector2(posX - 2, posY),
            Size=  new Vector2(pointerScale + 4, pointerScale + 4),
            RotationOrScale=rotation,
            Color = Color.Black,
        };
        frame.Add(sprite);
        
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Triangle",
            Position = new Vector2(posX, posY),
            Size=  new Vector2(pointerScale, pointerScale),
            RotationOrScale=rotation,
            Color = bodyColor,
        };
        frame.Add(sprite);
        
        
        
        //Echo("SHIP OFF SCREEN");
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

        
        
        // Ship Heading
        float headingX = heading.GetDim(0);
        float headingY = heading.GetDim(1);
        float headingZ = heading.GetDim(2);
        
        //Get the Ratio of direction Vector's apparent vs actual magnitudes.
        float shipLength = (float) 1.33 * SHIP_SCALE *(float) Math.Sqrt(headingX * headingX + headingY * headingY) / (float) Math.Sqrt (headingX * headingX + headingY * headingY + headingZ * headingZ);
        
        
        //float shipLength = heading.Length();//Math.Sqrt(headingX * headingX + headingY * headingY);
        
        float shipAngle = (float) Math.Atan2(headingX, headingY) * -1;//(float) Math.PI - 
        
        
        

        position += _viewport.Center;
        
        
        Vector2 offset = new Vector2( (float) Math.Sin(shipAngle), (float) Math.Cos(shipAngle) * -1);
        position += offset * shipLength/4;
        position -= new Vector2(SHIP_SCALE/2 ,0); // Center width of ship over position
        Vector2 startPosition = position;
        
        position -= new Vector2(2,0);
        
        //Outline
        var sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Triangle",
            Position = position,
            Size=  new Vector2(SHIP_SCALE+4, shipLength+4),
            RotationOrScale=shipAngle,
            Color = Color.Black,
        };
        frame.Add(sprite);
        
        float aftHeight = SHIP_SCALE - shipLength/(float) 1.33;
        
        position = startPosition;
        position -= offset * shipLength/2;
        position -= new Vector2(2,0);
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Position = position,
            Size=  new Vector2(SHIP_SCALE+4, aftHeight+4),
            RotationOrScale=shipAngle,
            Color = Color.Black,
        };
        frame.Add(sprite);
        
        
        
        position = startPosition;
        // Ship Body
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Triangle",
            Position = position,
            Size=  new Vector2(SHIP_SCALE, shipLength),
            RotationOrScale=shipAngle,
            Color = bodyColor,
        };
        frame.Add(sprite);
        
        
        // Aft Ellipse

        
        if(headingZ < 0)
        {
            aftColor = bodyColor;
            plumeColor = bodyColor;
        }
        
        position -= offset * shipLength/2;
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Position = position,
            Size=  new Vector2(SHIP_SCALE, aftHeight),
            RotationOrScale=shipAngle,
            Color = aftColor,
        };
        frame.Add(sprite);
        
        
        position -= offset * shipLength/16;
        position += new Vector2(SHIP_SCALE/6, 0);
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Position = position,
            Size=  new Vector2(SHIP_SCALE*0.67f, aftHeight*0.67f),
            RotationOrScale=shipAngle,
            Color = plumeColor,
        };
        frame.Add(sprite);
    }
}


///////////////////
// PLOT OBJECT //
///////////////////
public Vector2 PlotObject(Vector3 pos, StarMap map)
{
    float zFactor = map.depthOfField/pos.GetDim(2);
    
    float plotX = pos.GetDim(0)*zFactor;
    float plotY = pos.GetDim(1)*zFactor;
    
    Vector2 mapPos = new Vector2(-plotX, -plotY);
    return mapPos;
}



//////////////////////
// DRAW PLANETS //
//////////////////////
public void DrawPlanets(List<Planet> displayPlanets, ref MySpriteDrawFrame frame, StarMap map)
{
    PlanetSort(displayPlanets);
    
  
    Echo("\nDIPSLAYED PLANETS:");
    for(int d = 0; d < displayPlanets.Count; d++)
    {
        Planet planet = displayPlanets[d];
        Echo(planet.name);
        //Echo(planet.ToString());
        
        Vector2 planetPosition = PlotObject(planet.transformedCenter, map);
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
            case "ORANGE":
                surfaceColor = new Color(32,16,0);
                lineColor = new Color(64,32,0);
                break;
            case "TAN":
                surfaceColor = new Color(153,100,48);
                lineColor = new Color(205,133,63);
                break;
            case "GRAY":
                surfaceColor = new Color(32,32,32);
                lineColor = new Color(64,64,64);
                break;
            case "GREY":
                surfaceColor = new Color(32,32,32);
                lineColor = new Color(64,64,64);
                break;
            case "WHITE":
                surfaceColor = new Color(75,100,100);
                lineColor = new Color(192,192,192);                
                break;
        }
            
            
        Vector2 startPosition = _viewport.Center + planetPosition;

        float diameter = ProjectDiameter(planet, map);//2*planet.radius*map.depthOfField/planet.transformedCenter.GetDim(2);
        
        Vector2 position = startPosition - new Vector2(diameter/2,0);

        // Body
        var sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Position = position,
            Size=  new Vector2(diameter,diameter), 
            Color = surfaceColor,
        };
        frame.Add(sprite);
            
            
        // Equator
        double radAngle = (float) map.altitude * Math.PI/180;
        float pitchMod = (float) Math.Sin(radAngle)*diameter;
        
        
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "CircleHollow",
            Position = position,
            Size=  new Vector2(diameter,pitchMod), 
            Color = lineColor,
        };
    
        frame.Add(sprite);
     
        // Mask
        int scaleMod = -1;
        if(map.altitude < 0)
        {
            scaleMod *= -1;
        }

        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "SemiCircle",
            Position = position,
            Size=  new Vector2(diameter, diameter*scaleMod), 
            Color = surfaceColor,
        };
        // Add the sprite to the frame
        frame.Add(sprite);

            

            
        // Border
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "CircleHollow",
            Position = position,
            Size=  new Vector2(diameter, diameter), 
            Color = lineColor,
        }; 
        frame.Add(sprite);


        // HashMarks
        if(diameter > HASH_LIMIT)// && Vector3.Distance(planet.center, map.center) < 2*planet.radius
        {
            DrawHashMarks(planet, diameter, lineColor, map, ref frame);
        }


        if(_showPlanetNames)
        {
            // PLANET NAME
            float fontMod = 1;

            if(diameter < 50)
            {
                fontMod =(float)0.5;
            }

            
            position = startPosition;
            
            // Name Shadow
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = planet.name,
                Position = position,
                RotationOrScale = fontMod*0.8f,
                Color = Color.Black,
                Alignment = TextAlignment.CENTER /* Center the text on the position */,
                FontId = "White"
            };
            frame.Add(sprite);


            position += new Vector2(-2,2);

            // Name
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = planet.name,
                Position = position,
                RotationOrScale = fontMod*0.8f,
                Color = Color.Yellow,
                Alignment = TextAlignment.CENTER /* Center the text on the position */,
                FontId = "White"
            };
            frame.Add(sprite);    
        }
    }
}


///////////////////////
// DRAW HASHMARKS //   Makes series of low-profile waypoints to denote latitude and longitude on planets.
///////////////////////
public void DrawHashMarks(Planet planet, float diameter, Color lineColor, StarMap map, ref MySpriteDrawFrame frame)
{
    List<Waypoint> hashMarks = new List<Waypoint>();
    
    float planetDepth = planet.transformedCenter.GetDim(2);
    
    
    //North Pole
    Waypoint north = new Waypoint();
    north.name = "N -";
    north.location = planet.center + new Vector3(0, (float) planet.radius, 0);
    north.transformedLocation = transformVector(north.location, map);
    if(north.transformedLocation.GetDim(2) < planetDepth)
    {
        hashMarks.Add(north);
    }
    
    
    //South Pole
    Waypoint south = new Waypoint();
    south.name = "S -";
    south.location = planet.center - new Vector3(0, (float) planet.radius, 0);
    south.transformedLocation = transformVector(south.location, map);
    if(south.transformedLocation.GetDim(2) < planetDepth)
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
            
            hashMark.location = planet.center + new Vector3(xCoord, yCoord, zCoord);
            hashMark.transformedLocation = transformVector(hashMark.location, map);
            
            if(hashMark.transformedLocation.GetDim(2) < planetDepth)
            {
                hashMarks.Add(hashMark);
            }
        }
    }
    
    for(int h = 0; h < hashMarks.Count; h++)
    {
        Waypoint hash = hashMarks[h];
        Vector2 position = _viewport.Center + PlotObject(hash.transformedLocation, map);        
        
        // Print more detail for closer planets
        if(diameter > 2 * HASH_LIMIT)
        {
            String[] hashLabels = hash.name.Split(' ');
            
            
            Vector2 hashOffset = new Vector2(0,10);
            position -= hashOffset;
            
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = hashLabels[0],
                Position = position,
                RotationOrScale = 0.5f,
                Color = lineColor,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite);
            
            position += hashOffset; 
 
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = hashLabels[1],
                Position = position,
                RotationOrScale = 0.4f,
                Color = lineColor,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            frame.Add(sprite); 
            
            
        }
        else
        {
            position += new Vector2(-2,2);
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Circle",
                Position = position,
                Size=  new Vector2(4,4), 
                Color = lineColor,
            };
            frame.Add(sprite);
        }
    }
    //hashMarks.Clear();
}

///////////////////////
// DRAW WAYPOINTS //
///////////////////////
public void DrawWaypoints(ref MySpriteDrawFrame frame, StarMap map)
{
    float fontSize = 0.5f;
    int markerSize = MARKER_WIDTH;
    if(_viewport.Width > 500)
    {
        fontSize *= 1.5f;
        markerSize *= 2;
    }
    for(int w = 0; w < _waypointList.Count; w++)
    {
        Waypoint waypoint = _waypointList[w];
        
        if(waypoint.isActive)
        {
            float rotationMod = 0;
            Color markerColor = Color.White;
            Vector2 markerScale = new Vector2(markerSize,markerSize);
            
            Vector2 waypointPosition = PlotObject(waypoint.transformedLocation, map);
            Vector2 startPosition = _viewport.Center + waypointPosition;
            
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
                default:
                    markerShape = "SquareHollow";
                    break;
            }
            
            
            if(waypoint.transformedLocation.GetDim(2) > map.depthOfField)
            {
                Vector2 position = startPosition - new Vector2(markerSize/2,0);
                
                // PRINT MARKER
                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = markerShape,
                    Position = position,
                    RotationOrScale = rotationMod,
                    Size=  markerScale, 
                    Color = Color.Black,
                };
                frame.Add(sprite);
                
                position += new Vector2(1,0);
                
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = markerShape,
                    Position = position,
                    RotationOrScale = rotationMod,
                    Size =  markerScale * 1.2f, 
                    Color = markerColor,
                };
                frame.Add(sprite);
                
                position += new Vector2(1,0);
                
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = markerShape,
                    Position = position,
                    RotationOrScale = rotationMod,
                    Size=  markerScale, 
                    Color = markerColor,
                };
                frame.Add(sprite);
                
                if(waypoint.marker.ToUpper() == "STATION")
                {
                    position += new Vector2(markerSize/2 - markerSize/20, 0);
                    
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = position,
                        RotationOrScale = rotationMod,
                        Size=  new Vector2(markerSize/10 ,markerSize), 
                        Color = markerColor,
                    };
                    frame.Add(sprite);
                    
                    position = startPosition;
                }
                
                if(waypoint.marker.ToUpper() == "HAZARD")
                {
                    position += new Vector2(markerSize/2 - markerSize/20, -markerSize*0.85f);
                    
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = "!",
                        Position = position,
                        RotationOrScale = fontSize*1.2f,
                        Color = Color.White,
                        Alignment = TextAlignment.CENTER, 
                        FontId = "White"
                    };
                    frame.Add(sprite);
                    
                    position = startPosition;
                }


                if(waypoint.marker.ToUpper() == "BASE")
                {
                    position += new Vector2(markerSize/6, -markerSize/12);
                    sprite = new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SemiCircle",
                        Position = position,
                        RotationOrScale = rotationMod,
                        Size=  new Vector2(markerSize*1.15f,markerSize*1.15f), 
                        Color = new Color(0,64,64),
                    };
                    frame.Add(sprite);
                    position += new Vector2(0.25f * markerSize,-0.33f * markerSize);
                    
                }
            
            
            
            
            
                // PRINT NAME
                position += new Vector2(1.33f * markerSize,-0.75f * markerSize);
                
                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = waypoint.name,
                    Position = position,
                    RotationOrScale = fontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };
                frame.Add(sprite);
            }
        }    
    }
}


/////////////////////////
// PROJECT DIAMETER //
/////////////////////////
float ProjectDiameter(Planet planet, StarMap map)
{
    float viewAngle = (float) Math.Asin(planet.radius/planet.transformedCenter.GetDim(2));
    
    float diameter = (float) Math.Tan(Math.Abs(viewAngle)) * 2 * map.depthOfField;
    
    if(diameter < DIAMETER_MIN)
    {
        diameter = DIAMETER_MIN;
    }
    
    return diameter;
}



/////////////////////
// OBSCURE SHIP //
/////////////////////
public String obscureShip(Vector2 shipPos, List<Planet> planets, StarMap map)
{
    //Get Nearest Planet on Screen
    Planet closest = planets[0];
    for(int p = 0; p < planets.Count; p++)
    {
        if(Vector2.Distance(shipPos, planets[p].mapPos) < Vector2.Distance(shipPos, closest.mapPos))
        {
            closest = planets[p];
        }
    }
    
    String color = "NONE";
    float distance = Vector2.Distance(shipPos, closest.mapPos);
    float radius = 0.95f * closest.radius*map.depthOfField/closest.transformedCenter.GetDim(2);
    
    if(distance < radius && closest.transformedCenter.GetDim(2) < transformVector(_refBlock.GetPosition(), map).GetDim(2))
    {
        color = closest.color;
    }

    return color;
}   


// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


///////////////////////////
// STRING TO WAYPOINT //
///////////////////////////
public Waypoint StringToWaypoint(String argument)
{
    Waypoint waypoint = new Waypoint();
    String[] wayPointData = argument.Split(';');
    if(wayPointData.Length > 3)
    {
        waypoint.SetName(wayPointData[0]);
        waypoint.SetLocation(StringToVector3(wayPointData[1]));
        waypoint.SetMarker(wayPointData[2]);    
        waypoint.isActive = wayPointData[3].ToUpper() == "ACTIVE";
    }
    return waypoint;
}


///////////////////////////
// WAYPOINT TO STRING //
///////////////////////////
public String WaypointToString(Waypoint waypoint)
{
    String output = waypoint.name + ";" + Vector3ToString(waypoint.location) + ";" + waypoint.marker;
    
    String activity = "INACTIVE";
    if(waypoint.isActive)
    {
        activity = "ACTIVE";
    }
    output += ";" + activity;
    
    return output;        
}


//////////////////////////
// VECTOR3 TO STRING //
//////////////////////////
public static string Vector3ToString(Vector3 vec3)
{
	String newData = "(" + vec3.GetDim(0)+"," + vec3.GetDim(1) + "," + vec3.GetDim(2) + ")";
	return newData;
}


//////////////////////////
// STRING TO VECTOR3 //
//////////////////////////
public static Vector3 StringToVector3(string sVector)
{
    // Remove the parentheses
    if (sVector.StartsWith ("(") && sVector.EndsWith (")"))
    {
        //Echo(sVector);
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


/////////////////         Allows you to cycle continuously through an array or matrix. If you move past
// WRAP ADD //           either end of the array, it returns you to the other side. (i.e. From last index to
/////////////////         first if you're going in the positive direction)

public int wrapAdd(int i, int mod, int length) // mod is the direction you want to move in the array.  Should be 1 or -1.
{
    int iNew = i+mod;

    if(iNew<0)
    {
        iNew = length - 1;
    }
    else if(iNew >= length)
    {
        iNew = 0;
    }

    return iNew;    
}


//////////////////////////
// ABBREVIATE VALUE //   Abbreviates float value to k/M/G notation (i.e. 1000 = 1k). Returns string.
//////////////////////////
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


////////////////////////
// REPLACE COLUMN //
////////////////////////
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


///////////////
// T-VALUE //
///////////////
public static double TValue(Vector3 vec3)
{
    double result;
	double x = vec3.GetDim(0);
	double y = vec3.GetDim(1);
	double z = vec3.GetDim(2);
	
    result = -1*(x*x + y*y + z*z);
    return result;
}


////////////
// Det4 //        Gets determinant of a 4x4 Matrix
////////////
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


////////////////////
// PLANET SORT //    Sorts Planets List by Transformed Z-Coordinate from largest to smallest.
////////////////////    For the purpose of sprite printing.
public void PlanetSort(List<Planet> planets)
{
    int length = planets.Count;
    
    for(int i = 0; i < planets.Count - 1; i++)
    {
        for(int p = 1; p < length; p++)
        {
            Planet planetA = planets[p-1];
            Planet planetB = planets[p];
            
            if(planetA.transformedCenter.GetDim(2) < planetB.transformedCenter.GetDim(2))
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


///////////////////////
// SORT BY NEAREST //    Sorts Planets by nearest to farthest.
///////////////////////
public void SortByNearest(List<Planet> planets)
{
    int length = planets.Count;
    
    for(int i = 0; i < planets.Count - 1; i++)
    {
        for(int p = 1; p < length; p++)
        {
            Planet planetA = planets[p-1];
            Planet planetB = planets[p];
            
            float distA = Vector3.Distance(planetA.center, _refBlock.GetPosition());
            float distB = Vector3.Distance(planetB.center, _refBlock.GetPosition());
            
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


/////////////////////////
// TRANSFORM VECTOR //     Transforms vector location of planet or waypoint for StarMap view.
/////////////////////////
 public Vector3 transformVector(Vector3 vectorIn, StarMap map)
 {
     //double x = vectorIn.GetDim(0);
     //double y = vectorIn.GetDim(1);
     //double z = vectorIn.GetDim(2);
     
     //double xC = map.center.GetDim(0);
     //double yC = map.center.GetDim(1);
     //double zC = map.center.GetDim(2);
     
     double xS = vectorIn.GetDim(0) - map.center.GetDim(0); //Vector X - Map X
     double yS = vectorIn.GetDim(1) - map.center.GetDim(1); //Vector Y - Map Y
     double zS = vectorIn.GetDim(2) - map.center.GetDim(2); //Vector Z - Map Z 
     
     double r = map.rotationalRadius;
     
     double cosAz = Math.Cos(toRadians(map.azimuth));
     double sinAz = Math.Sin(toRadians(map.azimuth));
     
     double cosAlt = Math.Cos(toRadians(map.altitude));
     double sinAlt = Math.Sin(toRadians(map.altitude));
     
     // Transformation Formulas from Matrix Calculations
     double xT = cosAz * xS + sinAz * zS;
     double yT = sinAz*sinAlt * xS + cosAlt * yS - sinAlt * cosAz * zS;
     double zT = -sinAz * cosAlt * xS + sinAlt * yS + cosAz * cosAlt *zS + r;
     
     Vector3 vectorOut = new Vector3(xT,yT,zT);
     //Echo("VectorOut: " + Vector3ToString(vectorOut));
     return vectorOut;
 }
 
 
//////////////////////
// ROTATE VECTOR //
//////////////////////
public Vector3 rotateVector(Vector3 vecIn, StarMap map)
{
  float x = vecIn.GetDim(0);
  float y = vecIn.GetDim(1);
  float z = vecIn.GetDim(2);
  
  float cosAz = (float) Math.Cos(toRadians(map.azimuth));
  float sinAz = (float) Math.Sin(toRadians(map.azimuth));
     
  float cosAlt = (float) Math.Cos(toRadians(map.altitude));
  float sinAlt = (float) Math.Sin(toRadians(map.altitude));
  
  float xT = cosAz * x + sinAz * z;
  float yT = sinAz * sinAlt * x + cosAlt * y -sinAlt * cosAz * z;
  float zT = -sinAz * cosAlt * x + sinAlt * y + cosAz * cosAlt * z;

  Vector3 vecOut = new Vector3(xT, yT, zT);
  return vecOut;
}


////////////////////////
// ROTATE MOVEMENT //    Rotates Movement Vector for purpose of translation.
////////////////////////
public Vector3 rotateMovement(Vector3 vecIn, StarMap map)
{
  float x = vecIn.GetDim(0);
  float y = vecIn.GetDim(1);
  float z = vecIn.GetDim(2);
  
  float cosAz = (float) Math.Cos(toRadians(-map.azimuth));
  float sinAz = (float) Math.Sin(toRadians(-map.azimuth));
     
  float cosAlt = (float) Math.Cos(toRadians(-map.altitude));
  float sinAlt = (float) Math.Sin(toRadians(-map.altitude));
  
  float xT = cosAz * x + sinAz * sinAlt * y + sinAz * cosAlt * z;
  float yT = cosAlt * y - sinAlt * z;
  float zT = -sinAz * x + cosAz * sinAlt * y + cosAz * cosAlt * z;
  
  Vector3 vecOut = new Vector3(xT, yT, zT);
  return vecOut;    
}


//////////////////////
// CYCLE EXECUTE //      Wait specified number of cycles to execute cyclial commands
//////////////////////
public void cycleExecute(StarMap map)
{
    _cycleStep--;
    
    // EXECUTE CYCLE DELAY FUNCTIONS
    if(_cycleStep < 0)
    {
        _cycleStep = CYCLE_LENGTH;
        
        //Toggle Indicator LightGray
        _lightOn = !_lightOn;
        
        if(map.mode.ToUpper() == "PLANET")
        {
            //Sort Planets by proximity to ship.
            SortByNearest(_planetList);
            
            ShipToPlanet(_planetList[0], map);
            _previousCommand = "NEWLY LOADED";
        }
        else if(map.mode.ToUpper() == "SHIP")
        {
            map.center = _refBlock.GetPosition();
            _previousCommand = "NEWLY LOADED";
        }
    }
}

// BORROWED FUNCTIONS /////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////
// PREPARE TEXT //  Not mine.  From Malware on   GitHub
// SURFACE FOR  //
//      SPRITES      //
////////////////////
public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
{
    // Set the sprite display mode
    textSurface.ContentType = ContentType.SCRIPT;
    // Make sure no built-in script has been selected
    textSurface.Script = "";
}


/////////////////////
// DRAW SPRITES // Also from Malware
/////////////////////
public void DrawSprites(ref MySpriteDrawFrame frame, StarMap map)
{
    Echo("\nMAP DATA:");
    Echo("H: " + _viewport.Height + "  W: " + _viewport.Width);
    Echo("Center: " + map.center);
    Echo("Azimuth: " + map.azimuth);
    Echo("Altitude: " + map.altitude*-1);
    Echo("View Radius: " + map.rotationalRadius);
    Echo("Depth of Field: " + map.depthOfField);
    

    Vector3 mapCenter = map.center;
    
    Color gridColor = new Color(0,64,0);
    // Create background sprite
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "Grid",
        Position = _viewport.Center,
        Size = _viewport.Size,
        Color = gridColor,
        Alignment = TextAlignment.CENTER
    };
    // Add the sprite to the frame
    frame.Add(sprite);

    //DRAW PLANETS
    List<Planet> displayPlanets = new List<Planet>();
    
    for(int p = 0; p < _planetList.Count; p++)
    {
        if(_planetList[p].transformedCenter.GetDim(2) > map.depthOfField)
        {
            displayPlanets.Add(_planetList[p]);
        }
    }
    DrawPlanets(displayPlanets, ref frame, map);
    
    //DRAW WAYPOINTS
    if(_gpsActive)
    {
        DrawWaypoints(ref frame, map);
    }


    // DRAW SHIP
    DrawShip(ref frame, map, displayPlanets);

    // MAP INFO
    if(_showMapParameters)
    {
        drawMapInfo(ref frame, map);
    }
}


/////////////////////
// DRAW MAP INFO //
/////////////////////
public void drawMapInfo(ref MySpriteDrawFrame frame, StarMap map)
{
    //DEFAULT SIZING / STRINGS
    float fontSize = 0.6f;
    int barHeight = BAR_HEIGHT;
    String angleReading = map.altitude*-1 + "° " + map.azimuth + "°";
    String shipMode = "S";
    String planetMode = "P";
    String worldMode = "W";
    
    if(_viewport.Width > 500)
    {
        fontSize *= 1.5f;
        barHeight *= 2;
        angleReading = "Alt:" + map.altitude*-1 + "°  Az:" + map.azimuth + "°";
        shipMode = "SHIP";
        planetMode = "PLANET";
        worldMode = "WORLD";
    }
    
    
    
    //TOP BAR
    var position = new Vector2(0,_viewport.Width/2 - _viewport.Height/2 + barHeight/2);
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Color = Color.Black,
        Size= new Vector2(_viewport.Width, barHeight),
    };
    frame.Add(sprite);
        
        
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
        default:
            modeReading = worldMode;
            break;        
    }
    
    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = modeReading,
        Position = position,
        RotationOrScale = fontSize,
        Color = Color.White,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
        
        
    // CENTER READING
    string xCenter = abbreviateValue(map.center.GetDim(0));
    string yCenter = abbreviateValue(map.center.GetDim(1));
    string zCenter = abbreviateValue(map.center.GetDim(2));
    string centerReading = "[" + xCenter + ", " + yCenter + ", " + zCenter + "]";
        
    position += new Vector2(_viewport.Width/2 - SIDE_MARGIN, 0);

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = centerReading,
        Position = position,
        RotationOrScale = fontSize,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
        
        
    // RUNNING INDICATOR
    position += new Vector2(_viewport.Width/2 - SIDE_MARGIN, TOP_MARGIN);
    
    Color lightColor = new Color(0,8,0);
    
    if(_lightOn)
    {
        sprite = new MySprite()
        {
            Type = SpriteType.TEXTURE,
            Data = "Circle",
            Position = position,
            Size=  new Vector2(7,7), 
            Color = lightColor,
        };
        frame.Add(sprite); 
    }


    // BOTTOM BAR
    position = new Vector2(0,_viewport.Width/2 + _viewport.Height/2 - barHeight/2);
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Color = Color.Black,
        Size= new Vector2(_viewport.Width, barHeight),
    };
    frame.Add(sprite);
        
        
    // DEPTH OF FIELD READING
    position += new Vector2(SIDE_MARGIN,-TOP_MARGIN);
        
    string dofReading = "DoF:" + abbreviateValue((float)map.depthOfField);

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = dofReading,
        Position = position,
        RotationOrScale = fontSize,
        Color = Color.White,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
        
        
    // ANGLE READING
    position += new Vector2(_viewport.Width/2 - SIDE_MARGIN, 0);

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = angleReading,
        Position = position,
        RotationOrScale = fontSize,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
        
        
    // RADIUS READING
    string radius = "R:"+ abbreviateValue((float)map.rotationalRadius);
    position += new Vector2(_viewport.Width/2 - SIDE_MARGIN, 0);
        
    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = radius,
        Position = position,
        RotationOrScale = fontSize,
        Color = Color.White,
        Alignment = TextAlignment.RIGHT,
        FontId = "White"
    };
    frame.Add(sprite);

}



///////////////////////////
// MULTIPLY MATRICES //     Get the dot product of two different square matrices.
///////////////////////////
public double[,] MultiplyMatrices(double[,] matrixA, double[,] matrixB, int matrixSize)
{
    double[,] matrixOut = new double[matrixSize, matrixSize];
    
    for(int row = 0; row < matrixSize; row++)
    {
        for(int col = 0; col < matrixSize; col++)
        {
            double matrixEntry = 0;
            
            for(int i = 0; i < matrixSize; i++)
            {
                matrixEntry += matrixA[row,i] * matrixB[i,col];
            }
            
            matrixOut[row, col] = matrixEntry;
        }
    }
    
    return matrixOut;
}

///////////////////
// TO RADIANS //  Converts Degree Value to Radians
///////////////////
public double toRadians(int angle)
{
    double radianValue = (double) angle * Math.PI/180;
    return radianValue;
}


//////////////////
// TO DEGREES //
//////////////////
public float toDegrees(float angle)
{
    float degreeValue = angle * 180/(float) Math.PI;
    return degreeValue;
}


///////////////////
// DEGREE ADD //    Adds two degree angles.  Sets Rollover at +/- 180°
///////////////////
public static int degreeAdd(int angle_A, int angle_B)
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
 
 
 
///////////////////
// UPDATE MAP //     Transform logged locations based on map parameters
///////////////////
public void updateMap(StarMap map)
 {
     Echo("Map Updated!");
     if(_planetList.Count > 0)
     {
         //Echo("RELATIVE COORDINATES:");
         for(int p = 0; p < _planetList.Count; p++)
         {
             Planet planet = _planetList[p];
             planet.transformedCenter = transformVector(planet.center, map);
             //Echo(planet.name + ": " + Vector3ToString(planet.transformedCenter));
         }
     }
     
     if(_waypointList.Count > 0)
     {
         for(int w = 0; w < _waypointList.Count; w++)
         {
             Waypoint waypoint = _waypointList[w];
             waypoint.transformedLocation = transformVector(waypoint.location, map);
         }
     }
 }