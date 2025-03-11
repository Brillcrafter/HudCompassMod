using System.Drawing;
using Draygo.API;

namespace HudCompassMod
{
    public class CompassDivisionClass
    {
        public bool MajorDiv; //if false its a minor div
        public HudAPIv2.HUDMessage Division;
        public string Character;
        public double Offset;

        public CompassDivisionClass(bool majorDiv,string character, double offset, HudAPIv2.HUDMessage division)
        {
            MajorDiv = majorDiv;
            Division = division;
            Character = character;
            Offset = offset;
        }
    }
}