MyIni _mapLog = new MyIni();
string _lcdName;
string _refName;
string _previousCommand;
int _lcdIndex;
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
const int MARKER_WIDTH = 10; // Width of GPS Markers
const float DIAMETER_MIN = 6; //Minimum Diameter For Distant Planets

const string DEFAULT_SETTINGS = "[Map Settings]\nLCD_Name=[MAP]\nLCD_Index=0\nReference_Name=[Reference]\nPlanet_List=\nWaypoint_List=\n";
//const string DEFAULT_SETTINGS = "[Map Settings]\nLCD_Name=<name>\nLCD_Index=0\nReference_Name=<name>\n";
const string DEFAULT_DISPLAY = "[mapDisplay]\nViewPlane=X Y\nCenter=(0,0,0)\nScale=1000\nMode=WORLD";

const int HEIGHT = 128;
const int WIDTH = 256;

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

    public string viewPlane;
    public Vector3 center;
    public int scale;
    public string mode;
    public int altitude;
    public int azimuth;
    public int rotationalRadius;
    public int depthOfField;
    
    public StarMap()
    {
        
    }
    
    public void SetViewPlane(string plane)
    {
        this.viewPlane = plane.ToUpper();
    }
    
    public string GetViewPlane()
    {
        return this.viewPlane;
    }
    
    public void SetCenter(Vector3 coordinate)
    {
        this.center = coordinate;
    }
    
    public Vector3 GetCenter()
    {
        return this.center;
    }
    
    public void SetScale(int inputScale)
    {
        this.scale = inputScale;
    }
    
    public int GetScale()
    {
        return this.scale;
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
    
    public void yaw(int angle)
    {
        this.azimuth = degreeAdd(this.azimuth, angle);
    }
    
    public void pitch(int angle)
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
        if(planetData.Length < 8)
        {
            return;
        }
		        
        this.SetName(planetData[0]);
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

    public Waypoint (String wayPointString)
    {
        //this.designation = wayPointList.Count()+1;
        String[] wayPointData = wayPointString.Split(';');
			
        this.SetName(wayPointData[0]);
        this.SetLocation(StringToVector3(wayPointData[1]));
        this.SetMarker(wayPointData[2]);
        
        this.isActive = true;
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
           Waypoint waypoint = new Waypoint(gpsEntries[g]);
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
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
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
        Echo("Last Logged: " + planet.name);
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
        Echo("Last Logged: " + waypoint.name);
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
        
        if (_previousCommand == "NEWLY LOADED")
        {
            updateMap(myMap);
        }

        if (argument != "")
        {
            string [] args = argument.Split(' ');
            string command = args[0].ToUpper();
            _previousCommand = "Command: " + argument;
            
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
                case "TOGGLE_MODE":
                    ToggleMode(myMap);
                    break;
                case "CENTER_MAP":
                    CenterMap(myMap);
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
        string newData = DEFAULT_DISPLAY;
        
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


////////////////
// ZOOM IN //
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
// ZOOM OUT //
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


/////////////////
// MOVE LEFT //
/////////////////
void moveLeft(StarMap map)
{
    Vector3 moveVector = new Vector3(MOVE_STEP,0,0);
    map.center += rotateMovement(moveVector, map);
}


//////////////////
// MOVE RIGHT //
//////////////////
void moveRight(StarMap map)
{
    Vector3 moveVector = new Vector3(MOVE_STEP,0,0);
    map.center -= rotateMovement(moveVector, map);
}


//////////////////
// MOVE UP //
//////////////////
void moveUp(StarMap map)
{
    Vector3 moveVector = new Vector3(0,MOVE_STEP,0);
    map.center += rotateMovement(moveVector, map);
}


//////////////////
// MOVE DOWN //
//////////////////
void moveDown(StarMap map)
{
    Vector3 moveVector = new Vector3(0,MOVE_STEP,0);
    map.center -= rotateMovement(moveVector, map);
}


/////////////////////
// MOVE FORWARD //
/////////////////////
void moveForward(StarMap map)
{
    Vector3 moveVector = new Vector3(0,0,MOVE_STEP);
    map.center += rotateMovement(moveVector, map);
}


//////////////////////
// MOVE BACKWARD //
//////////////////////
void moveBackward(StarMap map)
{
    Vector3 moveVector = new Vector3(0,0,MOVE_STEP);
    map.center -= rotateMovement(moveVector, map);
}


//////////////////
// CENTER MAP //   Centers Map on Ship Position
//////////////////
void CenterMap(StarMap map)
{
    Vector3 newCenter = _refBlock.GetPosition();
    map.center = newCenter;
}


/////////////////
// SHIP MODE //    Changes map object to SHIP mode.
/////////////////  (Possibly expand to take LCD argument for finding multiple maps across ship)
void ShipMode(StarMap map)
{
    map.mode = "SHIP";
}


//////////////////
// WORLD MODE //    Changes map object to WORLD mode.
//////////////////
void WorldMode(StarMap map)
{
    map.mode = "WORLD";
}


///////////////////
// TOGGLE MODE //    Toggles MAP between "World" and "Ship" modes.
///////////////////

void ToggleMode(StarMap map)
{
    if(map.mode=="WORLD")
    {
    //IF MAP IS IN WORLD MODE, CHANGE TO SHIP MODE.
        ShipMode(map);
    }
    else
    {
    //IF MAP IS IN SHIP MODE OR THE STRING IS NOT RECOGNIZED, SWITCH TO WORLD MODE.
        WorldMode(map);
    }
}


////////////////////
// DEFAULT VIEW //
////////////////////
void DefaultView(StarMap map)
{
    WorldMode(map);
    
    Vector3 origin = new Vector3(0,0,0);
    map.center = origin;
    map.depthOfField = 512;
    map.rotationalRadius = 120000;
    map.azimuth = 0;
    map.altitude = 15;
}


// DRAWING FUNCTIONS //////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////
// PLOT SHIP //
///////////////
public void PlotShip(ref MySpriteDrawFrame frame, StarMap map)
{
    // DETERMINE ACTIVE AXES FROM VIEWPLANE
    string [] axes = map.viewPlane.Split(' ');

    // Convert Axes into numerical dimensions to asign to vertical and horizontal as well as +/- mods
    int [] horz = ParseAxis(axes[0]);
    int [] vert = ParseAxis(axes[1]);
    
    int hDimension = horz[0];
    int hMod = horz[1];
    
    int vDimension = vert[0];
    int vMod = vert[1];
    
    //Get the appropriate dimensions of the screen center.
    Vector3 mapCenter = map.center;
    float hMap = mapCenter.GetDim(hDimension);
    float vMap = mapCenter.GetDim(vDimension);
    
    //Get appropriate dimentsions of ship position.
    Vector3 shipPos = _refBlock.GetPosition();
    float hShip = shipPos.GetDim(hDimension);
    float vShip = shipPos.GetDim(vDimension);
    
    
    //Get ship's directional vector and break it down to its relevent components.
    Vector3 heading = _refBlock.WorldMatrix.Forward;
    float adj = heading.GetDim(hDimension)*hMod;
    float opp = heading.GetDim(vDimension)*vMod;
    float angle = (float) Math.Atan2(opp,adj);
    
    int mapScale = map.scale;
    
    //Determine scaled coordinates of ship on map.
    float hPlot = (hShip - hMap)*hMod/mapScale;
    float vPlot = (vMap - vShip)*vMod/mapScale; //INVERTED BECAUSE OF DRAWFRAME COORDINATES
    
    Vector2 vecPlot = new Vector2(hPlot, vPlot);
    //DrawShip(ref frame, vecPlot, angle);
}


/////////////////
// DRAW SHIP //
/////////////////

public void DrawShip(ref MySpriteDrawFrame frame, StarMap map, List<Planet> displayPlanets)
{
    Vector2 position = PlotObject(transformVector(_refBlock.GetPosition(), map),map);
    
    // Get ships Direction Vector and align it with map
    Vector3 heading = rotateVector(_refBlock.WorldMatrix.Forward, map);
    
    // SHIP COLORS
    Color bodyColor = Color.LightGray;
    Color aftColor = Color.DarkOrange;
    
    if(displayPlanets.Count > 0)
    {
        String planetColor = obscureShip(position, displayPlanets, map);
        switch(planetColor.ToUpper())
        {
            case "RED":
                aftColor = new Color(48,0,0);
                bodyColor = new Color(64,0,0);
                break;
            case "GREEN":
                aftColor = new Color(0,48,0);
                bodyColor = new Color(0,64,0);
                break;
            case "BLUE":
                aftColor = new Color(0,0,48);
                bodyColor = new Color(0,0,64);
                break;
            case "YELLOW":
                aftColor = new Color(127,127,39);
                bodyColor = new Color(127,127,51);
                break;
            case "MAGENTA":
                aftColor = new Color(96,0,96);
                bodyColor = new Color(127,0,127);
                break;
            case "PURPLE":
                aftColor = new Color(36,0,62);
                bodyColor = new Color(48,0,96);
                break;
            case "CYAN":
                aftColor = new Color(0,48,48);
                bodyColor = new Color(0,64,64);
                break;
            case "ORANGE":
                aftColor = new Color(48,24,0);
                bodyColor = new Color(64,32,0);
                break;
            case "TAN":
                aftColor = new Color(175,115,54);
                bodyColor = new Color(205,133,63);
                break;
            case "GRAY":
                aftColor = new Color(48,48,48);
                bodyColor = new Color(64,64,64);
                break;
            case "GREY":
                aftColor = new Color(48,48,48);
                bodyColor = new Color(64,64,64);
                break;
            case "WHITE":
                aftColor = new Color(100,150,150);
                bodyColor = new Color(192,192,192);                
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
    
 
    
    
    
    
    // Ship Body
    var sprite = new MySprite()
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
    float aftHeight = SHIP_SCALE - shipLength/(float) 1.33;
    
    if(headingZ < 0)
    {
        aftColor = bodyColor;
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
}


///////////////////
// PLOT OBJECT //
///////////////////
public Vector2 PlotObject(Vector3 pos, StarMap map)
{
    Echo("Pos: " + Vector3ToString(pos));
    Echo("DoF: " + map.depthOfField);
    Echo("Z: " + pos.GetDim(2));
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
    
    
    Echo("DIPSLAYED PLANETS:");
    for(int d = 0; d < displayPlanets.Count; d++)
    {
        Planet planet = displayPlanets[d];
        Echo(planet.name);
        
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
        //Echo("StartPosition: " + startPosition);
        //Echo("ViewPort: " + _viewport.Center);
        //Echo("Planet: " + planetPosition);
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


        // TITLE
        float fontMod = 1;

        if(diameter < 50)
        {
            fontMod =(float)0.5;
        }


        position = startPosition;

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


///////////////////////
// DRAW WAYPOINTS //
///////////////////////
public void DrawWaypoints(ref MySpriteDrawFrame frame, StarMap map)
{
    for(int w = 0; w < _waypointList.Count; w++)
    {
        Waypoint waypoint = _waypointList[w];
        if(waypoint.transformedLocation.GetDim(2) > map.depthOfField)
        {
            Echo(waypoint.name);
            
            Vector2 waypointPosition = PlotObject(waypoint.transformedLocation, map);
            Vector2 startPosition = _viewport.Center + waypointPosition;
            
            float rotationMod = (float)Math.PI/4;
            
            Vector2 position = startPosition - new Vector2(MARKER_WIDTH/2,0);
            
            // PRINT MARKER
            var sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareHollow",
                Position = position,
                RotationOrScale = rotationMod,
                Size=  new Vector2(MARKER_WIDTH,MARKER_WIDTH), 
                Color = Color.White,
            };
            frame.Add(sprite);
            
            position += new Vector2(1,1);
            
            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareHollow",
                Position = position,
                RotationOrScale = rotationMod,
                Size=  new Vector2(MARKER_WIDTH,MARKER_WIDTH), 
                Color = Color.White,
            };
            frame.Add(sprite);
            
            position += new Vector2(-2,0);
            
            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "SquareHollow",
                Position = position,
                RotationOrScale = rotationMod,
                Size=  new Vector2(MARKER_WIDTH,MARKER_WIDTH), 
                Color = Color.White,
            };
            frame.Add(sprite);
            
            // PRINT NAME
            position += new Vector2(1.33f * MARKER_WIDTH,-0.75f * MARKER_WIDTH);
            
            sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = waypoint.name,
                Position = position,
                RotationOrScale = 0.5f,
                Color = Color.White,
                Alignment = TextAlignment.LEFT,
                FontId = "White"
            };
            frame.Add(sprite);
            
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
    float radius = closest.radius*map.depthOfField/closest.transformedCenter.GetDim(2);
    
    if(distance < radius && closest.transformedCenter.GetDim(2) < transformVector(_refBlock.GetPosition(), map).GetDim(2))
    {
        color = closest.color;
    }

    return color;
}   


// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////







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


//////////////////
// PARSE AXIS //   Converts axis string to 2 element int array.  First element is Dimension of axis (X->0, Y->1, Z->2) Second element is a +/- modifier.
//////////////////
public int[] ParseAxis(String dimension)
{
    int[] axis = new int[2];
    
    switch(dimension.ToUpper())
    {
        case "X":
            axis[0] = 0;
            axis[1] = 1;
            break;
        case "-X":
            axis[0] = 0;
            axis[1] = -1;
            break;
        case "Y":
            axis[0] = 1;
            axis[1] = 1;        
            break;
        case "-Y":
            axis[0] = 1;
            axis[1] = -1;
            break;
        case "Z":
            axis[0] = 2;
            axis[1] = 1;
            break;
        case "-Z":
            axis[0] = 2;
            axis[1] = -1;
            break;
        default:
            axis[0] = 3;
            axis[1] = 3;
            Echo("IMPROPER VIEWPLANE FORMAT!!!");
            break;
    }
    
    return axis;    
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
////////////////////
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
    Echo("MAP DATA:");
    Echo("Center: " + map.center);
    Echo("Azimuth: " + map.azimuth);
    Echo("Altitude: " + map.altitude*-1);
    Echo("View Radius: " + map.rotationalRadius);
    Echo("Depth of Field: " + map.depthOfField);
    
    
    Vector3 mapCenter = map.center;
    
    
    // Create background sprite
    var sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "Grid",
        Position = _viewport.Center,
        Size = _viewport.Size,
        Color = Color.DarkGray,
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
    DrawWaypoints(ref frame, map);

    // DRAW SHIP
    DrawShip(ref frame, map, displayPlanets);

    // TOP BAR
    var position = new Vector2(0,47);
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Color = Color.Black,
        Size= new Vector2(_viewport.Width,20),
    };
    frame.Add(sprite);
    
   // AZIMUTH READING
    position += new Vector2(10,-8);
    
    string mapAz = "Az: " + map.azimuth + "°";

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = mapAz,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
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
    
    position += new Vector2(_viewport.Width/2 -15, 0);

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = centerReading,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
    
    
    // ALTITUDE READING
    string mapAlt = "Alt: " + map.altitude*-1 + "°";
    position += new Vector2(125 ,0);
    
    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = mapAlt,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
        Color = Color.White,
        Alignment = TextAlignment.RIGHT,
        FontId = "White"
    };
    frame.Add(sprite);
    
    
    // BOTTOM BAR
    position = new Vector2(0,210);
    sprite = new MySprite()
    {
        Type = SpriteType.TEXTURE,
        Data = "SquareSimple",
        Position = position,
        Color = Color.Black,
        Size= new Vector2(_viewport.Width,20),
    };
    frame.Add(sprite);
    
    // DEPTH OF FIELD READING
    position += new Vector2(10,-8);
    
    string dofReading = "DoF:" + abbreviateValue((float)map.depthOfField);

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = dofReading,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
        Color = Color.White,
        Alignment = TextAlignment.LEFT /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
    
    
    // MODE READING
    position += new Vector2(_viewport.Width/2 -15, 0);
    string modeReading = map.mode;

    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = modeReading,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
        Color = Color.White,
        Alignment = TextAlignment.CENTER /* Center the text on the position */,
        FontId = "White"
    };
    frame.Add(sprite);
    
    
    // RADIUS READING
    string radius = "R:"+ abbreviateValue((float)map.rotationalRadius);
    position += new Vector2(125 ,0);
    
    sprite = new MySprite()
    {
        Type = SpriteType.TEXT,
        Data = radius,
        Position = position,
        RotationOrScale = 0.6f /* 80 % of the font's default size */,
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
         Echo("RELATIVE COORDINATES:");
         for(int p = 0; p < _planetList.Count; p++)
         {
             Planet planet = _planetList[p];
             planet.transformedCenter = transformVector(planet.center, map);
             Echo(planet.name + ": " + Vector3ToString(planet.transformedCenter));
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