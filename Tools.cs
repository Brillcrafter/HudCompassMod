using Sandbox.ModAPI;

namespace HudCompassMod
{
    public static class Tools
    {
        
        
        public static bool IsDedicatedServer =>
            MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Utilities.IsDedicated;
        
        
    }
}