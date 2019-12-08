    ////////////////////
  /// CONSTANTS ///
////////////////////
const String MAP_TAG = "[MAP]"; // Include this tag in the name of any relevent LCD's.
const String COCKPIT_TAG = "Remote Control NWL";

const String DEFAULT_DISPLAY = "X Y (0,0,0) 2000 WORLD";
const String DEFAULT_PLANET = "U0|EDEN|(0,0,0)|60000|Blue|||";


/// SCALE PARAMETERS ///
const int MIN_SCALE = 250;
const int VERT = 32;
const int HORZ = 32;
const int FWD = 32;
const int HEIGHT = 64;
const int WIDTH = 64;
const int DEPTH = 64;
const int RADIUS = 30;
const int THICK = 120000;
const int THICK2 = 60000;
const int CROSS_SPACE = 10; //Empty Space Between Crosshairs
const int MARGIN = 4;

const char GRID = '\uE149';
const char BODY1 = '\uE102'; //No touchey 
const char ARROW1 = '\uE220'; //No touchey
const char BODY2 = '\uE1C3'; //No touchey 
const char ARROW2 = '\uE190'; //No touchey
//const char EQUATOR = '\uE120';

const char RED = '\uE200'; //No touchey
const char MED_RED = '\uE1C0'; //No touchey 
const char DARK_RED = '\uE180'; //No touchey 

const char GREEN = '\uE120'; //No touchey 
const char MED_GREEN = '\uE118'; //No touchey 
const char DARK_GREEN = '\uE110'; //No touchey 

const char BLUE = '\uE104'; //No touchey 
const char MED_BLUE = '\uE103'; //No touchey 
const char DARK_BLUE = '\uE102'; //No touchey 

const char YELLOW = '\uE220'; //No touchey
const char MED_YELLOW = '\uE1D8'; //No touchey
const char DARK_YELLOW = '\uE190'; //No touchey

const char MAGENTA = '\uE204'; //No touchey
const char MED_MAGENTA = '\uE1C3'; //No touchey
const char DARK_MAGENTA = '\uE182'; //No touchey

const char CYAN = '\uE124'; //No touchey
const char MED_CYAN = '\uE11B'; //No touchey
const char DARK_CYAN = '\uE112'; //No touchey

const char WHITE = '\uE2FF'; //No touchey 
const char LIGHT_GRAY = '\uE1DB'; //No touchey 
const char MED_GRAY = '\uE192'; //No touchey 
const char DARK_GRAY = '\uE149'; //No touchey 
const char BLACK = '\uE100'; //No touchey 

readonly String[,] AXIS = new String[6,6] {{"E","-X -Y", "-X -Z", "E", "-X Y", "-X Z"},
                                                                    {"-Y -X", "E", "-Y -Z", "-Y X", "E", "-Y Z"},
                                                                    {"-Z -X", "-Z -Y", "E", "-Z X", "-Z Y", "E"},
                                                                    {"E", "X -Y", "X -Z", "E", "X Y", "X Z"},
                                                                    {"Y -X", "E", "Y -Z", "Y X", "E", "Y Z"},
                                                                    {"Z -X", "Z -Y", "E", "Z X", "Z Y", "E"}};


    /////////////////
  /// CLASSES ///
/////////////////

public Program(){ 
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
} 
 
public void Save(){} 

public class Planet{

    public int designation;
    public String name;
    public Vector3 center;
    public float radius;
    public String color;
    public Vector3 point1;
    public Vector3 point2;
    public Vector3 point3;
    public Vector3 point4;

    public Planet(String planetString, List<Planet> planetList){
        		this.designation = planetList.Count()+1;
        if(planetString.Contains("|")){	
		    String[] planetData = planetString.Split('|');
            this.SetName(planetData[1]);
            this.SetColor(planetData[4]);
	
            if(planetData[2] != ""){
                this.SetCenter(StringToVector3(planetData[2]));
            }

            if(planetData[3] != ""){
                this.SetRadius(float.Parse(planetData[3]));
            }

            if(planetData[5] != ""){
                this.SetPoint(1, StringToVector3(planetData[5]));
            }

            if(planetData[6] != ""){
                		this.SetPoint(2, StringToVector3(planetData[6]));
            }

            if(planetData[7] != ""){
                this.SetPoint(3, StringToVector3(planetData[7]));
            }

            if(planetData[8] != ""){
                		this.SetPoint(4, StringToVector3(planetData[8]));
            }
        }
        else{
            this.SetName(planetString);
        }
		
        planetList.Add(this);
    }

    public void SetDesignation(int des)
    {
        	designation = des;
    }
	
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
	
    public int GetDesignation()
    {
        return designation;
    }
	
    public String GetName()
    {
        return name;
    }

    public Vector3 GetCenter()
    {
        return center;
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

    public override String ToString()
    {
        String[] planetData = new String[9];
		
        planetData[1] = this.GetName();
        planetData[2] = Vector3ToString(this.GetCenter());
	
        float radius = this.GetRadius();
        if(radius>0)
        {
            planetData[3] = radius.ToString();
            planetData[0] = "P" + this.GetDesignation();
        }
        else
        {
            planetData[3] = "";
            planetData[0] = "U" + this.GetDesignation();
        }

        planetData[4] = this.GetColor();
		for(int c = 5; c<9; c++)
		{
			if(this.GetPoint(c-4) != Vector3.Zero)
			{
				planetData[c] = Vector3ToString(this.GetPoint(c-4));
			}
		}

        String planetString = BuildEntries(planetData, 0, 9, "|");
      //  String planetString = planetData[0];
        //for(int i=1; i<9; i++){
          //  planetString = planetString + "|" + planetData[i];
        //}
        return planetString;
    }

    		public void SetMajorAxes(){
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


public class WayPoint
{
	public int designation;
    public String name;
    public Vector3 location;

    public WayPoint (String wayPointString, List<WayPoint> wayPointList)
    {
        this.designation = wayPointList.Count()+1;
        String[] wayPointData = wayPointString.Split('|');
			
        this.SetName(wayPointData[1]);
        this.SetLocation(StringToVector3(wayPointData[2]));

    	wayPointList.Add(this);
    }	
	
    public void SetDesignation(int des)
    {
        designation = des;
    }
	
    public void SetName(String arg)
    {
        name = arg;
    }

    public void SetLocation(Vector3 coord)
    {
        location = coord;
    }

    	public int GetDesignation()
    {
        return designation;
    }
	
    public String GetName()
    {
        return name;
    }

    public Vector3 GetLocation()
    {
        return location;
    }

    public override String ToString()
    {
        String wpString = "G" + this.GetDesignation() + "|" + this.GetName() + "|" + Vector3ToString(this.GetLocation());
        return wpString;
    }
}

    /////////////
  /// MAIN ///
/////////////

public void Main(String arg){
    IMyShipController cockpit = GridTerminalSystem.GetBlockWithName(COCKPIT_TAG) as IMyShipController;
    List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(MAP_TAG, lcds);
	
//CHECK FOR ARGUMENTS
    Echo(arg);
    if(arg != ""){
    	//LOAD PLANETS & WAYPOINTS FROM PROGRAM BLOCK
        String[] mapData = Me.CustomData.Split('\n');
        List<Planet> planets = new List<Planet>();
        List<Planet> unchartedPlanets = new List<Planet>();
        List<WayPoint> wayPoints = new List<WayPoint>();

        for(int p = 0; p < mapData.Length; p++){
        //BUILD PLANETS FROM LOAD - SORT INTO PLANET LIST
            String entity = mapData[p];
            if (entity.StartsWith("P")){
                Planet planet = new Planet(entity, planets);

            } else if(entity.StartsWith("G")){
            //BUILD WAYPOINTS FROM LOAD - SORT INTO WAYPOINT LIST
                WayPoint wayPoint = new WayPoint(entity, wayPoints);

            } else if(entity.StartsWith("U")){
                				Planet uncharted = new Planet(entity, unchartedPlanets);
            }
        }
	
        String[] args = arg.Split(' ');
        String pilot_cmd = args[0].ToUpper();
        Echo(pilot_cmd);
        String bigArg="";

    	//Check if there is an LCD tag argument.  If there isn't, MAP_TAG is used in its place.
        if (args.Length==1){
            bigArg = MAP_TAG;

        } else if(args.Length>1){
            bigArg = BuildEntries(args, 1, args.Length," ");
        }

        if(pilot_cmd == "NEW_PLANET"){
            Echo(bigArg);
            Planet newPlanet = new Planet(bigArg, unchartedPlanets);
            newPlanet.SetPoint(1, Me.GetPosition());
			            EntriesToData(planets, wayPoints, unchartedPlanets);

        } else if(pilot_cmd == "LOG_GPS"){
            String wpInput = "G0|" + bigArg + "|" + Vector3ToString(Me.GetPosition());
            WayPoint newGPS = new WayPoint(wpInput, wayPoints);
            			EntriesToData(planets, wayPoints, unchartedPlanets);

        } else if(pilot_cmd == "LOG_NEXT"){
            for(int u = 0; u<unchartedPlanets.Count; u++){
                Planet logPlanet = unchartedPlanets[u];
                if(logPlanet.GetName()==bigArg){
                    logNext(logPlanet);
                }
            }
            			EntriesToData(planets, wayPoints, unchartedPlanets);

        } else if(pilot_cmd == "UPDATE_PLANET"){
            for(int p = 0; p<planets.Count; p++){
                Planet logPlanet = planets[p];
                if(logPlanet.GetName()==bigArg){
                    logNext(logPlanet);
                }
            }
            			EntriesToData(planets, wayPoints, unchartedPlanets);

        } else{
            for(int c=0; c<lcds.Count(); c++){
                IMyTextPanel lcd = lcds[c] as IMyTextPanel;
                String lcdName = lcd.CustomName;
                if(lcdName.Contains(bigArg)&&lcdName.Contains(MAP_TAG)){

                    switch(pilot_cmd){
                        case "ZOOM_IN":
                            zoomIn(lcd);
                            break;
                        case "ZOOM_OUT":
                            zoomOut(lcd);
                            break;
                        case "FLIP_RIGHT":
                            flipRight(lcd);
                            break;
                        case "FLIP_LEFT":
                            flipLeft(lcd);
                            break;
                        case "FLIP_UP":
                            flipUp(lcd);
                            break;
                        case "FLIP_DOWN":
                            flipDown(lcd);
                            break;
                        case "MOVE_LEFT":
                            moveLeft(lcd);
                            	break;
                        	case "MOVE_RIGHT":
                            moveRight(lcd);
                            break;
                        	case "MOVE_UP":
                            moveUp(lcd);
                            	break;
                        case "MOVE_DOWN":
                            moveDown(lcd);
                            break;
                        						case "CENTER_MAP":
                            centerMap(lcd);
                            break;
                        case "SHIP_MODE":
                            shipMode(lcd);
                            break;
                        case "WORLD_MODE":
                            worldMode(lcd);
                            break;
                        case "TOGGLE_MODE":
                            toggleMode(lcd);
                            break;
                    }
                }
            //Update Planets & GPS on LCD
            mapBuild(lcd);
            }
        }
    } else{
        for(int d = 0; d<lcds.Count; d++){
            IMyTextPanel lcd = lcds[d] as IMyTextPanel;
            mapPrint(lcd, cockpit);
        }
    } 
}

    /////////////////////
  /// PLOT PLANET ///
/////////////////////

public void plotPlanet(Planet planet, char[,] array, int[] n, int[] m, Vector3 origin, int scale)
{
    float radius = planet.GetRadius()/scale;
    Vector3 center = planet.GetCenter();
    float nCenter = (center.GetDim(n[0])-origin.GetDim(n[0]))*n[1]/scale + HORZ;
    float mCenter = (center.GetDim(m[0])-origin.GetDim(m[0]))*m[1]/scale + VERT;
    int nCent = (int) nCenter;
    int mCent = (int) mCenter;

//DECLARE CHARACTERS THAT WILL MAKE UP PLANETS
    char planetGrid;
    char planetBody;
    char equator;
    String planetColor = planet.GetColor().ToLower();
    
//SET COLOR SCHEME OF PLANET
    switch(planetColor)
    {
        case "gray":
            planetBody = DARK_GRAY;
            planetGrid = MED_GRAY;
            equator = LIGHT_GRAY;
            break;
        case "grey":
            planetBody = DARK_GRAY;
            planetGrid = MED_GRAY;
            equator = LIGHT_GRAY;
            break;
        case "red":
            planetBody = DARK_RED;
            planetGrid = MED_RED;
            equator = RED;
            break;
        case "blue":
            planetBody = DARK_BLUE;
            planetGrid = MED_BLUE;
            equator = BLUE;
            break;
        case "green":
            planetBody = DARK_GREEN;
            planetGrid = MED_GREEN;
            equator = GREEN;
            break;
        case "yellow":
            planetBody = DARK_YELLOW;
            planetGrid = MED_YELLOW;
            equator = YELLOW;
            break;
        case "magenta":
            planetBody = DARK_MAGENTA;
            planetGrid = MED_MAGENTA;
            equator = MAGENTA;
            break;
        case "cyan":
            planetBody = DARK_CYAN;
            planetGrid = MED_CYAN;
            equator = CYAN;
            break;
        case "white":
            planetBody = WHITE;
            planetGrid = CYAN;
            equator = LIGHT_GRAY;
            break;
        case "wire":
            planetBody = BLACK;
            planetGrid = DARK_GREEN;
            equator = MED_GREEN;
            break;
        default:
            planetBody = BLACK;
            planetGrid = LIGHT_GRAY;
            equator = WHITE;
            break;
    }

    if(nCenter+radius>=0 || nCenter-radius<=WIDTH || mCenter+radius>=0 || mCenter-radius<=HEIGHT)
    {
        for(int row = 0; row<HEIGHT; row++)
        {
            for(int col = 0; col<WIDTH; col++)
            {
                float x = col-nCenter;
                double x2 = col - nCenter/Math.Sqrt(2);
                double x3 = col - nCenter*1.85/Math.Sqrt(2);
                float y = row-mCenter;

                //USED FOR LATITUDE LINES
                double y30 = mCenter+radius/2;
                double y30n = mCenter-radius/2; //Negative 30 degrees Latitude
                double y60 = mCenter+radius*Math.Sqrt(3)/2;
                double y60n = mCenter-radius*Math.Sqrt(3)/2; //Negative 60 degrees Latitude
                int thickA = THICK/scale;
                int thickB = THICK2/scale;

                if(radius<4 && mCent > 2 && mCent < HEIGHT-3 && nCent > 2 && nCent < WIDTH-3)
                {
                //PLOTS PLANET ICON IF SCALE IS TOO SMALL
                    array[mCent+3,nCent-1] = planetGrid;         //01
                    array[mCent+3,nCent] = planetGrid;           //02
                    array[mCent+3,nCent+1] = planetGrid;        //03
                    array[mCent-3,nCent-1] = planetGrid;          //04
                    array[mCent-3,nCent] = planetGrid;            //05
                    array[mCent-3,nCent+1] = planetGrid;         //06
                    array[mCent-1,nCent+3] = planetGrid;         //07
                    array[mCent,nCent+3] = planetGrid;           //08
                    array[mCent+1,nCent+3] = planetGrid;        //09
                    array[mCent-1,nCent-3] = planetGrid;           //10
                    array[mCent,nCent-3] = planetGrid;              //11
                    array[mCent+1,nCent-3] = planetGrid;          //12
                    array[mCent-2,nCent-2] = planetGrid;          //13
                    array[mCent+2,nCent+2] = planetGrid;        //14
                    array[mCent-2,nCent+2] = planetGrid;        //15
                    array[mCent+2,nCent-2] = planetGrid;        //16

                    array[mCent+2,nCent-1] = planetBody;                //01
                    array[mCent+2,nCent] = planetBody;                  //02
                    array[mCent+2,nCent+1] = planetBody;               //03
                    array[mCent-2,nCent-1] = planetBody;                 //04
                    array[mCent-2,nCent] = planetBody;                   //05
                    array[mCent-2,nCent+1] = planetBody;                //06
                    array[mCent-1,nCent-2] = planetBody;                 //07
                    array[mCent-1,nCent-1] = planetBody;                  //08
                    array[mCent-1,nCent] = planetBody;                    //09
                    array[mCent-1,nCent+1] = planetBody;                  //10
                    array[mCent-1,nCent+2] = planetBody;                  //11
                    array[mCent+1,nCent-2] = planetBody;                 //12
                    array[mCent+1,nCent-1] = planetBody;                  //13
                    array[mCent+1,nCent] = planetBody;                    //14
                    array[mCent+1,nCent+1] = planetBody;                 //15
                    array[mCent+1,nCent+2] = planetBody;                //16
                    array[mCent,nCent-2] = planetBody;                    //17
                    array[mCent,nCent-1] = planetBody;                     //18
                    array[mCent,nCent] = planetBody;                       //19
                    array[mCent,nCent+1] = planetBody;                   //20
                    array[mCent,nCent+2] = planetBody;                   //21

                }
                else if(x*x+y*y<radius*radius)
                {
                    if(row==(int) mCenter)
                    {
                    //PLOTS EQUATOR
                        array[row,col] = equator;
                    }
                    else if(Math.Abs(x*x+y*y-radius*radius)<thickA)
                    {
                    //PLOTS BORDER AROUND PLANET
                        array[row,col] = planetGrid;
                    }
                    else if(col==(int) nCenter || Math.Abs(y*y*0.75+x2*x2-radius*radius)<thickB || Math.Abs(y*y*0.75+x3*x3-radius*radius)<thickB)
                    {
                    //PLOTS LONGITUDE LINES
                        array[row,col] = planetGrid;
                    }
                    else if(row == (int) y30 || row == (int) y30n || row == (int) y60 || row == (int) y60n)
                    {
                    //PLOTS LATITUDE LINES
                        array[row,col] = planetGrid;
                    }
                    else
                    {
                    //FILLS IN PLANET BODY
                        array[row,col] = planetBody;
                    }
                }
            }
        }
    }
}

    ////////////////////////
  /// PLOT WAYPOINT ///
////////////////////////

public void plotWayPoint(WayPoint wayPoint, char[,] array, int[] n, int[] m, Vector3 origin, int scale)
{
    float nCenter = origin.GetDim(n[0]);
    float mCenter = origin.GetDim(m[0]);

    Vector3 location = wayPoint.GetLocation();
    float col = (location.GetDim(n[0])-nCenter)*n[1]/scale + HORZ;
    float row = (location.GetDim(m[0])-mCenter)*m[1]/scale + VERT;

    int x = (int) col;
    int y = (int) row;

    Echo (col.ToString() + row.ToString());

    if(row>1 && col>1 && row<HEIGHT-1 && col<WIDTH-1)
    {
        //prints a 3x3 square box centered at the waypoint location.


        array[y-1, x] = WHITE;
        array[y-1, x+1] = WHITE;
        array[y-1, x-1] = WHITE;
        array[y, x-1] = WHITE;
        array[y, x+1] = WHITE;
        array[y+1, x-1] = WHITE;
        array[y+1, x] = WHITE;
        array[y+1, x+1] = WHITE;
    }
}


    //////////////////
  /// PLOT SHIP ///
//////////////////

public void plotShip(IMyShipController ship, char[,] array, int[] n, int[] m, Vector3 origin, int scale)
{
    char pointer = YELLOW;
    char pointer2 = DARK_YELLOW;

    float nCenter = origin.GetDim(n[0]);
    float mCenter = origin.GetDim(m[0]);

    Vector3 position = Me.GetPosition();
    float  col = (position.GetDim(n[0])-nCenter)*n[1]/scale+HORZ;
    float row = (position.GetDim(m[0])-mCenter)*m[1]/scale+VERT;

    int x = (int) col;
    int y = (int) row;

 //float row = (location.GetDim(m[0])-origin.GetDim(m[0]))*m[1]/scale + VERT; //Comparison to GetWaypoint()

    if(row>MARGIN && row<HEIGHT-MARGIN)
    {
        if(col>MARGIN && col<WIDTH-MARGIN)
        {
        //Get ship's directional vector and break it down to its relevent components.
            Vector3 heading = ship.WorldMatrix.Forward;
            double adj = heading.GetDim(n[0])*n[1];
            double opp = heading.GetDim(m[0])*m[1];
            double angle = Math.Atan2(opp,adj);

            if(Math.Abs(angle)<0.3927)
            {
            //LOCAL EAST ORIENTATION
                array[y+4, x-2] = pointer2;
                array[y+3, x-2] = pointer2;
                array[y+3, x-1] = pointer2;
                array[y+2, x-2] = pointer2;
                array[y+2, x-1] = pointer;
                array[y+2, x] = pointer2;
                array[y+1, x-2] = pointer2;
                array[y+1, x-1] = pointer;
                array[y+1, x] = pointer;
                array[y+1, x+1] = pointer2;
                array[y, x-2] = pointer2;
                array[y, x-1] = pointer;
                array[y, x] = pointer;
                array[y, x+1] = pointer;
                array[y, x+2] = pointer2;
                array[y-1, x-2] = pointer2;
                array[y-1, x-1] = pointer;
                array[y-1, x] = pointer;
                array[y-1, x+1] = pointer2;
                array[y-2, x-2] = pointer2;
                array[y-2, x-1] = pointer;
                array[y-2, x] = pointer2;
                array[y-3, x-2] = pointer2;
                array[y-3, x-1] = pointer2;
                array[y-4, x-2] = pointer2;
            }
            else if(angle>=0.3927 && angle<1.1781)
            {
            //LOCAL NE ORIENTATION
                array[y+1,x-4] = pointer2;
                array[y+1,x-3] = pointer2;
                array[y+1,x-2] = pointer2;
                array[y+1,x-1] = pointer2;
                array[y+1,x] = pointer2;
                array[y+1,x+1] = pointer2;
                array[y,x-4] = pointer2;
                array[y,x-3] = pointer;
                array[y,x-2] = pointer;
                array[y,x-1] = pointer;
                array[y,x] = pointer;
                array[y,x+1] = pointer2;
                array[y-1,x-3] = pointer2;
                array[y-1,x-2] = pointer;
                array[y-1,x-1] = pointer;
                array[y-1,x] = pointer;
                array[y-1,x+1] = pointer2;
                array[y-2,x-2] = pointer2;
                array[y-2,x-1] = pointer;
                array[y-2,x] = pointer;
                array[y-2,x+1] = pointer2;
                array[y-3,x-1] = pointer2;
                array[y-3,x] = pointer;
                array[y-3,x+1] = pointer2;
                array[y-4,x] = pointer2;
                array[y-4,x+1] = pointer2;
            }
            else if(angle>=1.1781 && angle<1.9635)
            {
            //LOCAL NORTH ORIENTATION
                array[y+2,x] = pointer2; 
                array[y+1,x-1] = pointer2;
                array[y+1,x] = pointer; 
                array[y+1,x+1] = pointer2;
                array[y,x-2] = pointer2;
                array[y,x-1] = pointer;
                array[y,x] = pointer;
                array[y,x+1] = pointer;
                array[y,x+2] = pointer2;
                array[y-1,x-3] = pointer2;
                array[y-1,x-2] = pointer;
                array[y-1,x-1] = pointer;
                array[y-1,x] = pointer;
                array[y-1,x+1] = pointer;
                array[y-1,x+2] = pointer;
                array[y-1,x+3] = pointer2;
                array[y-2,x-4] = pointer2;
                array[y-2,x-3] = pointer2;
                array[y-2,x-2] = pointer2;
                array[y-2,x-1] = pointer2;
                array[y-2,x] = pointer2;
                array[y-2,x+1] = pointer2;
                array[y-2,x+2] = pointer2;
                array[y-2,x+3] = pointer2;
                array[y-2,x+4] = pointer2;
            }
            else if(angle>=1.9635 && angle<2.7489)
            {
            //LOCAL NW ORIENTATION
                array[y+1,x+4] = pointer2;
                array[y+1,x+3] = pointer2;
                array[y+1,x+2] = pointer2;
                array[y+1,x+1] = pointer2;
                array[y+1,x] = pointer2;
                array[y+1,x-1] = pointer2;
                array[y,x+4] = pointer2;
                array[y,x+3] = pointer;
                array[y,x+2] = pointer;
                array[y,x+1] = pointer;
                array[y,x] = pointer;
                array[y,x-1] = pointer2;
                array[y-1,x+3] = pointer2;
                array[y-1,x+2] = pointer;
                array[y-1,x+1] = pointer;
                array[y-1,x] = pointer;
                array[y-1,x-1] = pointer2;
                array[y-2,x+2] = pointer2;
                array[y-2,x+1] = pointer;
                array[y-2,x] = pointer;
                array[y-2,x-1] = pointer2;
                array[y-3,x+1] = pointer2;
                array[y-3,x] = pointer;
                array[y-3,x-1] = pointer2;
                array[y-4,x] = pointer2;
                array[y-4,x-1] = pointer2;
            }
            else if(Math.Abs(angle)>=2.7489)
            {
            //LOCAL WEST ORIENTATION
                array[y+4, x+2] = pointer2;
                array[y+3, x+2] = pointer2;
                array[y+3, x+1] = pointer2;
                array[y+2, x+2] = pointer2;
                array[y+2, x+1] = pointer;
                array[y+2, x] = pointer2;
                array[y+1, x+2] = pointer2;
                array[y+1, x+1] = pointer;
                array[y+1, x] = pointer;
                array[y+1, x-1] = pointer2;
                array[y, x+2] = pointer2;
                array[y, x+1] = pointer;
                array[y, x] = pointer;
                array[y, x-1] = pointer;
                array[y, x-2] = pointer2;
                array[y-1, x+2] = pointer2;
                array[y-1, x+1] = pointer;
                array[y-1, x] = pointer;
                array[y-1, x-1] = pointer2;
                array[y-2, x+2] = pointer2;
                array[y-2, x+1] = pointer;
                array[y-2, x] = pointer2;
                array[y-3, x+2] = pointer2;
                array[y-3, x+1] = pointer2;
                array[y-4, x+2] = pointer2;
            }
            else if(angle>-2.7489 && angle<=-1.9635)
            {
            //LOCAL SW ORIENTATION
                array[y-1,x+4] = pointer2;
                array[y-1,x+3] = pointer2;
                array[y-1,x+2] = pointer2;
                array[y-1,x+1] = pointer2;
                array[y-1,x] = pointer2;
                array[y-1,x-1] = pointer2;
                array[y,x+4] = pointer2;
                array[y,x+3] = pointer;
                array[y,x+2] = pointer;
                array[y,x+1] = pointer;
                array[y,x] = pointer;
                array[y,x-1] = pointer2;
                array[y+1,x+3] = pointer2;
                array[y+1,x+2] = pointer;
                array[y+1,x+1] = pointer;
                array[y+1,x] = pointer;
                array[y+1,x-1] = pointer2;
                array[y+2,x+2] = pointer2;
                array[y+2,x+1] = pointer;
                array[y+2,x] = pointer;
                array[y+2,x-1] = pointer2;
                array[y+3,x+1] = pointer2;
                array[y+3,x] = pointer;
                array[y+3,x-1] = pointer2;
                array[y+4,x] = pointer2;
                array[y+4,x-1] = pointer2;
            }
            else if(angle>-1.9635 && angle<=-1.1781)
            {
            //LOCAL SOUTH ORIENTATION
                array[y-2,x] = pointer2; 
                array[y-1,x-1] = pointer2;
                array[y-1,x] = pointer; 
                array[y-1,x+1] = pointer2;
                array[y,x-2] = pointer2;
                array[y,x-1] = pointer;
                array[y,x] = pointer;
                array[y,x+1] = pointer;
                array[y,x+2] = pointer2;
                array[y+1,x-3] = pointer2;
                array[y+1,x-2] = pointer;
                array[y+1,x-1] = pointer;
                array[y+1,x] = pointer;
                array[y+1,x+1] = pointer;
                array[y+1,x+2] = pointer;
                array[y+1,x+3] = pointer2;
                array[y+2,x-4] = pointer2;
                array[y+2,x-3] = pointer2;
                array[y+2,x-2] = pointer2;
                array[y+2,x-1] = pointer2;
                array[y+2,x] = pointer2;
                array[y+2,x+1] = pointer2;
                array[y+2,x+2] = pointer2;
                array[y+2,x+3] = pointer2;
                array[y+2,x+4] = pointer2;
            }
            else
            {
            //LOCAL SE ORIENTATION
                array[y-1,x-4] = pointer2;
                array[y-1,x-3] = pointer2;
                array[y-1,x-2] = pointer2;
                array[y-1,x-1] = pointer2;
                array[y-1,x] = pointer2;
                array[y-1,x+1] = pointer2;
                array[y,x-4] = pointer2;
                array[y,x-3] = pointer;
                array[y,x-2] = pointer;
                array[y,x-1] = pointer;
                array[y,x] = pointer;
                array[y,x+1] = pointer2;
                array[y+1,x-3] = pointer2;
                array[y+1,x-2] = pointer;
                array[y+1,x-1] = pointer;
                array[y+1,x] = pointer;
                array[y+1,x+1] = pointer2;
                array[y+2,x-2] = pointer2;
                array[y+2,x-1] = pointer;
                array[y+2,x] = pointer;
                array[y+2,x+1] = pointer2;
                array[y+3,x-1] = pointer2;
                array[y+3,x] = pointer;
                array[y+3,x+1] = pointer2;
                array[y+4,x] = pointer2;
                array[y+4,x+1] = pointer2;
            }
        }
        else if(col<=MARGIN)
        {
            array[y+2,0] = pointer2;
            array[y+2,1] = pointer2;
            array[y+1,0] = pointer;
            array[y+1,1] = pointer;
            array[y+1,2] = pointer2;
            array[y,0] = pointer;
            array[y,1] = pointer;
            array[y,2] = pointer2;
            array[y-1,0] = pointer;
            array[y-1,1] = pointer;
            array[y-1,2] = pointer2;
            array[y-2,0] = pointer2;
            array[y-2,1] = pointer2;
        }
        else if(col>=WIDTH-MARGIN)
        {
            array[y+2,WIDTH-1] = pointer2;
            array[y+2,WIDTH-2] = pointer2;
            array[y+1,WIDTH-1] = pointer;
            array[y+1,WIDTH-2] = pointer;
            array[y+1,WIDTH-3] = pointer2;
            array[y,WIDTH-1] = pointer;
            array[y,WIDTH-2] = pointer;
            array[y,WIDTH-3] = pointer2;
            array[y-1,WIDTH-1] = pointer;
            array[y-1,WIDTH-2] = pointer;
            array[y-1,WIDTH-3] = pointer2;
            array[y-2,WIDTH-1] = pointer2;
            array[y-2,WIDTH-2] = pointer2;
        }
    }
    else if(col>MARGIN && col<WIDTH-MARGIN)
    {
        if(row<=MARGIN)
        {
            array[0,x+2] = pointer2;
            array[0,x+1] = pointer;
            array[0,x] = pointer;
            array[0,x-1] = pointer;
            array[0,x-2] = pointer2;
            array[1,x+2] = pointer2;
            array[1,x+1] = pointer;
            array[1,x] = pointer;
            array[1,x-1] = pointer;
            array[1,x-2] = pointer2;
            array[2,x+1] = pointer2;
            array[2,x] = pointer2;
            array[2,x-1] = pointer2;
        }
        else if(row>=HEIGHT-MARGIN)
        {
            array[HEIGHT-1,x+2] = pointer2;
            array[HEIGHT-1,x+1] = pointer;
            array[HEIGHT-1,x] = pointer;
            array[HEIGHT-1,x-1] = pointer;
            array[HEIGHT-1,x-2] = pointer2;
            array[HEIGHT-2,x+2] = pointer2;
            array[HEIGHT-2,x+1] = pointer;
            array[HEIGHT-2,x] = pointer;
            array[HEIGHT-2,x-1] = pointer;
            array[HEIGHT-2,x-2] = pointer2;
            array[HEIGHT-3,x+1] = pointer2;
            array[HEIGHT-3,x] = pointer2;
            array[HEIGHT-3,x-1] = pointer2;
        }
    }
    else if(row<=MARGIN && col <= MARGIN)
    {
        array[0,0] = pointer;
        array[0,1] = pointer;
        array[0,2] = pointer2;
        array[1,0] = pointer;
        array[1,1] = pointer;
        array[1,2] = pointer2;
        array[2,0] = pointer2;
        array[2,1] = pointer2;
    }
    else if(row<=MARGIN && col >= WIDTH-MARGIN)
    {
        array[0,WIDTH-1] = pointer;
        array[0,WIDTH-2] = pointer;
        array[0,WIDTH-3] = pointer2;
        array[1,WIDTH-1] = pointer;
        array[1,WIDTH-2] = pointer;
        array[1,WIDTH-3] = pointer2;
        array[2,WIDTH-1] = pointer2;
        array[2,WIDTH-2] = pointer2;
    }
    else if(row>= HEIGHT-MARGIN && col <= MARGIN)
    {
        array[HEIGHT-1,0] = pointer;
        array[HEIGHT-1,1] = pointer;
        array[HEIGHT-1,2] = pointer2;
        array[HEIGHT-2,0] = pointer;
        array[HEIGHT-2,1] = pointer;
        array[HEIGHT-2,2] = pointer2;
        array[HEIGHT-3,0] = pointer2;
        array[HEIGHT-3,1] = pointer2;
    }
    else if(row>= HEIGHT-MARGIN && col >= WIDTH-MARGIN)
    {
        array[HEIGHT-1,WIDTH-1] = pointer;
        array[HEIGHT-1,WIDTH-2] = pointer;
        array[HEIGHT-1,WIDTH-3] = pointer2;
        array[HEIGHT-2,WIDTH-1] = pointer;
        array[HEIGHT-2,WIDTH-2] = pointer;
        array[HEIGHT-2,WIDTH-3] = pointer2;
        array[HEIGHT-3,WIDTH-1] = pointer2;
        array[HEIGHT-3,WIDTH-2] = pointer2;
    }
}



    /////////////////////
  /// CROSSHAIRS ///                Prints Crosshairs to lcd array. To be used in "Ship Mode".
/////////////////////

public void crossHairs(char[,] array)
{
    int halfWidth = WIDTH/2;
    int halfHeight = HEIGHT/2;
    for(int left = 0; left<halfWidth-CROSS_SPACE; left++)
    {
        array[halfHeight, left] = WHITE;
    }
    for(int right = halfWidth+CROSS_SPACE; right<WIDTH; right++)
    {
        array[halfHeight, right] = WHITE;
    }
    for(int high = 0; high<halfHeight-CROSS_SPACE; high++)
    {
        array[high, halfWidth] = WHITE;
    }
    for(int low = halfHeight+CROSS_SPACE; low<HEIGHT; low++)
    {
        array[low, halfWidth] = WHITE;
    }
}


    ///////////////////         Reads Public Title from LCD Block then prints ship
  /// MAP PRINT ///            to the appropriate viewing plane.
///////////////////

public void mapPrint(IMyTextPanel lcd, IMyShipController cockpit)
{
//GET LCD PARAMETERS FROM PUBLIC TITLE
    String title = lcd.GetPublicTitle();
    if(title == "Public title")
    {
        title = DEFAULT_DISPLAY;
        lcd.WritePublicTitle(title);
    }
    String[] lcdData = title.Split(' ');
    int[] nAxis = parseAxis(lcdData[0]);
    int[] mAxis = parseAxis(lcdData[1]);
    Vector3 mapCenter = StringToVector3(lcdData[2]);
    int mapScale =  int.Parse(lcdData[3]);
    String mapMode = lcdData[4].ToUpper();

//IF LCD SET TO CHASE MODE CALCULATES DISTANCE BETWEEN SHIP AND LCD CENTER AND CENTERS MAP AS NEEDED.
    if(mapMode=="SHIP")
    {
        Vector3 position = Me.GetPosition();
        float distX = Math.Abs(position.GetDim(0)-mapCenter.GetDim(0));
        float distY = Math.Abs(position.GetDim(1)-mapCenter.GetDim(1));
        float distZ = Math.Abs(position.GetDim(2)-mapCenter.GetDim(2));

        if(distX>mapScale || distY>mapScale || distZ>mapScale)
        {
            centerMap(lcd);
			mapBuild(lcd);
        }
    }

    char[,] lcdArr = new char[HEIGHT, WIDTH];
    	String blankMap = lcd.CustomData;
	
    	if(blankMap == "")
    {
	   //POPULATE MAP MATRIX WITH BLACK PIXELS AND GRID LINES IF  MAP IS BLANK
        		for(int p = 0; p<HEIGHT; p++)
        {
            int gridY = (p+5)%10;
            for(int q = 0; q<WIDTH; q++)
            {
				                int gridX = (q+5)%10;
                if(gridX == 0 || gridY == 0)
				                {
                				    	lcdArr [p,q] = GRID;
				                }
				                else
				                {
                		  lcdArr [p,q] = BLACK;
				                }
            }
        }
    }
    	else
    {
	   //COPY THE BLANK MAP STRING INTO LCD ARRAY
        String[] lines = blankMap.Split('\n');
        for(int row = HEIGHT-1; row>-1; row--)
        {
            char[] pixels = lines[row].ToCharArray();
            for(int col = 0; col<WIDTH; col++)
            {
				                lcdArr[row,col] = pixels[col];
            }
        		}
	   }

//PLOT SHIP TO LCD
    plotShip(cockpit, lcdArr, nAxis, mAxis, mapCenter, mapScale);

//BUILD PRINT STRING THEN PRINT
    string map = "";
    for (int i=HEIGHT-1; i>-1; i--)
    {
        for (int j=0; j<WIDTH; j++)
        {
            map = map + lcdArr[i,j];
        }
        map = map+"\n";
    }
    lcd.WritePublicText(map);  
}

    //////////////////
  /// MAP BUILD ///        Gets LCD parameters then builds a blank map.  Map is stored as string in LCD Custom Data field.
///////////////////
public void mapBuild(IMyTextPanel lcd)
{
//CREATE BLANK LCD ARRAY
    char[,] lcdArr = new char[HEIGHT, WIDTH];

//POPULATE MAP MATRIX WITH BLACK PIXELS AND GRID LINES
    for(int p = 0; p<HEIGHT; p++)
    {
        int gridY = (p+5)%10;
        for(int q = 0; q<WIDTH; q++)
        {
            int gridX = (q+5)%10;
            if(gridX == 0 || gridY == 0)
            {
                lcdArr [p,q] = GRID;
            }
            else
            {
                lcdArr [p,q] = BLACK;
            }
        }
    }
	
//GET LCD PARAMETERS FROM CUSTOM DATA
    String title = lcd.GetPublicTitle();
    if(title == "")
    {
        title = DEFAULT_DISPLAY;
        lcd.WritePublicTitle(title);
    }
    String[] lcdData = title.Split(' ');
    int[] nAxis = parseAxis(lcdData[0]);
    int[] mAxis = parseAxis(lcdData[1]);
    Vector3 mapCenter = StringToVector3(lcdData[2]);
    int mapScale =  int.Parse(lcdData[3]);

	String[] mapData = Me.CustomData.Split('\n');
	List<Planet> planets = new List<Planet>();
	List<WayPoint> wayPoints = new List<WayPoint>();
    	for(int m = 0; m<mapData.Length; m++)
    {
        String entity = mapData[m];
        if(entity.StartsWith("P"))
        {
            Planet planet = new Planet(entity, planets);
        }
        else if(entity.StartsWith("G"))
        {
            WayPoint wayPoint = new WayPoint(entity, wayPoints);
        }
    }
	
	
//PLOT PLANETS TO LCD
    if(planets.Count>0)
    {
        for(int p = 0; p<planets.Count; p++)
        {
            //Go through planet list.  Print visible planets.
            plotPlanet(planets[p], lcdArr, nAxis, mAxis, mapCenter, mapScale);
        }
    }

//PLOT WAYPOINTS TO LCD
    if(wayPoints.Count>0)
    {
        for(int w = 0; w<wayPoints.Count; w++)
        {
            //Go through waypoint list.  Print visible waypoints.
            plotWayPoint(wayPoints[w], lcdArr, nAxis, mAxis, mapCenter, mapScale);
        }
    }

//ADD CROSSHAIRS IF LCD IS IN SHIP MODE
    if(lcdData[4].ToUpper()=="SHIP")
    {
        crossHairs(lcdArr);
    }
	
//BUILD MAP INTO STRING AND WRITE TO CUSTOM DATA
    string map = "";
    for (int i=0; i<HEIGHT; i++)
    {
        for (int j=0; j<WIDTH; j++)
        {
            map = map + lcdArr[i,j];
        }
        map = map+"\n";
    }
    lcd.CustomData = map; 
}



    ////////////////////
  /// PARSE AXIS ///        Converts selected strings into an int array to be used for mapping the appropriate axis in mapPrint()
////////////////////

public int[] parseAxis(String dimension)
{
    int[] axis = new int[2];
    if(dimension == "X" || dimension == "x")
    {
        axis[0] = 0;
        axis[1] = 1;
    }
    else if(dimension == "-X" || dimension == "-x")
    {
        axis[0] = 0;
        axis[1] = -1;
    }
    else if(dimension == "Y" || dimension == "y")
    {
        axis[0] = 1;
        axis[1] = 1;
    }
    else if(dimension == "-Y" || dimension == "-y")
    {
        axis[0] = 1;
        axis[1] = -1;
    }
    else if(dimension == "Z" || dimension == "z")
    {
        axis[0] = 2;
        axis[1] = 1;
    }
    else if(dimension == "-Z" || dimension == "-z")
    {
        axis[0] = 2;
        axis[1] = -1;
    }
    else
    {
        axis[0] = 3;
        axis[1] = 3;
    }
    return axis;
}


    //////////////////
  /// FLIP RIGHT ///       Changes Axis settings of LCD to flip the view to the right.
//////////////////

void flipRight(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    String axes = data[0] + " " + data[1];
    String newData = "";

    for(int p = 0; p<6; p++)
    {
        for(int q = 0; q<6; q++)
        {
            if(axes==AXIS[p,q])
            {
                p = wrapAdd(p,1,6);
                if(AXIS[p,q]=="E")
                {
                    p = wrapAdd(p,1,6);
                }
                newData = AXIS[p,q];
                for(int r = 2; r<data.Length; r++)
                {
                    newData = newData + " " + data[r];
                }
                lcd.WritePublicTitle(newData);
                return;
            }
        }
    }
}


    /////////////////
  /// FLIP LEFT ///       Changes Axis settings of LCD to flip the view to the left.
/////////////////

void flipLeft(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    String axes = data[0] + " " + data[1];
    String newData = "";

    for(int p = 0; p<6; p++)
    {
        for(int q = 0; q<6; q++)
        {
            if(axes==AXIS[p,q])
            {
                p = wrapAdd(p,-1,6);
                if(AXIS[p,q]=="E")
                {
                    p = wrapAdd(p,-1,6);
                }
                newData = AXIS[p,q];
                for(int r = 2; r<data.Length; r++)
                {
                    newData = newData + " " + data[r];
                }
                lcd.WritePublicTitle(newData);
                return;
            }
        }
    }	
}

    ///////////////
  /// FLIP UP ///       Changes Axis settings of LCD to flip the view up.
///////////////

void flipUp(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    String axes = data[0] + " " + data[1];
    String newData = "";

    for(int p = 0; p<6; p++)
    {
        for(int q = 0; q<6; q++)
        {
            if(axes==AXIS[p,q])
            {
                q = wrapAdd(q,1,6);
                if(AXIS[p,q]=="E")
                {
                    q = wrapAdd(q,1,6);
                }
                newData = AXIS[p,q];
                for(int r = 2; r<data.Length; r++)
                {
                    newData = newData + " " + data[r];
                }
                lcd.WritePublicTitle(newData);
                return;
            }
        }
    }	
}

    ///////////////////
  /// FLIP DOWN ///       Changes Axis settings of LCD to flip the view down.
///////////////////

void flipDown(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    String axes = data[0] + " " + data[1];
    String newData = "";

    for(int p = 0; p<6; p++)
    {
        for(int q = 0; q<6; q++)
        {
            if(axes==AXIS[p,q])
            {
                q = wrapAdd(q,-1,6);
                if(AXIS[p,q]=="E")
                {
                    q = wrapAdd(q,-1,6);
                }
                newData = AXIS[p,q];
                for(int r = 2; r<data.Length; r++)
                {
                    newData = newData + " " + data[r];
                }
                lcd.WritePublicTitle(newData);
                return;
            }
        }
    }
}

    ////////////////
  /// ZOOM IN ///
////////////////

void zoomIn(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
  
    int scale = int.Parse(data[3]);
    int newScale = scale/2;
    if (newScale > MIN_SCALE)
    {
        scale = newScale;
    }
    else
    {
        scale = MIN_SCALE;
    }
    data[3] = newScale.ToString();

    String newData = data [0];
    for(int i = 1; i < data.Length; i++)
    {
        newData = newData + " " + data[i];
    }

    lcd.WritePublicTitle(newData);
}

    //////////////////
  /// ZOOM OUT ///
//////////////////

void zoomOut(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');

    int scale = int.Parse(data[3]);
    scale = scale*2;
    data[3]=scale.ToString();

    String newData = data [0];
    for(int i = 1; i < data.Length; i++)
    {
        newData = newData + " " + data[i];
    }

    lcd.WritePublicTitle(newData);
}

    //////////////////
  /// MOVE LEFT ///
///////////////////

void moveLeft(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');


    String mode = data[4].ToUpper();
    if(mode != "SHIP")
    {
        String newData = data[0];
        String axis = data[0].ToUpper();
        int scale = int.Parse(data[3]);
        int mod = 1;
        int dim=0;

        if(axis.StartsWith("-"))
        {
            mod = -1;
            axis = axis.Substring(1);
        }
        switch(axis)
        {
            case "X":
                dim = 0;
                break;
            case "Y":
                dim = 1;
                break;
            case "Z":
                dim = 2;
                break;
        }
        Vector3 center = StringToVector3(data[2]);
        float[] vec3 = new float[]{center.GetDim(0), center.GetDim(1), center.GetDim(2)}    ;

        vec3[dim] -= WIDTH*scale*mod/4;
        data[2] = "("+vec3[0]+","+vec3[1]+","+vec3[2]+")";
        for(int i = 1; i < data.Length; i++)
        {
            newData = newData + " " + data[i];
        }
        lcd.WritePublicTitle(newData);
    }
}

    ///////////////////
  /// MOVE RIGHT ///
////////////////////

void moveRight(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');


    String mode = data[4].ToUpper();
    if(mode != "SHIP")
    {
        String newData = data[0];
        String axis = data[0].ToUpper();
        int scale = int.Parse(data[3]);
        int mod = 1;
        int dim=0;

        if(axis.StartsWith("-"))
        {
            mod = -1;
            axis = axis.Substring(1);
        }
        switch(axis)
        {
            case "X":
                dim = 0;
                break;
            case "Y":
                dim = 1;
                break;
            case "Z":
                dim = 2;
                break;
        }
        Vector3 center = StringToVector3(data[2]);
        float[] vec3 = new float[]{center.GetDim(0), center.GetDim(1), center.GetDim(2)}    ;

        vec3[dim] += WIDTH*scale*mod/4;
        data[2] = "("+vec3[0]+","+vec3[1]+","+vec3[2]+")";
        for(int i = 1; i < data.Length; i++)
        {
            newData = newData + " " + data[i];
        }
        lcd.WritePublicTitle(newData);
    }
}


    ////////////////
  /// MOVE UP ///
/////////////////

void moveUp(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');

    if(data[4].ToUpper() != "SHIP")
    {
        String newData = data[0];
        String axis = data[1].ToUpper();
        int scale = int.Parse(data[3]);
        int mod = 1;
        int dim=0;

        if(axis.StartsWith("-"))
        {
            mod = -1;
            axis = axis.Substring(1);
        }
        switch(axis)
        {
            case "X":
                dim = 0;
                break;
            case "Y":
                dim = 1;
                break;
            case "Z":
                dim = 2;
                break;
        }
        Vector3 center = StringToVector3(data[2]);
        float[] vec3 = new float[]{center.GetDim(0), center.GetDim(1), center.GetDim(2)}    ;

        vec3[dim] += HEIGHT*scale*mod/4;
        data[2] = "("+vec3[0]+","+vec3[1]+","+vec3[2]+")";
        for(int i = 1; i < data.Length; i++)
        {
            newData = newData + " " + data[i];
        }
        lcd.WritePublicTitle(newData);
    }
}


    ////////////////////
  /// MOVE DOWN ///
/////////////////////

void moveDown(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');

    String mode = data[4].ToUpper();
    if(mode != "SHIP")
    {
        String newData = data[0];
        String axis = data[1].ToUpper();
        int scale = int.Parse(data[3]);
        int mod = 1;
        int dim=0;

        if(axis.StartsWith("-"))
        {
            mod = -1;
            axis = axis.Substring(1);
        }

        switch(axis)
        {
            case "X":
                dim = 0;
                break;
            case "Y":
                dim = 1;
                break;
            case "Z":
                dim = 2;
                break;
        }
        Vector3 center = StringToVector3(data[2]);
        float[] vec3 = new float[]{center.GetDim(0), center.GetDim(1), center.GetDim(2)}    ;

        vec3[dim] -= HEIGHT*scale*mod/4;
        data[2] = "("+vec3[0]+","+vec3[1]+","+vec3[2]+")";
        for(int i = 1; i < data.Length; i++)
        {
            newData = newData + " " + data[i];
        }
        lcd.WritePublicTitle(newData);
    }
}


    ////////////////////
  /// CENTER MAP ///            Centers Map on current position
////////////////////

void centerMap(IMyTextPanel lcd)
{
    Vector3 position = Me.GetPosition();

    String[] data = lcd.GetPublicTitle().Split(' ');
    String center = "("+position.GetDim(0)+","+position.GetDim(1)+","+position.GetDim(2)+")";
    data[2] = center;
    String newData = data[0];
    for(int i = 1; i<data.Length; i++)
    {
        newData = newData + " " +data[i];
    }
    lcd.WritePublicTitle(newData);
}


    ///////////////////
  /// SHIP MODE ///            Toggles LCD to "Ship" Mode:  Map will center on ship as it travels.
///////////////////

void shipMode(IMyTextPanel lcd)
{
    centerMap(lcd);
    String[] data = lcd.GetPublicTitle().Split(' ');
    data[4] = "SHIP";
    String newData = data[0];
    for(int i = 1; i<data.Length; i++)
    {
        newData = newData + " " +data[i];
    }
    lcd.WritePublicTitle(newData);

}


    /////////////////////
  /// WORLD MODE ///            Toggles LCD to "World" Mode:  Map remains fixed while ship moves across it.
/////////////////////

void worldMode(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    data[4] = "WORLD";
    String newData = data[0];
    for(int i = 1; i<data.Length; i++)
    {
        newData = newData + " " +data[i];
    }
    lcd.WritePublicTitle(newData);
}


    //////////////////////
  /// TOGGLE MODE ///            Toggles LCD between "World" and "Ship" modes.
//////////////////////

void toggleMode(IMyTextPanel lcd)
{
    String[] data = lcd.GetPublicTitle().Split(' ');
    String mode = data[4].ToUpper();

    if(mode=="WORLD")
    {
    //IF LCD IS IN WORLD MODE, CHANGE TO SHIP MODE.
        shipMode(lcd);
    }
    else
    {
    //IF LCD IS IN SHIP MODE OR THE STRING IS NOT RECOGNIZED, SWITCH TO WORLD MODE.
        worldMode(lcd);
    }
}


    //////////////////          Allows you to cycle continuously through an array or matrix. If you move past
  /// WRAP ADD ///           either end of the array, it returns you to the other side. (i.e. From last index to
///////////////////             first if you're going in the positive direction)

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


    /////////////////////////
  /// ENTRIES TO DATA ///
//////////////////////////

public void EntriesToData(List<Planet> planetList, List<WayPoint> gpsList, List<Planet> unchartedList)
{
    	String newData = "";

    	int planetCount = planetList.Count();
    	int gpsCount = gpsList.Count();
    int unchartedCount = unchartedList.Count();

    Echo(planetCount.ToString());
    Echo(gpsCount.ToString());
    Echo(unchartedCount.ToString());

	
    if(planetCount>0)
    {
        String[] planetEntries = new String[planetCount];
        for(int p = 0; p<planetCount; p++)
        {
            planetEntries[p] = planetList[p].ToString();
        }
        newData = BuildEntries(planetEntries, 0, planetCount, "\n");
    }

    if(gpsCount>0)
    {
        if(newData != "")
        {
            newData = newData+"\n";
        }
		
        String[] gpsEntries = new String[gpsCount];
        for(int g = 0; g<gpsCount; g++)
        {
            gpsEntries[g] = gpsList[g].ToString();
        }

        newData = newData + BuildEntries(gpsEntries, 0, gpsCount, "\n");
    		}
	
    if(unchartedCount>0)
    {
        if(newData != "")
        {
            newData = newData+"\n";
        }

        		String[] unchartedEntries = new String[unchartedCount];
        		for(int u = 0; u<unchartedCount; u++)
        {
            unchartedEntries[u] = unchartedList[u].ToString();
        }
        newData = newData + BuildEntries(unchartedEntries, 0, unchartedCount, "\n");
    }

    Me.CustomData=newData;
}


    //////////////////////
  /// BUILD ENTRIES ///
//////////////////////

public static String BuildEntries(String[] data,int start, int length, String split)
{
        String newData = data[start];
        start+=1;
 
        if(length>start)
        {
            for(int c = start; c<length; c++)
            {
                newData = newData + split + data[c];
            }
        }
        return newData;
}

   
    ////////////////////////////
  /// STRING TO VECTOR3 ///
////////////////////////////

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


    ////////////////////////////
  /// VECTOR3 TO STRING ///
////////////////////////////

public static String Vector3ToString(Vector3 vec3)
{
	String newData = "(" + vec3.GetDim(0)+"," + vec3.GetDim(1) + "," + vec3.GetDim(2) + ")";
	return newData;
}



//////////////////////
/// REPLACE COLUMN ///
//////////////////////

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
/// T-VALUE ///
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


    /////////////
  ///  Det4  ///          Gets determinant of a 4x4 Matrix
/////////////

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
  /// COORD ROW ///
////////////////////

public static double[] CoordRow(Vector3 vec3)
{
	double[] row = new double[4]{vec3.GetDim(0),vec3.GetDim(1),vec3.GetDim(2),1};
	return row;
}


    /////////////////
  /// LOG NEXT ///
/////////////////

	public void logNext(Planet planet){
		if(planet.GetPoint(1)==Vector3.Zero){
			planet.SetPoint(1, Me.GetPosition());
		}
		else if(planet.GetPoint(2)==Vector3.Zero){
planet.SetPoint(2, Me.GetPosition());
		}
		else if(planet.GetPoint(3)==Vector3.Zero){
			planet.SetPoint(3, Me.GetPosition());
		}
		else{
			planet.SetPoint(4, Me.GetPosition());
			planet.CalculatePlanet();
		}
}