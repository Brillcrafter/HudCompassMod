using Sandbox.ModAPI;

namespace HudCompassMod
{
    public class Tools
    {
        
        
        public static bool IsDedicatedServer =>
            MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Utilities.IsDedicated;
        
        
    }
}