﻿using Sandbox.Game.EntityComponents;
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
    partial class Program
    {
		MySpriteDrawFrame _frame;


		// DRAW TEXTURE //
		public void DrawTexture(string shape, Vector2 position, Vector2 size, float rotation, Color color)
		{
			var sprite = new MySprite()
			{
				Type = SpriteType.TEXTURE,
				Data = shape,
				Position = position,
				Size = size,
				RotationOrScale = rotation,
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


		// DRAW MAPS //
		public void DrawMaps()
        {
			foreach (StarMap map in _mapList)
			{
				if (map.mode == "CHASE")
				{
					AlignShip(map);
				}
				else if (map.mode == "PLANET" && _planetList.Count > 0)
				{
					ShipToPlanet(map);
				}
				else if (map.mode == "ORBIT")
				{
					AlignOrbit(map);
				}
				else
				{
					map.azimuth = DegreeAdd(map.azimuth, map.dAz);

					if (map.mode != "SHIP")
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

				//_statusMessage = Me.DefinitionDisplayName;
			}
		}


		// DRAW SHIP //
		public void DrawShip(StarMap map, List<Planet> displayPlanets)
		{
			// SHIP COLORS
			Color bodyColor = new Color(SHIP_RED, SHIP_GREEN, SHIP_BLUE);
			Color aftColor = new Color(180, 60, 0);
			Color plumeColor = Color.Yellow;
			Color canopyColor = Color.DarkBlue;

			Vector3 transformedShip = transformVector(_myPos, map);
			Vector2 shipPos = PlotObject(transformedShip, map);
			float shipX = shipPos.X;
			float shipY = shipPos.Y;

			int vertMod = 0;
			if (map.showInfo)
			{
				vertMod = BAR_HEIGHT;

				if (map.viewport.Width > 500)
				{
					vertMod *= 2;
				}
			}

			bool offZ = transformedShip.Z < map.focalLength;
			bool leftX = shipX < -map.viewport.Width / 2 || (offZ && shipX < 0);
			bool rightX = shipX > map.viewport.Width / 2 || (offZ && shipX >= 0);
			bool aboveY = shipY < -map.viewport.Height / 2 + vertMod || (offZ && shipY < 0);
			bool belowY = shipY > map.viewport.Height / 2 - vertMod || (offZ && shipX >= 0);
			bool offX = leftX || rightX;
			bool offY = aboveY || belowY;

			if (offZ || offX || offY)
			{
				float posX;
				float posY;
				float rotation = 0;
				int pointerScale = SHIP_SCALE / 2;


				if (offZ)
				{
					bodyColor = Color.DarkRed;
					//shipX *= -1;
					//shipY *= -1;
				}
				else
				{
					bodyColor = Color.DodgerBlue;
				}

				if (leftX)
				{
					posX = 0;
					rotation = (float)Math.PI * 3 / 2;
				}
				else if (rightX)
				{
					posX = map.viewport.Width - pointerScale;
					rotation = (float)Math.PI / 2;
				}
				else
				{
					posX = map.viewport.Width / 2 + shipX - pointerScale / 2;
				}

				if (aboveY)
				{
					posY = vertMod + TOP_MARGIN + map.viewport.Center.Y - map.viewport.Height / 2;
					rotation = 0;
				}
				else if (belowY)
				{
					posY = map.viewport.Center.Y + map.viewport.Height / 2 - vertMod - TOP_MARGIN;
					rotation = (float)Math.PI;
				}
				else
				{
					posY = map.viewport.Height / 2 + shipY + (map.viewport.Width - map.viewport.Height) / 2;
				}

				if (offX && offY)
				{
					rotation = (float)Math.Atan2(shipY, shipX);
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

				if (displayPlanets.Count > 0)
				{
					String planetColor = obscureShip(position, displayPlanets, map);

					if (planetColor != "NONE")
					{
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
				float shipLength = (float)1.33 * SHIP_SCALE * (float)Math.Sqrt(headingX * headingX + headingY * headingY) / (float)Math.Sqrt(headingX * headingX + headingY * headingY + headingZ * headingZ);

				float shipAngle = (float)Math.Atan2(headingX, headingY) * -1;

				position += map.viewport.Center;

				Vector2 offset = new Vector2((float)Math.Sin(shipAngle), (float)Math.Cos(shipAngle) * -1);
				position += offset * shipLength / 4;
				position -= new Vector2(SHIP_SCALE / 2, 0);
				Vector2 startPosition = position;

				position -= new Vector2(2, 0);

				//Outline
				DrawTexture("Triangle", position, new Vector2(SHIP_SCALE + 4, shipLength + 4), shipAngle, Color.Black);

				float aftHeight = SHIP_SCALE - shipLength / (float)1.33;

				position = startPosition;
				position -= offset * shipLength / 2;
				position -= new Vector2(2, 0);

				DrawTexture("Circle", position, new Vector2(SHIP_SCALE + 4, aftHeight + 4), shipAngle, Color.Black);

				position = startPosition;

				// Ship Body
				DrawTexture("Triangle", position, new Vector2(SHIP_SCALE, shipLength), shipAngle, bodyColor);

				if (headingZ < 0)
				{
					position = startPosition;
					position -= offset * shipLength / 2;

					DrawTexture("Circle", position, new Vector2(SHIP_SCALE, aftHeight), shipAngle, bodyColor);
				}

				// Canopy
				position = startPosition;
				position += offset * shipLength / 8;
				position += new Vector2(SHIP_SCALE / 4, 0);
				DrawTexture("Triangle", position, new Vector2(SHIP_SCALE * 0.5f, shipLength * 0.5f), shipAngle, canopyColor);



				// Canopy Mask Variables
				Vector3 shipUp = rotateVector(_refBlock.WorldMatrix.Up, map);
				Vector3 shipRight = rotateVector(_refBlock.WorldMatrix.Right, map);

				float rollInput = (float)Math.Atan2(shipRight.Z, shipUp.Z) + (float)Math.PI;

				//Echo("Roll Angle: " + ToDegrees(rollInput) + "°");

				float rollAngle = (float)Math.Cos(rollInput / 2) * (float)Math.Atan2(SHIP_SCALE, 2 * shipLength) * 0.9f;//(float) Math.Atan2(shipLength, 2 * SHIP_SCALE) * (1 - (float) Math.Cos(rollInput))/2;//

				float rollScale = (float)Math.Sin(rollInput / 2) * 0.7f * shipLength / SHIP_SCALE;// + (float) Math.Abs(headingZ)/10;
				Vector2 rollOffset = new Vector2((float)Math.Sin(shipAngle - rollAngle), (float)Math.Cos(shipAngle - rollAngle) * -1);

				float maskMod = 0.95f;
				if (rollInput < 0.35f || rollInput > 5.93f)
					maskMod = 0.6f;

				// Canopy Edge
				var edgeColor = bodyColor;
				int edgeMod = 1;

				if (headingZ < 0)
				{
					edgeColor = canopyColor;
					edgeMod = -1;
				}

				position = startPosition;
				position -= offset * shipLength / 8;
				position += new Vector2(SHIP_SCALE / 4, 0);
				DrawTexture("SemiCircle", position, new Vector2(SHIP_SCALE * 0.5f, aftHeight * 0.5f * edgeMod), shipAngle, edgeColor);

				// Canopy Mask
				position = startPosition;
				position += new Vector2((1 - rollScale) * SHIP_SCALE / 2, 0);
				position += offset * shipLength * 0.25f;
				position -= rollOffset * 0.7f * shipLength / 3;

				DrawTexture("Triangle", position, new Vector2(SHIP_SCALE * rollScale, shipLength * maskMod), shipAngle - rollAngle, bodyColor);

				//Aft Plume
				if (headingZ >= 0)
				{
					position = startPosition;
					position -= offset * shipLength / 2;

					DrawTexture("Circle", position, new Vector2(SHIP_SCALE, aftHeight), shipAngle, aftColor);

					position -= offset * shipLength / 16;
					position += new Vector2(SHIP_SCALE / 6, 0);

					DrawTexture("Circle", position, new Vector2(SHIP_SCALE * 0.67f, aftHeight * 0.67f), shipAngle, plumeColor);

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
			float zFactor = map.focalLength / pos.Z;

			float plotX = pos.X * zFactor;
			float plotY = pos.Y * zFactor;

			Vector2 mapPos = new Vector2(-plotX, -plotY);
			return mapPos;
		}


		// DRAW PLANETS //
		public void DrawPlanets(List<Planet> displayPlanets, StarMap map)
		{
			PlanetSort(displayPlanets, map);

			string drawnPlanets = "Displayed Planets:";
			foreach (Planet planet in displayPlanets)
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
				if (map.mode == "ORBIT" && planet == map.activePlanet)
				{
					float radMod = 0.83f;
					size = new Vector2(diameter * radMod * 2, diameter * radMod * 2);
					position = startPosition - new Vector2(diameter * radMod, 0);

					DrawTexture("CircleHollow", position, size, 0, Color.Yellow);
					radMod *= 0.99f;
					position = startPosition - new Vector2(diameter * radMod, 0);
					size = new Vector2(diameter * radMod * 2, diameter * radMod * 2);

					DrawTexture("CircleHollow", position, size, 0, Color.Black);
				}

				// Planet Body
				position = startPosition - new Vector2(diameter / 2, 0);
				DrawTexture("Circle", position, new Vector2(diameter, diameter), 0, surfaceColor);

				// Equator
				double radAngle = (float)map.altitude * Math.PI / 180;
				float pitchMod = (float)Math.Sin(radAngle) * diameter;
				DrawTexture("CircleHollow", position, new Vector2(diameter, pitchMod), 0, lineColor);

				// Mask
				int scaleMod = -1;
				if (map.altitude < 0)
				{
					scaleMod *= -1;
				}
				DrawTexture("SemiCircle", position, new Vector2(diameter, diameter * scaleMod), 0, surfaceColor);

				// Border
				DrawTexture("CircleHollow", position, new Vector2(diameter, diameter), 0, lineColor);


				// HashMarks
				if (diameter > HASH_LIMIT && map.mode != "CHASE")// && Vector3.Distance(planet.position, map.center) < 2*planet.radius
				{
					DrawHashMarks(planet, diameter, lineColor, map);
				}

				if (map.showNames)
				{
					// PLANET NAME
					float fontMod = 1;

					if (diameter < 50)
					{
						fontMod = (float)0.5;
					}

					// Name Shadow
					position = startPosition;
					DrawText(planet.name, position, fontMod * 0.8f, TextAlignment.CENTER, Color.Black);

					// Name
					position += new Vector2(-2, 2);
					DrawText(planet.name, position, fontMod * 0.8f, TextAlignment.CENTER, Color.Yellow * _brightnessMod);
				}
			}

			Echo(drawnPlanets.Trim(',') + "\n");
		}


		// DRAW HASHMARKS //   Makes series of low-profile waypoints to denote latitude and longitude on planets.
		public void DrawHashMarks(Planet planet, float diameter, Color lineColor, StarMap map)
		{
			List<Waypoint> hashMarks = new List<Waypoint>();

			float planetDepth = planet.transformedCoords[map.number].Z;

			//North Pole
			Waypoint north = new Waypoint();
			north.name = "N -";
			north.position = planet.position + new Vector3(0, (float)planet.radius, 0);
			north.transformedCoords.Add(transformVector(north.position, map));
			if (north.transformedCoords[0].Z < planetDepth)
			{
				hashMarks.Add(north);
			}

			//South Pole
			Waypoint south = new Waypoint();
			south.name = "S -";
			south.position = planet.position - new Vector3(0, (float)planet.radius, 0);
			south.transformedCoords.Add(transformVector(south.position, map));
			if (south.transformedCoords[0].Z < planetDepth)
			{
				hashMarks.Add(south);
			}

			float r1 = planet.radius * 0.95f;
			float r2 = (float)Math.Sqrt(2) / 2 * r1;

			float r3 = r1 / 2;

			String[] latitudes = new String[] { "+", "|", "+" };
			String[] longitudes = new String[] { "135°E", "90°E", "45°E", "0°", "45°W", "90°W", "135°W", "180°" };

			float[] yCoords = new float[] { -r2, 0, r2 };
			float[,] xCoords = new float[,] { { -r3, -r2, -r3, 0, r3, r2, r3, 0 }, { -r2, -r1, -r2, 0, r2, r1, r2, 0 }, { -r3, -r2, -r3, 0, r3, r2, r3, 0 } };
			float[,] zCoords = new float[,] { { r3, 0, -r3, -r2, -r3, 0, r3, r2 }, { r2, 0, -r2, -r1, -r2, 0, r2, r1 }, { r3, 0, -r3, -r2, -r3, 0, r3, r2 } };

			for (int m = 0; m < 3; m++)
			{
				String latitude = latitudes[m];
				float yCoord = yCoords[m];
				for (int n = 0; n < 8; n++)
				{
					Waypoint hashMark = new Waypoint();
					hashMark.name = latitude + " " + longitudes[n];

					float xCoord = xCoords[m, n];
					float zCoord = zCoords[m, n];

					hashMark.position = planet.position + new Vector3(xCoord, yCoord, zCoord);
					hashMark.transformedCoords.Add(transformVector(hashMark.position, map));

					if (hashMark.transformedCoords[0].Z < planetDepth)
					{
						hashMarks.Add(hashMark);
					}
				}
			}

			foreach (Waypoint hash in hashMarks)
			{
				Vector2 position = map.viewport.Center + PlotObject(hash.transformedCoords[0], map);

				// Print more detail for closer planets
				if (diameter > 2 * HASH_LIMIT)
				{

					String[] hashLabels = hash.name.Split(' ');
					float textMod = 1;
					int pitchMod = 1;
					if (map.altitude > 0)
					{
						pitchMod = -1;
					}

					if (diameter > 3 * HASH_LIMIT)
					{
						textMod = 1.5f;
					}

					Vector2 hashOffset = new Vector2(0, 10 * textMod * pitchMod);
					position -= hashOffset;

					DrawText(hashLabels[0], position, 0.5f * textMod, TextAlignment.CENTER, lineColor);

					position += hashOffset;

					DrawText(hashLabels[1], position, 0.4f * textMod, TextAlignment.CENTER, lineColor);
				}
				else
				{
					position += new Vector2(-2, 2);

					DrawTexture("Circle", position, new Vector2(4, 4), 0, lineColor);
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

			if (map.viewport.Width > 500)
			{
				fontSize *= 1.5f;
				markerSize *= 2;
			}
			foreach (Waypoint waypoint in _waypointList)
			{

				if (waypoint.isActive)
				{

					float rotationMod = 0;
					Color markerColor = ColorSwitch(waypoint.color, true);

					float gpsScale = 1;
					float coordZ;


					try
					{
						coordZ = waypoint.transformedCoords[map.number].Z;
					}
					catch
					{
						return;
					}

					bool activePoint = (map.activeWaypointName != "") && (waypoint.name == map.activeWaypointName);


					if (map.gpsState == 1 && !activePoint)
						gpsScale = FOCAL_MOD * map.focalLength / coordZ;//coordZ / (-2 * focalRadius) + 1.5f;



					float iconSize = markerSize * gpsScale;

					Vector2 markerScale = new Vector2(iconSize, iconSize);

					Vector2 waypointPosition = PlotObject(waypoint.transformedCoords[map.number], map);
					Vector2 startPosition = map.viewport.Center + waypointPosition;

					String markerShape = "";
					switch (waypoint.marker.ToUpper())
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
							markerColor = new Color(48, 48, 48);
							break;
						case "HAZARD":
							markerShape = "SquareTapered";
							markerColor = Color.Red;
							rotationMod = (float)Math.PI / 4;
							break;
						case "ASTEROID":
							markerShape = "SquareTapered";
							markerColor = new Color(48, 32, 32);
							markerScale *= 0.9f;
							rotationMod = (float)Math.PI / 4;
							break;
						default:
							markerShape = "SquareHollow";
							break;
					}



					if (coordZ > map.focalLength)
					{

						Vector2 position = startPosition - new Vector2(iconSize / 2, 0);

						markerColor *= _brightnessMod;

						// PRINT MARKER

						// Shadow
						DrawTexture(markerShape, position, markerScale, rotationMod, Color.Black);

						// Marker
						position += new Vector2(1, 0);
						DrawTexture(markerShape, position, markerScale * 1.2f, rotationMod, markerColor);

						position += new Vector2(1, 0);
						DrawTexture(markerShape, position, markerScale, rotationMod, markerColor);

						// Draw secondary features for special markers
						switch (waypoint.marker.ToUpper())
						{
							case "STATION":
								position += new Vector2(iconSize / 2 - iconSize / 20, 0);
								DrawTexture("SquareSimple", position, new Vector2(iconSize / 10, iconSize), rotationMod, markerColor);
								break;
							case "HAZARD":
								position += new Vector2(iconSize / 2 - iconSize / 20, -iconSize * 0.85f);
								DrawText("!", position, fontSize * 1.2f * gpsScale, TextAlignment.CENTER, Color.White);
								break;
							case "BASE":
								position += new Vector2(iconSize / 6, -iconSize / 12);
								DrawTexture("SemiCircle", position, new Vector2(iconSize * 1.15f, iconSize * 1.15f), rotationMod, new Color(0, 64, 64) * _brightnessMod);
								startPosition -= new Vector2(0, iconSize * 0.4f);
								break;
							case "ASTEROID":
								position += new Vector2(iconSize / 2 - iconSize / 20, 0);
								DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32, 32, 32) * _brightnessMod);
								position -= new Vector2(iconSize - iconSize / 10, 0);
								DrawTexture("SquareTapered", position, markerScale, rotationMod, new Color(32, 32, 32) * _brightnessMod);
								break;
							default:
								break;
						}

						if (activePoint)
						{
							position = startPosition - new Vector2(0.9f * markerSize, 0.33f * markerSize);
							DrawText("|________", position, fontSize, TextAlignment.LEFT, Color.White);
						}

						// PRINT NAME
						if (map.showNames)
						{
							position = startPosition + new Vector2(1.33f * iconSize, -0.75f * iconSize);
							DrawText(waypoint.name, position, fontSize * gpsScale, TextAlignment.LEFT, markerColor * _brightnessMod);
						}
					}
				}
			}
		}


		// DRAW SURFACE POINTS //
		public void DrawSurfacePoint(Vector3 surfacePoint, int pointNumber, StarMap map)
		{
			Color pointColor;
			switch (pointNumber)
			{
				case 1:
					pointColor = new Color(32, 0, 48);
					break;
				case 2:
					pointColor = new Color(64, 0, 96);
					break;
				case 3:
					pointColor = new Color(128, 0, 192);
					break;
				default:
					pointColor = Color.Red;
					break;
			}

			Vector3 pointTransformed = transformVector(surfacePoint, map);
			if (pointTransformed.Z > map.focalLength)
			{
				float markerScale = MARKER_WIDTH * 2;
				float textSize = 0.6f;

				if (map.viewport.Width > 500)
				{
					markerScale *= 1.5f;
					textSize *= 1.5f;
				}

				Vector2 startPosition = map.viewport.Center + PlotObject(pointTransformed, map);
				Vector2 position = startPosition + new Vector2(-markerScale / 2, 0);

				Vector2 markerSize = new Vector2(markerScale, markerScale);

				position += new Vector2(-markerScale * 0.025f, markerScale * 0.05f);

				DrawTexture("Circle", position, markerSize * 1.1f, 0, Color.Black);

				DrawTexture("CircleHollow", position, markerSize, 0, pointColor);

				position = startPosition - new Vector2(0, markerScale / 2);

				DrawText("?", position, textSize, TextAlignment.CENTER, pointColor);
			}
		}


		// PLOT UNCHARTED //
		public void PlotUncharted(StarMap map)
		{
			if (_unchartedList.Count > 0)
			{
				foreach (Planet planet in _unchartedList)
				{
					String[] planetData = planet.ToString().Split(';');

					for (int p = 4; p < 8; p++)
						if (planetData[p] != "")
						{
							int pointNumber = p - 3;
							DrawSurfacePoint(planet.GetPoint(pointNumber), pointNumber, map);
						}
				}
			}
		}


		// PROJECT DIAMETER //
		float ProjectDiameter(Planet planet, StarMap map)
		{
			float viewAngle = (float)Math.Asin(planet.radius / planet.transformedCoords[map.number].Z);

			float diameter = (float)Math.Tan(Math.Abs(viewAngle)) * 2 * map.focalLength;

			if (diameter < DIAMETER_MIN)
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
			foreach (Planet planet in planets)
			{
				if (Vector2.Distance(shipPos, planet.mapPos) < Vector2.Distance(shipPos, closest.mapPos))
				{
					closest = planet;
				}
			}

			String color = "NONE";
			float distance = Vector2.Distance(shipPos, closest.mapPos);
			float radius = 0.95f * closest.radius * map.focalLength / closest.transformedCoords[map.number].Z;

			if (distance < radius && closest.transformedCoords[map.number].Z < transformVector(_myPos, map).Z)
			{
				color = closest.color;
			}

			return color;
		}


		// PREPARE TEXT SURFACE FOR SPRITES //
		public static void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
		{
			// Set the sprite display mode
			textSurface.ContentType = ContentType.SCRIPT;
			textSurface.ScriptBackgroundColor = new Color(0, 0, 0);
			// Make sure no built-in script has been selected
			textSurface.Script = "";
		}


		// DRAW SPRITES //
		public void DrawSprites(StarMap map)
		{
			Echo("[MAP " + map.number + "]");
			Vector3 mapCenter = map.center;

			// Create background sprite
			Color gridColor = new Color(0, 64, 0);
			DrawTexture("Grid", new Vector2(0, map.viewport.Width / 2), map.viewport.Size, 0, gridColor);

			//DRAW PLANETS
			List<Planet> displayPlanets = new List<Planet>();

			foreach (Planet planet in _planetList)
			{
				if (planet.transformedCoords.Count == _mapList.Count && planet.transformedCoords[map.number].Z > map.focalLength)
				{
					displayPlanets.Add(planet);
				}
			}

			DrawPlanets(displayPlanets, map);
			//DRAW WAYPOINTS & UNCHARTED SURFACE POINTS
			if (map.gpsState > 0)
			{
				DrawWaypoints(map);
				PlotUncharted(map);
			}

			// DRAW SHIP
			if (map.showShip)
			{
				DrawShip(map, displayPlanets);
			}

			// MAP INFO
			if (map.showInfo)
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
			String angleReading = map.altitude * -1 + "° " + map.azimuth + "°";
			String shipMode = "S";
			String planetMode = "P";
			String freeMode = "F";
			String worldMode = "W";
			String chaseMode = "C";
			String orbitMode = "O";

			if (map.viewport.Width > 500)
			{
				fontSize *= 1.5f;
				barHeight *= 2;
				angleReading = "Alt:" + map.altitude * -1 + "°  Az:" + map.azimuth + "°";
				shipMode = "SHIP";
				planetMode = "PLANET";
				freeMode = "FREE";
				worldMode = "WORLD";
				chaseMode = "CHASE";
				orbitMode = "ORBIT";
			}

			//TOP BAR
			var position = map.viewport.Center;
			position -= new Vector2(map.viewport.Width / 2, map.viewport.Height / 2 - barHeight / 2);
			DrawTexture("SquareSimple", position, new Vector2(map.viewport.Width, barHeight), 0, Color.Black);

			//MODE	  
			position += new Vector2(SIDE_MARGIN, -TOP_MARGIN);

			string modeReading = "";
			switch (map.mode)
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

			position += new Vector2(map.viewport.Width / 2 - SIDE_MARGIN, 0);

			DrawText(centerReading, position, fontSize, TextAlignment.CENTER, Color.White);

			// RUNNING INDICATOR
			position += new Vector2(map.viewport.Width / 2 - SIDE_MARGIN, TOP_MARGIN);

			Color lightColor = new Color(0, 8, 0);

			if (_lightOn)
			{
				DrawTexture("Circle", position, new Vector2(7, 7), 0, lightColor);
			}

			// MAP ID
			position -= new Vector2(5, 7);

			string mapID = "[" + map.number + "]";

			DrawText(mapID, position, fontSize, TextAlignment.RIGHT, Color.White);

			// BOTTOM BAR
			position = map.viewport.Center;
			position -= new Vector2(map.viewport.Width / 2, barHeight / 2 - map.viewport.Height / 2);

			if (map.viewport.Width == 1024)
			{
				position = new Vector2(0, map.viewport.Height - barHeight / 2);
			}
			DrawTexture("SquareSimple", position, new Vector2(map.viewport.Width, barHeight), 0, Color.Black);

			// FOCAL LENGTH READING
			position += new Vector2(SIDE_MARGIN, -TOP_MARGIN);

			string dofReading = "FL:" + abbreviateValue((float)map.focalLength);

			DrawText(dofReading, position, fontSize, TextAlignment.LEFT, Color.White);

			// ANGLE READING
			position += new Vector2(map.viewport.Width / 2 - SIDE_MARGIN, 0);

			DrawText(angleReading, position, fontSize, TextAlignment.CENTER, Color.White);

			// RADIUS READING
			string radius = "R:" + abbreviateValue((float)map.rotationalRadius);
			position += new Vector2(map.viewport.Width / 2 - SIDE_MARGIN, 0);

			DrawText(radius, position, fontSize, TextAlignment.RIGHT, Color.White);
		}


		// DRAW MENU //
		public void DrawMenu(MapMenu menu)
        {
			_frame = menu.Surface.DrawFrame();

			// Set height and width variables
			Vector2 center = menu.Viewport.Center;
			float height = menu.Viewport.Height;
			float width = menu.Viewport.Width;
			float fontSize = 0.7f;
			Color color1 = menu.Color1;
			Color color2 = menu.Color2;

			int page = menu.CurrentPage;
			float cellWidth = (width / 7);
			float buttonHeight = (height / 2);
			if (buttonHeight > cellWidth)
				buttonHeight = cellWidth - 4;

			// Background
			Vector2 position = center - new Vector2(width / 2, 0);
			DrawTexture("SquareSimple", position, new Vector2(width, height), 0, color1);

			// Button Backgrounds
			position = center + new Vector2(cellWidth * -3.5f + (cellWidth - buttonHeight)/2, buttonHeight / 2);
			Vector2 buttonScale = new Vector2(buttonHeight, buttonHeight);

			for (int i = 1; i < 8; i ++)
            {
				Color color;

				// Brighten button if active, otherwise darken
				if (i == menu.ActiveButton)
					color = color2 * 2;
				else
					color = color2 * 0.5f;

				DrawTexture("SquareSimple", position, buttonScale, 0, color);
				position += new Vector2(cellWidth, 0);
			}

			// Menu Title
			position = center - new Vector2(width/2 - 10, buttonHeight);
			DrawText("MENU " + page + ": " + _menuTitle[page], position, fontSize, TextAlignment.LEFT, color2);

			// Menu ID
			position += new Vector2(width/2 - 10, 0);
			if(_mapMenus.Count > 1)
				DrawText("< " + menu.IDNumber + " >", position, fontSize, TextAlignment.CENTER, color2);

			// Current Map
			position += new Vector2(width / 2 - 10, 0);
			if(_mapList.Count > 1)
				DrawText("MAP: " + menu.CurrentMapIndex, position, fontSize, TextAlignment.RIGHT, color2);

			// Label A
			position = center - new Vector2(cellWidth * 2.5f, buttonHeight * 0.4f);
			DrawText(_labelA[page], position, fontSize * 0.9f, TextAlignment.CENTER, color2);

			// Label B
			position += new Vector2(cellWidth * 2, 0);
			DrawText(_labelB[page], position, fontSize * 0.9f, TextAlignment.CENTER, color2);

			// Label C
			position += new Vector2(cellWidth * 2, 0);
			DrawText(_labelC[page], position, fontSize * 0.9f, TextAlignment.CENTER, color2);

			// Label D
			position += new Vector2(cellWidth * 1.5f, 0);
			DrawText(_labelD[page], position, fontSize * 0.75f, TextAlignment.CENTER, color2);


			Vector2 iconScale = buttonScale * 0.33f;

			// Cmd 1
			position = center + new Vector2(-3 * cellWidth, buttonHeight * 0.33f);
			//DrawText(_cmd1[page], position, fontSize, TextAlignment.CENTER, color1);
			StringToIcon(_cmd1[page], position, iconScale, color1);

			// Cmd 2
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd2[page], position, iconScale, color1);

			// Cmd 3
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd3[page], position, iconScale, color1);

			// Cmd 4
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd4[page], position, iconScale, color1);

			// Cmd 5
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd5[page], position, iconScale, color1);

			// Cmd 6
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd6[page], position, iconScale, color1);

			// Cmd 7
			position += new Vector2(cellWidth, 0);
			StringToIcon(_cmd7[page], position, iconScale, color1);

			fontSize *= 1.5f;

			// Key 1
			position = center + new Vector2(-3 * cellWidth, buttonHeight * 0.45f);
			DrawText("1", position, fontSize, TextAlignment.CENTER, color1);

			// Key 2
			position += new Vector2(cellWidth, 0);
			DrawText("2", position, fontSize, TextAlignment.CENTER, color1);

			// Key 3
			position += new Vector2(cellWidth, 0);
			DrawText("3", position, fontSize, TextAlignment.CENTER, color1);

			// Key 4
			position += new Vector2(cellWidth, 0);
			DrawText("4", position, fontSize, TextAlignment.CENTER, color1);

			// Key 5
			position += new Vector2(cellWidth, 0);
			DrawText("5", position, fontSize, TextAlignment.CENTER, color1);

			// Key 6
			position += new Vector2(cellWidth, 0);
			DrawText("6", position, fontSize, TextAlignment.CENTER, color1);

			// Key 7
			position += new Vector2(cellWidth, 0);
			DrawText("7", position, fontSize, TextAlignment.CENTER, color1);


			// Test Circle - Center
			//DrawTexture("CircleHollow", center - new Vector2(buttonHeight/2, 0), buttonScale, 0, Color.Red);

			_frame.Dispose();
		}


		void StringToIcon(string arg, Vector2 position, Vector2 scale, Color color)
        {
			switch(arg)
            {
				case "<":
					DrawTriangle(position, scale, color, "left");
					break;
				case ">":
					DrawTriangle(position, scale, color, "right");
					break;
				case "<<":
					DrawDoubleTriangle(position, scale, color, "left");
					break;
				case ">>":
					DrawDoubleTriangle(position, scale, color, "right");
					break;
				case "^":
					DrawTriangle(position, scale, color, "up");
					break;
				case "v":
					DrawTriangle(position, scale, color, "down");
					break;
				case "^^":
					DrawDoubleTriangle(position, scale, color, "up");
					break;
				case "vv":
					DrawDoubleTriangle(position, scale, color, "down");
					break;
				case "-/o":
					DrawToggle(position, scale, color);
					break;
				case "cycle":
					DrawCycle(position, scale, color);
					break;
				default:
					DrawCharacters(arg, position, scale, color);
					break;
            }
        }


		// Draw Horz Triangle
		void DrawTriangle(Vector2 position, Vector2 scale, Color color, string direction)
        {
			float rotation;
			Vector2 offset;
			

			switch(direction)
            {
				case "right":
					rotation = 0.5f;
					offset = new Vector2(scale.Y * 0.33f, 0);
					break;
				case "down":
					rotation = 1;
					offset = new Vector2(scale.X / 2, scale.Y * -0.1f);
					break;
				case "left":
					rotation = 1.5f;
					offset = new Vector2(scale.Y * 0.67f, 0);
					break;
				case "up":
				default:
					rotation = 0;
					offset = new Vector2(scale.X / 2, scale.Y * 0.1f);
					break;
			}				

			DrawTexture("Triangle", position - offset, scale, (float) Math.PI * rotation, color);
        }


		// Draw Vert Triangle
		void DrawDoubleTriangle(Vector2 position, Vector2 scale, Color color, string direction)
        {
			float rotation;
			float length = scale.Y * 0.33f;
			Vector2 offset1;
			Vector2 offset2;

			switch (direction)
			{
				case "right":
					rotation = 0.5f;
					offset1 = new Vector2(scale.Y * 0.33f, 0);
					offset2 = new Vector2(length, 0);
					break;
				case "down":
					rotation = 1;
					offset1 = new Vector2(scale.X / 4, scale.Y * -0.25f);
					offset2 = new Vector2(0, -length);
					break;
				case "left":
					rotation = 1.5f;
					offset1 = new Vector2(scale.Y * 0.167f, 0);
					offset2 = new Vector2(-length, 0);
					break;
				case "up":
				default:
					rotation = 0;
					offset1 = new Vector2(scale.X / 4, scale.Y * 0.125f);
					offset2 = new Vector2(0, length);
					break;
			}

			rotation *= (float)Math.PI;
			position -= offset1;

			DrawTexture("Triangle", position, scale * 0.5f, rotation, color);

			position += offset2;

			DrawTexture("Triangle", position, scale * 0.5f, rotation, color);
		}


		// DRAW TOGGLE //
		void DrawToggle(Vector2 position, Vector2 scale, Color color)
        {
			position -= new Vector2(scale.Y / 2, 0);
			DrawTexture("SemiCircle", position, scale, (float) Math.PI * 1.5f, color);
			DrawTexture("CircleHollow", position, scale, 0, color);
			DrawTexture("CircleHollow", position + new Vector2(scale.X * 0.05f, 0), scale * 0.9f, 0, color);
			DrawTexture("CircleHollow", position + new Vector2(scale.X * 0.075f, 0), scale * 0.85f, 0, color);
		}


		// DRAW CYCLE //
		void DrawCycle(Vector2 position, Vector2 scale, Color color)
        {
			position -= new Vector2(scale.Y / 2, 0);
			DrawTexture("CircleHollow", position, scale, 0, color);
			DrawTexture("CircleHollow", position + new Vector2(scale.X * 0.05f, 0), scale * 0.9f, 0, color);
			DrawTexture("CircleHollow", position + new Vector2(scale.X * 0.075f, 0), scale * 0.85f, 0, color);
			DrawTexture("Triangle", position + new Vector2(scale.X * 0.67f, -scale.Y * 0.25f), scale * 0.5f, (float)Math.PI * 0.75f, color);
		}

		
		// DRAW CHARACTERS //
		void DrawCharacters(string characters, Vector2 position, Vector2 scale, Color color)
        {
			Vector2 offset;
			float fontSize;

			if (characters.Length > 3)
			{
				offset = new Vector2(0, scale.Y * 0.67f);
				fontSize = 0.75f;
			}
			else
            {
				offset = new Vector2(0, scale.Y * 1.25f);
				fontSize = 1.25f;
			}
				


			DrawText(characters, position - offset, fontSize, TextAlignment.CENTER, color);
		}

		// DRAW MENUS //
		public void DrawMenus()
        {
			if (_mapMenus.Count < 1)
				return;

			foreach(MapMenu menu in _mapMenus)
            {
				

				DrawMenu(menu);


            }
        }
	}
}
