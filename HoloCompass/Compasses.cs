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
        public class Compass
        {
            public IMyProjector Block;
            public int PitchOffset; // x
            public int YawOffset; // y
            public int RollOffset; // z

            MyIni Ini;

            public Compass(IMyProjector projector)
            {
                Block = projector;
                Ini = GetIni(Block);

                // Get heading of reference block and projector block
                Vector3I refHeading = VectorToDegrees(_refBlock.WorldMatrix.Forward);
                Vector3I blockHeading = VectorToDegrees(Block.WorldMatrix.Forward);

                MatrixD blockOrientation = projector.WorldMatrix.GetOrientation();
                MatrixD refOrientation = _refBlock.WorldMatrix.GetOrientation();

                Quaternion refQuat = Quaternion.CreateFromForwardUp(_refBlock.WorldMatrix.Forward, _refBlock.WorldMatrix.Up);
                Quaternion blockQuat = Quaternion.CreateFromForwardUp(Block.WorldMatrix.Forward, Block.WorldMatrix.Up);


                // Get Heading difference and set offsets
                Vector3I blockOffset = ReduceVector(blockHeading - refHeading);
                PitchOffset = blockOffset.X;
                YawOffset = blockOffset.Y;
                RollOffset = blockOffset.Z;
            }
        }


        // ALIGN COMPASSES //
        void AlignCompasses()
        {
            if (_compasses.Count < 1)
            {
                Echo("No blocks found with tag \"" + COMPASS_TAG + "\" found!");
                return;
            }

            foreach(Compass compass in _compasses)
            {
                IMyProjector projector = compass.Block;
                Echo(projector.CustomName + "\n Offsets - x:" + compass.PitchOffset + ", y:" + compass.YawOffset + ", z:" + compass.RollOffset);
                Matrix matrix = new Matrix();
                projector.Orientation.GetMatrix(out matrix);
                Echo(matrix.ToString());


                if (projector.IsProjecting)
                    Echo(" Holo: " + projector.ProjectionRotation.ToString() + "\n");
                else
                    Echo(" Inactive\n");
            }
        }
    }
}
