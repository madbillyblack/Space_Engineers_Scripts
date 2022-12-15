using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
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

		// View Defaults
		const int DV_RADIUS = 262144; //Default View Radius
		const int DV_FOCAL = 256; //Default Focal Length
		const int DV_ALTITUDE = -15; //Default Altitude (angle)
		const int BRIGHTNESS_LIMIT = 4;


		// THERE IS NO REASON TO ALTER ANYTHING BELOW THIS LINE! //////////////////////////////////////////////////////////////////////////////////////////////////////////


		// OTHER CONSTANTS //
		const int BAR_HEIGHT = 20; //Default height of parameter bars
		const int TOP_MARGIN = 8; // Margin for top and bottom of frame
		const int SIDE_MARGIN = 15; // Margin for sides of frame
		const int MAX_VALUE = 1073741824; //General purpose MAX value = 2^30
		
		const string SLASHES = " //////////////////////////////////////////////////////////////";

		//const string DEFAULT_SETTINGS = "[Map Settings]\nMAP_Tag=[MAP]\nMAP_Index=0\nData_Tag=[Map Data]\nGrid_ID=\nData_Index=0\nReference_Name=[Reference]\nSlow_Mode=false\nCycle_Step=5\nPlanet_List=\nWaypoint_List=\n";
		const string PROGRAM_HEAD = "Map Settings";

		string[] _cycleSpinner = { "--", " / ", " | ", " \\ " };



		// GLOBALS //
		string _mapTag;
		string _previousCommand;
		bool _lightOn;
		static bool _planets;
		bool _planetToLog;
		bool _slowMode = false;
		const int CYCLE_LENGTH = 5;
		static int _cycleStep;
		static int _cycleOffset;
		int _sortCounter = 0;
		string _activePlanet = "";
		static string _clipboard = "";
		Vector3 _myPos;
		List<IMyTerminalBlock> _mapBlocks;
		
		static List<string> _messages;
		const int MESSAGE_LIMIT = 20;

		Planet _nearestPlanet;

		IMyTerminalBlock _refBlock;


		// PROGRAM ///////////////////////////////////////////////////////////////////////////////////////////////
		public Program()
		{
			_cycleOffset = Math.Abs((int) Me.CubeGrid.EntityId % CYCLE_LENGTH);

			_planetToLog = false;

			Build();

			_previousCommand = "NEWLY LOADED";
		}


		public void Save()
		{
			//String saveData = "";
			//Storage = saveData;
		}


		// MAIN ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public void Main(string argument)
		{
			_planets = _planetList.Count > 0;
			_myPos = _refBlock.GetPosition();

			Echo("////// PLANET MAP 3D ////// " + _cycleSpinner[_cycleStep % _cycleSpinner.Length]);
			Echo("Cmd: " + _previousCommand + "\n");

			EchoMessages();

			// Display Data from Planet Scanning System
			DisplayScanData();

			//Echo("Cycle Offset" + _cycleOffset);

			Echo("\nMAPS: " + _mapList.Count + "\nMENUS: " + _mapMenus.Count + "\nDATA DISPLAYS: " + _dataDisplays.Count);

			if (_planets)
			{
				Echo("Planet Count: " + _planetList.Count);
				//Planet planet = _planetList[_planetList.Count - 1];
			}
			else
			{
				Echo("No Planets Logged!");
			}

			if (_waypointList.Count > 0)
			{
				Echo("GPS Count: " + _waypointList.Count + "\n");
				//Waypoint waypoint = _waypointList[_waypointList.Count - 1];
			}
			else
			{
				Echo("No Waypoints Logged!");
			}

			ShowMenuData();


			if (_mapList.Count > 0)
			{
				CycleExecute();
				ButtonTimer();

				if (argument != "")
				{
					MainSwitch(argument);
				}

				if (_planets)
				{
					if (_cycleStep == CYCLE_LENGTH || _previousCommand == "NEWLY LOADED")
					{
						SortByNearest(_planetList);
					}
					_nearestPlanet = _planetList[0];
				}

				DrawMaps();
			}
			else
			{
				SetGridID("");

				if (_mapList.Count < 1)
					AddMessage("NO MAP DISPLAY FOUND!\nPlease add tag " + _mapTag + " to desired block.");
			}

			UpdateDisplays();
		}


		// VIEW FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		// SHOW //
		void Show(List<StarMap> maps, string attribute, int state)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				switch (attribute)
				{
					case "GPS":
						if (state == 3)
						{
							cycleGPSForList(maps);
						}
						else
						{
							map.GpsState = state;
						}
						break;
					case "NAMES":
						map.ShowNames = setState(map.ShowNames, state);
						break;
					case "SHIP":
						map.ShowShip = setState(map.ShowShip, state);
						break;
					case "INFO":
						map.ShowInfo = setState(map.ShowInfo, state);
						break;
					default:
						AddMessage("INVALID DISPLAY COMMAND");
						break;
				}

				//MapToParameters(map);
			}
		}


		// CYCLE GPS //
		void cycleGPS(StarMap map)
		{
			map.GpsState++;
			if (map.GpsState > 2)
				map.GpsState = 0;
		}

		void cycleGPSForList(List<StarMap> maps)
		{
			if (NoMaps(maps))
				return;

			foreach (StarMap map in maps)
			{
				cycleGPS(map);
			}
		}


		// DATA TO LOG //
		public void DataToLog()
		{
			MyIni mapIni = DataToIni(Me);

			if (_waypointList.Count > 0)
			{
				String waypointData = "";
				foreach (Waypoint waypoint in _waypointList)
				{
					waypointData += WaypointToString(waypoint) + "\n";
				}
				mapIni.Set(PROGRAM_HEAD, "Waypoint_List", waypointData);
			}

			String planetData = "";
			if (_planetList.Count > 0)
			{
				foreach (Planet planet in _planetList)
				{
					planetData += planet.ToString() + "\n";
				}
			}

			if (_unchartedList.Count > 0)
			{
				foreach (Planet uncharted in _unchartedList)
				{
					planetData += uncharted.ToString() + "\n";
				}
			}

			if (planetData != "")
			{
				mapIni.Set(PROGRAM_HEAD, "Planet_List", planetData);
			}

			Me.CustomData = mapIni.ToString();
		}


		// CLIPBOARD TO LOG //
		void ClipboardToLog(string markerType, string clipboard)
		{
			string[] waypointData = clipboard.Split(':');
			if (waypointData.Length < 6)
			{
				AddMessage("Does not match GPS format:/nGPS:<name>:X:Y:Z:<color>:");
				return;
			}

			Vector3 position = new Vector3(float.Parse(waypointData[2]), float.Parse(waypointData[3]), float.Parse(waypointData[4]));
			LogWaypoint(waypointData[1], position, markerType, waypointData[5]);
		}


		// LOG TO CLIPBOARD //
		string LogToClipboard(string waypointName)
		{
			Waypoint waypoint = GetWaypoint(waypointName);
			if (waypoint == null)
			{
				AddMessage("No waypoint " + waypointName + " found!");
				return _messages[_messages.Count - 1];
			}

			Vector3 location = waypoint.position;
			string output = "GPS:" + waypoint.name + ":" + location.X + ":" + location.Y + ":" + location.Z + ":#FF75C9F1:";

			return output;
		}


		// LOG WAYPOINT //
		public void LogWaypoint(String waypointName, Vector3 position, String markerType, String waypointColor)
		{
			if (waypointName == "")
			{
				AddMessage("No Waypoint Name Provided! Please Try Again.");
				return;
			}

			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint != null)
			{
				AddMessage("Waypoint " + waypointName + " already exists! Please choose different name.");
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
			foreach (StarMap map in _mapList)
			{
				UpdateMap(map);
			}
		}


		// SET WAYPOINT STATE //
		public void SetWaypointState(String waypointName, int state)
		{
			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint == null)
			{
				WaypointError(waypointName);
				return;
			}

			//State Switch: 0 => Deactivate, 1 => Activate, 2 => Toggle
			switch (state)
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
					AddMessage("Waypoint deleted: " + waypointName);
					break;
				default:
					AddMessage("Invalid waypoint state int!");
					break;
			}

			DataToLog();
		}


		// PLOT JUMP POINT //
		void PlotJumpPoint(string planetName)
		{
			Planet planet = GetPlanet(planetName);
			{
				if (planet == null)
				{
					PlanetError(planetName);
				}
			}

			int designation = 1;
			string name = planet.name + " Orbit ";
			Waypoint jumpPoint = GetWaypoint(name + designation);

			while (jumpPoint != null)
			{
				designation++;
				jumpPoint = GetWaypoint(name + designation);
			}

			Vector3 position = planet.position + (_myPos - planet.position) / Vector3.Distance(_myPos, planet.position) * planet.radius * JUMP_RATIO;

			LogWaypoint(name + designation, position, "WAYPOINT", "WHITE");
		}


		// PROJECT POINT//
		void ProjectPoint(string marker, string arg)
		{
			string[] args = arg.Split(' ');

			if (args.Length < 2)
			{
				AddMessage("INSUFFICIENT ARGUMENT!\nPlease include arguments <DISTANCE(in meters)> <WAYPOINT NAME>");
				return;
			}

			int distance;
			if (int.TryParse(args[0], out distance))
			{
				string name = "";
				for (int i = 1; i < args.Length; i++)
				{
					name += args[i] + " ";
				}

				Vector3 location = _myPos + _refBlock.WorldMatrix.Forward * distance;

				LogWaypoint(name.Trim(), location, marker, "WHITE");

				return;
			}

			AddMessage("DISTANCE ARGEMENT FAILED!\nPlease include Distance in meters. Do not include unit.");
		}


		// PLANET ERROR //
		void PlanetError(string name)
		{
			AddMessage("No planet " + name + " found!");
		}


		// WAYPOINT ERROR //
		void WaypointError(string name)
		{
			AddMessage("No waypoint " + name + " found!");
		}


		// DELETE PLANET //
		public void DeletePlanet(String planetName)
		{
			Planet alderaan = GetPlanet(planetName);

			if (alderaan == null)
			{
				PlanetError(planetName);
				return;
			}

			//_unchartedList.Remove(alderaan);
			_planetList.Remove(alderaan);
			DataToLog();
			AddMessage("PLANET DELETED: " + planetName + "\n\nDon't be too proud of this TECHNOLOGICAL TERROR you have constructed. The ability to DESTROY a PLANET is insignificant next to the POWER of the FORCE.");


			_planets = _planetList.Count > 0;

			if (_planets)
				_nearestPlanet = _planetList[0];
		}


		// SET PLANET COLOR //
		void SetPlanetColor(String argument)
		{
			String[] args = argument.Split(' ');
			String planetColor = args[0];

			if (args.Length < 2)
			{
				AddMessage("Insufficient Argument.  COLOR_PLANET requires COLOR and PLANET NAME.");
			}
			else
			{
				String planetName = "";
				for (int p = 1; p < args.Length; p++)
				{
					planetName += args[p] + " ";
				}
				planetName = planetName.Trim(' ').ToUpper();

				Planet planet = GetPlanet(planetName);

				if (planet != null)
				{
					planet.color = planetColor;
					AddMessage(planetName + " color changed to " + planetColor);
					DataToLog();
					return;
				}

				PlanetError(planetName);
			}
		}


		// SET WAYPOINT COLOR //
		void SetWaypointColor(String argument)
		{
			String[] args = argument.Split(' ');
			String waypointColor = args[0];

			if (args.Length < 2)
			{
				AddMessage("Insufficient Argument.  COLOR_WAYPOINT requires COLOR and WAYPOINT NAME.");
			}
			else
			{
				String waypointName = "";
				for (int w = 1; w < args.Length; w++)
				{
					waypointName += args[w] + " ";
				}
				waypointName = waypointName.Trim(' ').ToUpper();

				Waypoint waypoint = GetWaypoint(waypointName);

				if (waypoint != null)
				{
					waypoint.color = waypointColor;
					AddMessage(waypointName + " color changed to " + waypointColor);
					DataToLog();
					return;
				}

				WaypointError(waypointName);
			}
		}


		// SET WAYPOINT TYPE //
		void SetWaypointType(string arg, string waypointName)
		{
			Waypoint waypoint = GetWaypoint(waypointName);

			if (waypoint == null)
			{
				WaypointError(waypointName);
				return;
			}

			waypoint.marker = arg;
			DataToLog();
		}


		// TOOL FUNCTIONS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		// ON GRID // Checks if map blocks are part of current ship, even if merged
		bool onGrid(IMyTerminalBlock mapTerminal)
		{
			if (!mapTerminal.IsSameConstructAs(Me))
				return false;

			string iniGrid = GetKey(mapTerminal, SHARED, "Grid_ID", Me.CubeGrid.EntityId.ToString());

			if (iniGrid == _gridID)
				return true;
			else
				return false;
		}


		// DATA TO INI //
		MyIni DataToIni(IMyTerminalBlock block)
		{
			MyIni iniOuti = new MyIni();

			MyIniParseResult result;
			if (!iniOuti.TryParse(block.CustomData, out result))
				throw new Exception(result.ToString());

			return iniOuti;
		}


		// COLOR SWITCH //
		Color ColorSwitch(string colorString, bool isWaypoint)
		{
			colorString = colorString.ToUpper();

			if (colorString.StartsWith("#"))
				return HexToColor(colorString);

			Color colorOut = new Color(8, 8, 8);
			if (isWaypoint)
				colorOut = Color.White;

			switch (colorString)
			{
				case "RED":
					colorOut = new Color(32, 0, 0);
					break;
				case "GREEN":
					colorOut = new Color(0, 32, 0);
					break;
				case "BLUE":
					colorOut = new Color(0, 0, 32);
					break;
				case "YELLOW":
					colorOut = new Color(127, 127, 26);
					break;
				case "MAGENTA":
					colorOut = new Color(64, 0, 64);
					break;
				case "PURPLE":
					colorOut = new Color(24, 0, 48);
					break;
				case "CYAN":
					colorOut = new Color(0, 32, 32);
					break;
				case "LIGHTBLUE":
					colorOut = new Color(32, 32, 96);
					break;
				case "ORANGE":
					colorOut = new Color(32, 16, 0);
					break;
				case "TAN":
					colorOut = new Color(153, 100, 48);
					break;
				case "BROWN":
					colorOut = new Color(38, 25, 12);
					break;
				case "RUST":
					colorOut = new Color(64, 20, 16);
					break;
				case "GRAY":
					colorOut = new Color(16, 16, 16);
					break;
				case "GREY":
					colorOut = new Color(16, 16, 16);
					break;
				case "WHITE":
					colorOut = new Color(64, 64, 64);
					if (isWaypoint)
						colorOut = Color.White;
					break;
				default:
					colorOut = new Color(8, 8, 8);
					break;
			}

			return colorOut;
		}


		// HEX TO COLOR //
		Color HexToColor(string hexString)
		{

			if (hexString.Length != 9 && hexString.Length != 7)
				return Color.White;
			int i = 3; // Starting index for ARGB format.

			if (hexString.Length == 7)
				i = 1; // Starting index for normal HEX.

			int r, g, b = 0;

			r = Convert.ToUInt16(hexString.Substring(i, 2), 16);
			g = Convert.ToUInt16(hexString.Substring(i + 2, 2), 16);
			b = Convert.ToUInt16(hexString.Substring(i + 4, 2), 16);

			return new Color(r, g, b);
		}


		// SET STATE // Sets specified boolean to true, false, or toggle
		bool setState(bool attribute, int state)
		{
			switch (state)
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


		// STRING TO WAYPOINT //
		public Waypoint StringToWaypoint(String argument)
		{
			Waypoint waypoint = new Waypoint();
			String[] wayPointData = argument.Split(';');
			if (wayPointData.Length > 3)
			{
				waypoint.name = wayPointData[0];
				waypoint.position = StringToVector3(wayPointData[1]);
				waypoint.marker = wayPointData[2];
				waypoint.isActive = wayPointData[3].ToUpper() == "ACTIVE";
			}

			if (wayPointData.Length < 5)
			{
				waypoint.color = "WHITE";
			}
			else
			{
				waypoint.color = wayPointData[4];
			}
			return waypoint;
		}


		// WAYPOINT TO STRING //
		public String WaypointToString(Waypoint waypoint)
		{
			String output = waypoint.name + ";" + Vector3ToString(waypoint.position) + ";" + waypoint.marker;

			String activity = "INACTIVE";
			if (waypoint.isActive)
			{
				activity = "ACTIVE";
			}
			output += ";" + activity + ";" + waypoint.color;

			return output;
		}


		// VECTOR3 TO STRING //
		public static string Vector3ToString(Vector3 vec3)
		{
			String newData = "(" + vec3.X + "," + vec3.Y + "," + vec3.Z + ")";
			return newData;
		}


		// STRING TO VECTOR3 //
		public static Vector3 StringToVector3(string sVector)
		{
            try
            {
				// Remove the parentheses
				if (sVector.StartsWith("(") && sVector.EndsWith(")"))
				{
					sVector = sVector.Substring(1, sVector.Length - 2);
				}

				// split the items
				string[] sArray = sVector.Split(',');

				// store as a Vector3
				Vector3 result = new Vector3(
					ParseFloat(sArray[0], 0),
					ParseFloat(sArray[1], 0),
					ParseFloat(sArray[2], 0));

				return result;
			}
			catch
            {
				return Vector3.Zero;
            }
		}


		// ABBREVIATE VALUE //	 Abbreviates float value to k/M/G notation (i.e. 1000 = 1k). Returns string.
		public string abbreviateValue(float valueIn)
		{
			string abbreviatedValue;
			if (valueIn <= -1000000000 || valueIn >= 1000000000)
			{
				valueIn = valueIn / 1000000000;
				abbreviatedValue = valueIn.ToString("0.0") + "G";
			}
			else if (valueIn <= -1000000 || valueIn >= 1000000)
			{
				valueIn = valueIn / 1000000;
				abbreviatedValue = valueIn.ToString("0.0") + "M";
			}
			else if (valueIn <= -1000 || valueIn >= 1000)
			{
				valueIn = valueIn / 1000;
				abbreviatedValue = valueIn.ToString("0.0") + "k";
			}
			else
			{
				abbreviatedValue = valueIn.ToString("F0");
			}
			return abbreviatedValue;
		}


		// PLANET SORT //  Sorts Planets List by Transformed Z-Coordinate from largest to smallest. For the purpose of sprite printing.
		public void PlanetSort(List<Planet> planets, StarMap map)
		{
			int length = planets.Count;

			for (int i = 0; i < planets.Count - 1; i++)
			{
				for (int p = 1; p < length; p++)
				{
					Planet planetA = planets[p - 1];
					Planet planetB = planets[p];

					if (planetA.transformedCoords[map.Number].Z < planetB.transformedCoords[map.Number].Z)
					{
						planets[p - 1] = planetB;
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


		// TRANSFORM VECTOR //	   Transforms vector location of planet or waypoint for StarMap view.
		public Vector3 transformVector(Vector3 vectorIn, StarMap map)
		{
			double xS = vectorIn.X - map.Center.X; //Vector X - Map X
			double yS = vectorIn.Y - map.Center.Y; //Vector Y - Map Y
			double zS = vectorIn.Z - map.Center.Z; //Vector Z - Map Z 

			double r = map.RotationalRadius;

			double cosAz = Math.Cos(ToRadians(map.Azimuth));
			double sinAz = Math.Sin(ToRadians(map.Azimuth));

			double cosAlt = Math.Cos(ToRadians(map.Altitude));
			double sinAlt = Math.Sin(ToRadians(map.Altitude));

			// Transformation Formulas from Matrix Calculations
			double xT = cosAz * xS + sinAz * zS;
			double yT = sinAz * sinAlt * xS + cosAlt * yS - sinAlt * cosAz * zS;
			double zT = -sinAz * cosAlt * xS + sinAlt * yS + cosAz * cosAlt * zS + r;

			Vector3 vectorOut = new Vector3(xT, yT, zT);
			return vectorOut;
		}


		// ROTATE VECTOR //
		public Vector3 rotateVector(Vector3 vecIn, StarMap map)
		{
			float x = vecIn.X;
			float y = vecIn.Y;
			float z = vecIn.Z;

			float cosAz = (float)Math.Cos(ToRadians(map.Azimuth));
			float sinAz = (float)Math.Sin(ToRadians(map.Azimuth));

			float cosAlt = (float)Math.Cos(ToRadians(map.Altitude));
			float sinAlt = (float)Math.Sin(ToRadians(map.Altitude));

			float xT = cosAz * x + sinAz * z;
			float yT = sinAz * sinAlt * x + cosAlt * y - sinAlt * cosAz * z;
			float zT = -sinAz * cosAlt * x + sinAlt * y + cosAz * cosAlt * z;

			Vector3 vecOut = new Vector3(xT, yT, zT);
			return vecOut;
		}


		// ROTATE MOVEMENT //	 Rotates Movement Vector for purpose of translation.
		public static Vector3 rotateMovement(Vector3 vecIn, StarMap map)
		{
			float x = vecIn.X;
			float y = vecIn.Y;
			float z = vecIn.Z;

			float cosAz = (float)Math.Cos(ToRadians(-map.Azimuth));
			float sinAz = (float)Math.Sin(ToRadians(-map.Azimuth));

			float cosAlt = (float)Math.Cos(ToRadians(-map.Altitude));
			float sinAlt = (float)Math.Sin(ToRadians(-map.Altitude));

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

			// Decide which stage of the cycle will be executed based on this grid's offset
			int stage = (_cycleStep - _cycleOffset + CYCLE_LENGTH) % CYCLE_LENGTH;

			//Echo("Cycle Stage: " + stage);

			switch (stage)
            {					
				case 4:
					UpdateDistances();
					break;
				case 3:
					SortGlobalWaypoints();
					break;
				case 2:
					SortPlanetsForMaps();
					break;
				case 1:
					if (_planetToLog)
					{
						DataToLog();
						_planetToLog = false;
					}
					break;
				case 0:
					UpdatePageData();
					break;
			}

			if(_cycleStep < 1)
            {
				_cycleStep = CYCLE_LENGTH; // Reset counter
				_lightOn = !_lightOn; //Toggle Indicator LightGray
			}
		}


		// UPDATE MAP // Transform logged locations based on map parameters
		public void UpdateMap(StarMap map)
		{
			if (_mapList.Count == 0)
				return;

			if (_planets)
			{
				foreach (Planet planet in _planetList)
				{
					Vector3 newCenter = transformVector(planet.position, map);

					if (planet.transformedCoords.Count < _mapList.Count)
					{
						planet.transformedCoords.Add(newCenter);
					}
					else
					{
						planet.transformedCoords[map.Number] = newCenter;
					}
				}
			}

			if (_waypointList.Count > 0)
			{
				foreach (Waypoint waypoint in _waypointList)
				{
					Vector3 newPos = transformVector(waypoint.position, map);
					if (waypoint.transformedCoords.Count < _mapList.Count)
					{
						waypoint.transformedCoords.Add(newPos);
					}
					else
					{
						waypoint.transformedCoords[map.Number] = newPos;
					}
				}
			}
		}


		// ACTIVATE MAP // activates tagged map screen without having to recompile.
		void activateMap(StarMap map)
		{
			IMyTextSurfaceProvider mapBlock = map.Block as IMyTextSurfaceProvider;
			map.DrawingSurface = mapBlock.GetSurface(map.Index);
			PrepareTextSurfaceForSprites(map.DrawingSurface);

			// Calculate the viewport offset by centering the surface size onto the texture size
			map.Viewport = new RectangleF(
			(map.DrawingSurface.TextureSize - map.DrawingSurface.SurfaceSize) / 2f,
				map.DrawingSurface.SurfaceSize
			);
			map.Number = _mapList.Count;
			_mapList.Add(map);
			UpdateMap(map);
		}


		// ADD MESSAGE //
		static void AddMessage(string message)
        {
			_messages.Add(message);

			if (_messages.Count >= MESSAGE_LIMIT)
				_messages.RemoveAt(0);
        }


		
		// ECHO MESSAGES //
		void EchoMessages()
        {
			if (_messages.Count < 1)
				return;

			Echo("-- MESSAGES --");

			for (int i = _messages.Count - 1; i > -1; i--)
				Echo("* " + _messages[i]);
        }
	}
}
