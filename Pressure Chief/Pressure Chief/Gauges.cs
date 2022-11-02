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
    partial class Program
    {
        public class GaugeBlock
        {
            public IMyTerminalBlock Block;
            public List<GaugeSurface> Surfaces;
            public List<Sector> Sectors;

			// CONSTRUCTOR
            public GaugeBlock(IMyTerminalBlock block)
            {
                Block = block;
                Surfaces = new List<GaugeSurface>();
                Sectors = new List<Sector>();

                _buildMessage += "\nAdded LCD Block " + Block.CustomName;

                // Get Sectors from Ini String
                string[] sectorStrings = IniKey.GetKey(Block, GAUGE_HEAD, "Sectors", "").Split('\n');
                foreach(string sectorString in sectorStrings)
                {
                    Sector sector = GetSector(sectorString);
                    if (sector != null)
                    {
                        Sectors.Add(sector);
                    }  
                }

                // Set INI Key bool parameters for whether each screen should display a gauge.
                for(int i = 0; i < (Block as IMyTextSurfaceProvider).SurfaceCount; i++)
                {
                    string boolDefault;
                    if (i == 0) // First screen set to true by default
                        boolDefault = "True";
                    else // All else set to false by default.
                        boolDefault = "False";

                    // Check Custom Data of Block to see if screen should be active. Set parameter if not there.
                    if(Util.ParseBool(IniKey.GetKey(Block, GAUGE_HEAD, "Show_On_Screen_" + i, boolDefault)))
                    {
                        string gaugeHead = GAUGE_HEAD + " " + i;

                        string sectorA, sectorB;

                        if (Sectors.Count > 0)
                        {
                            sectorA = IniKey.GetKey(Block, gaugeHead, "Sector_A", Sectors[0].Name);

                            if (Sectors.Count > 1)
                            {
                                sectorB = IniKey.GetKey(Block, gaugeHead, "Sector_B", Sectors[1].Name);
                                Bulkhead bulkhead = GetBulkhead(sectorA + "," + sectorB);
                                _buildMessage += "\nAssigning surface " + i + " of " + Block.CustomName + " to bulkhead [" + sectorA + "," + sectorB + "]";

                                if (bulkhead != null)
                                    bulkhead.Gauges.Add(new GaugeSurface(this, i, bulkhead.Sectors, true));
                                else
                                    AssignSingleSectorGauge(sectorA, i);
                            }
                            else
                            {
                                AssignSingleSectorGauge(sectorA, i);
                            }
                        }
                    }
                }    
            }


            // ASSIGN SINGLE SECTOR GAUGE // Assign a gauge to a specific sector, just to display that sector's pressure.
            public void AssignSingleSectorGauge(string sectorName, int index)
            {
				

                Sector sector = GetSector(sectorName);
                if (sector != null)
				{
                    List<Sector> singleSector = new List<Sector>();
                    singleSector.Add(sector);
                    sector.Gauges.Add(new GaugeSurface(this, index, singleSector, false));
					_buildMessage += "\nAssigning LCD " + Block.CustomName + " to sector " + sector.Name;
				}		
			}
        }

        public class GaugeSurface
        {
            public IMyTextSurface Surface;
            public int Index;
            public string Side;
            public bool Vertical;
            public bool Flipped;
            public bool IsDouble;
            public float Brightness;
			public Sector SectorA;
			public Sector SectorB;

			// CONSTRUCTOR
            public GaugeSurface(GaugeBlock owner, int screenIndex, List<Sector> sectors, bool doubleScreen)
            {
                Index = screenIndex;
                Surface = (owner.Block as IMyTextSurfaceProvider).GetSurface(Index);
                string gaugeHead = GAUGE_HEAD + " " + Index;
                IsDouble = doubleScreen;
				SectorA = sectors[0];

				if(doubleScreen)
					SectorB = sectors[1];

                // Determine which side of the bulkhead the LCD is on.
                string blockName = owner.Block.CustomName;
                string defaultSide;

				if (IsDouble || blockName.Contains("(" + sectors[0].Name + ")"))
					defaultSide = "A";
				else if (sectors.Count > 1 && blockName.Contains("(" + sectors[1].Name + ")"))
					defaultSide = "B";
				else
					defaultSide = "Select A or B";

				Side = IniKey.GetKey(owner.Block, gaugeHead, "Side", defaultSide);
                
                bool vert;
                if (owner.Block.BlockDefinition.SubtypeId.ToString().Contains("Corner_LCD"))
                    vert = false;
                else
                    vert = true;
                
                Vertical = Util.ParseBool(IniKey.GetKey(owner.Block, gaugeHead, "Vertical", vert.ToString()));
                
                Flipped = Util.ParseBool(IniKey.GetKey(owner.Block, gaugeHead, "Flipped", "False"));
                Brightness = Util.ParseFloat(IniKey.GetKey(owner.Block, gaugeHead, "Brightness", "1"));

                // Set the sprite display mode
                Surface.ContentType = ContentType.SCRIPT;
                // Make sure no built-in script has been selected
                Surface.Script = "";
                // Set Background Color to black
                Surface.ScriptBackgroundColor = Color.Black;
            }
        }


		// DRAW GAUGE // - Draws the pressure display between room the lcd is locate in and the neighboring room.
		public static void DrawGauge(GaugeSurface gauge, Sector sectorA, Sector sectorB, bool locked, bool noSides)
		{
			IMyTextSurface drawSurface = gauge.Surface;
			bool vertical = gauge.Vertical;
			bool flipped = gauge.Flipped;
			float brightness = gauge.Brightness;

			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);

			float pressureA = sectorA.Vents[0].GetOxygenLevel();
			float pressureB = sectorB.Vents[0].GetOxygenLevel();

			// Set color of status frame.
			Color statusColor;
			if (locked)
				statusColor = Color.Red * brightness;
			else
				statusColor = Color.Green * brightness;


			var frame = drawSurface.DrawFrame();

			float height = drawSurface.SurfaceSize.Y;
			float width = viewport.Width;
			float textSize = 0.8f;
			float topEdge = viewport.Center.Y - viewport.Height / 2;
			float bottomEdge = viewport.Center.Y + viewport.Height / 2;
			if (width < SCREEN_THRESHHOLD)
				textSize = 0.4f;

			//Vector2 position;// = viewport.Center - new Vector2(width/2, 0);

			int redA = (int)(PRES_RED * (1 - pressureA) * brightness);
			int greenA = (int)(PRES_GREEN * pressureA * brightness);
			int blueA = (int)(PRES_BLUE * pressureA * brightness);
			int redB = (int)(PRES_RED * (1 - pressureB) * brightness);
			int greenB = (int)(PRES_GREEN * pressureB * brightness);
			int blueB = (int)(PRES_BLUE * pressureB * brightness);

			//Variables for position alignment and scale
			Vector2 leftPos, leftScale, leftTextPos, rightPos, rightTextPos, rightScale, leftReadingOffset, gridScale, position;
			TextAlignment leftChamberAlignment, rightChamberAlignment;

			if (vertical)
			{
				if (flipped)
				{
					leftPos = new Vector2(width, bottomEdge - height * pressureA * 0.5f);
					leftScale = new Vector2(-width * 0.425f, height * pressureA);
					leftTextPos = new Vector2(width * 0.78f, topEdge);
					rightPos = new Vector2(0, bottomEdge - height * pressureB * 0.5f);
					rightScale = new Vector2(width * 0.425f, height * pressureB);
					rightTextPos = new Vector2(width * 0.22f, topEdge);
				}
				else
				{
					leftPos = new Vector2(0, bottomEdge - height * pressureA * 0.5f);
					leftScale = new Vector2(width * 0.425f, height * pressureA);
					leftTextPos = new Vector2(width * 0.22f, topEdge);
					rightPos = new Vector2(width, bottomEdge - height * pressureB * 0.5f);
					rightScale = new Vector2(-width * 0.425f, height * pressureB);
					rightTextPos = new Vector2(width * 0.78f, topEdge);
				}

				leftChamberAlignment = TextAlignment.CENTER;
				rightChamberAlignment = TextAlignment.CENTER;
				leftReadingOffset = new Vector2(0, textSize * 25);
				gridScale = new Vector2(width * 20, height);
			}
			else
			{
				if (flipped)
				{
					leftPos = new Vector2(width, viewport.Center.Y);
					leftScale = new Vector2(-width * 0.425f * pressureA, height);
					leftTextPos = new Vector2(width - textSize * 10, topEdge);
					rightPos = new Vector2(0, viewport.Center.Y);
					rightScale = new Vector2(width * 0.425f * pressureB, height);
					rightTextPos = new Vector2(textSize * 10, topEdge);
					leftChamberAlignment = TextAlignment.RIGHT;
					rightChamberAlignment = TextAlignment.LEFT;
					leftReadingOffset = new Vector2(0, textSize * 25);
				}
				else
				{
					leftPos = new Vector2(0, viewport.Center.Y);
					leftScale = new Vector2(width * 0.425f * pressureA, height);
					leftTextPos = new Vector2(textSize * 10, topEdge);
					rightPos = new Vector2(width, viewport.Center.Y);
					rightScale = new Vector2(-width * 0.425f * pressureB, height);
					rightTextPos = new Vector2(width - textSize * 10, topEdge);
					leftChamberAlignment = TextAlignment.LEFT;
					rightChamberAlignment = TextAlignment.RIGHT;
					leftReadingOffset = new Vector2(textSize * 10, textSize * 25);
				}

				gridScale = new Vector2(width, height * 20);
			}

			// Left Chamber
			string leftText;
			Color leftColor;

			// If no assigned Side, draw gauge title in white without asterisk
			if (noSides)
			{
				leftText = sectorA.Name;
				leftColor = _textColor;
				leftReadingOffset = new Vector2(0, textSize * 25);
			}
			else
			{
				leftText = "*" + sectorA.Name;
				leftColor = _roomColor;
			}

			MyScreen.DrawTexture("SquareSimple", leftPos, leftScale, 0, new Color(redA, greenA, blueA), frame);
			MyScreen.WriteText(leftText, leftTextPos, leftChamberAlignment, textSize, leftColor, frame);
			leftTextPos += leftReadingOffset;
			MyScreen.WriteText((string.Format("{0:0.##}", (pressureA * _atmo * _factor))) + _unit, leftTextPos, leftChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Right Chamber
			MyScreen.DrawTexture("SquareSimple", rightPos, rightScale, 0, new Color(redB, greenB, blueB), frame);
			MyScreen.WriteText(sectorB.Name, rightTextPos, rightChamberAlignment, textSize, _textColor, frame);
			rightTextPos += new Vector2(0, textSize * 25);
			MyScreen.WriteText((string.Format("{0:0.##}", (pressureB * _atmo * _factor))) + _unit, rightTextPos, rightChamberAlignment, textSize * 0.75f, _textColor, frame);

			// Grid Texture
			position = new Vector2(0, viewport.Center.Y);
			MyScreen.DrawTexture("Grid", position, gridScale, 0, Color.Black, frame);
			position += new Vector2(1, 0);
			MyScreen.DrawTexture("Grid", position, new Vector2(width, height * 20), 0, Color.Black, frame);

			// Status Frame
			position = viewport.Center - new Vector2(width * 0.075f, 0);
			MyScreen.DrawTexture("SquareSimple", position, new Vector2(width * 0.15f, height), 0, statusColor, frame);

			// Door Background
			position = viewport.Center - new Vector2(width * 0.0625f, 0);
			MyScreen.DrawTexture("SquareSimple", position, new Vector2(width * 0.125f, height * 0.95f), 0, Color.Black, frame);

			// Door Status
			if (locked)
			{
				position = viewport.Center - new Vector2(width * 0.04f, 0);
				MyScreen.DrawTexture("Cross", position, new Vector2(width * 0.08f, width * 0.08f), 0, Color.White, frame);
			}
			else
			{
				position = viewport.Center - new Vector2(width * 0.06f, 0);
				MyScreen.DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float)Math.PI / -2, Color.White, frame);
				position += new Vector2(width * 0.04f, 0);
				MyScreen.DrawTexture("Arrow", position, new Vector2(width * 0.08f, width * 0.08f), (float)Math.PI / 2, Color.White, frame);
			}

			frame.Dispose();
		}

		public static void DrawSingleSectorGauge(GaugeSurface gauge, Sector sector)
        {
			IMyTextSurface drawSurface = gauge.Surface;
			var frame = drawSurface.DrawFrame();
			bool vertical = gauge.Vertical;
			float brightness = gauge.Brightness;

			RectangleF viewport = new RectangleF((drawSurface.TextureSize - drawSurface.SurfaceSize) / 2f, drawSurface.SurfaceSize);

			float pressure = sector.Vents[0].GetOxygenLevel();

			float height = drawSurface.SurfaceSize.Y;
			float width = viewport.Width;
			float textSize = 1.6f;
			float topEdge = viewport.Center.Y - viewport.Height / 2;
			float bottomEdge = viewport.Center.Y + viewport.Height / 2;
			if (width < SCREEN_THRESHHOLD)
				textSize = 0.8f;

			int red = (int)(PRES_RED * (1 - pressure) * brightness);
			int green = (int)(PRES_GREEN * pressure * brightness);
			int blue = (int)(PRES_BLUE * pressure * brightness);

			//Variables for position alignment and scale
			Vector2 pos, scale, textPos, readingOffset, gridScale, position;
			TextAlignment alignment;

			if(vertical)
            {
				pos = new Vector2(0, bottomEdge - height * pressure * 0.5f);
				scale = new Vector2(width, height * pressure);
				textPos = new Vector2(width * 0.5f, topEdge);
				alignment = TextAlignment.CENTER;
				readingOffset = new Vector2(0, textSize * 25);
				gridScale = new Vector2(width * 20, height);
			}
            else
            {
				pos = new Vector2(0, viewport.Center.Y);
				scale = new Vector2(width * pressure, height);
				textPos = new Vector2(textSize * 10, topEdge);
				alignment = TextAlignment.LEFT;
				readingOffset = new Vector2(textSize * 10, textSize * 25);
				gridScale = new Vector2(width, height * 20);
			}

			MyScreen.DrawTexture("SquareSimple", pos, scale, 0, new Color(red, green, blue), frame);
			MyScreen.WriteText(sector.Name, textPos, alignment, textSize, _textColor, frame);
			textPos += readingOffset;
			MyScreen.WriteText((string.Format("{0:0.##}", (pressure * _atmo * _factor))) + _unit, textPos, alignment, textSize * 0.75f, _textColor, frame);

			// Grid Texture
			position = new Vector2(0, viewport.Center.Y);
			MyScreen.DrawTexture("Grid", position, gridScale, 0, Color.Black, frame);
			position += new Vector2(1, 0);
			MyScreen.DrawTexture("Grid", position, new Vector2(width, height * 20), 0, Color.Black, frame);

			frame.Dispose();
		}
    }
}
