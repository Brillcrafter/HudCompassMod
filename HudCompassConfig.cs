using System;
using System.IO;
using Draygo.API;
using HudCompassMod;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace HudCompass.Data.Scripts.HudCompass
{
    [ProtoContract]
    public class HudCompassConfig
    {
        public static HudCompassConfig Instance = new HudCompassConfig();
        
        private HudAPIv2 HudApi;

        #region Config Settings

        public static readonly HudCompassConfig Default = new HudCompassConfig()
        {
            ShowCameraNumbers = true,
            ShipAzimuth = Vector2D.Zero,
            ShipElevation = Vector2D.Zero,
            CameraAzimuth = Vector2D.Zero,
            CameraElevation = Vector2D.Zero,
            TextSize = 1f
        };

        [ProtoMember(1)]
        public bool ShowCameraNumbers { get; set; } = true;
        [ProtoMember(2)]
        public Vector2D ShipAzimuth { get; set; } = Vector2D.Zero;
        [ProtoMember(3)]
        public Vector2D ShipElevation { get; set; } = Vector2D.Zero;
        [ProtoMember(4)]
        public Vector2D CameraAzimuth { get; set; } = Vector2D.Zero;
        [ProtoMember(5)]
        public Vector2D CameraElevation { get; set; } = Vector2D.Zero;
        [ProtoMember(6)] 
        public float TextSize { get; set; } = 1f;
        #endregion
        
        #region HudAPI Fields

        private HudAPIv2.MenuRootCategory SettingsMenu;
        private HudAPIv2.MenuSubCategory CameraSubCategory, ShipSubCategory;
        private HudAPIv2.MenuItem EnableCameraItem;
        private HudAPIv2.MenuTextInput TextSizeInput, ShipAzimuthXInput, ShipAzimuthYInput,
            CameraAzimuthXInput, CameraAzimuthYInput, ShipElevationXInput, ShipElevationYInput,
            CameraElevationXInput, CameraElevationYInput;
        #endregion

        public static void InitConfig()
        {
            string Filename = "HudCompassConfig.cfg";

            try
            {
                var localFileExists = MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(HudCompassConfig));
                if (!Tools.IsDedicatedServer && localFileExists)
                {
                    MyLog.Default.WriteLineAndConsole($"HudCompass: starting config. Local Exists: {localFileExists}");
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(HudCompassConfig));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    if (text.Length == 0)//if someone has been messing with it and its blank
                    {
                        MyAPIGateway.Utilities.ShowMessage("HudCompass", "Error with config file, overwriting with default.");
                        MyLog.Default.Error($"HudCompass: Error with config file, overwriting with default");
                        //run the save function with default
                    }
                    else
                    {
                        HudCompassConfig config = MyAPIGateway.Utilities.SerializeFromXML<HudCompassConfig>(text);
                        //run save with config
                    }
                }
                else //there is no config present
                {
                    MyLog.Default.WriteLineAndConsole($"HudCompass: Local config doesn't exist. Creating default");
                    //run save with default
                }
            }
            catch (Exception ex)
            {
                //run save with default
                MyAPIGateway.Utilities.ShowMessage("HudCompass", "Error with config file, overwriting with default." + ex);
                MyLog.Default.Error($"HudCompass: Error with config file, overwriting with default {ex}");

            }
        }
        public static void Save(HudCompassConfig config)
        {
            string Filename = "HudCompassConfig.cfg";
            try
            {
                if (!Tools.IsDedicatedServer)
                {
                    MyLog.Default.WriteLineAndConsole($"HudCompass: Saving config.");
                    TextWriter writer;
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(HudCompassConfig));
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(config));
                    writer.Close();
                }
                Instance = config;
            }
            catch (Exception ex)
            {
                MyLog.Default.Error($"HudCompass: Error saving config file {ex}");
            }
        }

        //setup the settings menu
        public void InitMenu()
        {
           SettingsMenu = new HudAPIv2.MenuRootCategory("Hud Compass", 
               HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Hud Compass Settings");
           CameraSubCategory = new HudAPIv2.MenuSubCategory("Camera Settings >>", SettingsMenu, "Camera Settings");
           EnableCameraItem = new HudAPIv2.MenuItem($"Enable azi and Ele in Camera: {CameraAzimuth}", CameraSubCategory, ShowEnableCamera);
           CameraAzimuthXInput = new HudAPIv2.MenuTextInput($"Camera Azimuth X location: {CameraAzimuth.X}",
               CameraSubCategory,"", UpdateCamAziX);
           CameraAzimuthYInput = new HudAPIv2.MenuTextInput($"Camera Azimuth Y location: {CameraAzimuth.Y}",
               CameraSubCategory,"", UpdateCamAziY);
           CameraElevationXInput = new HudAPIv2.MenuTextInput($"Camera Elevation X location: {CameraElevation.X}",
               CameraSubCategory,"", UpdateCamEleX);
           CameraElevationYInput = new HudAPIv2.MenuTextInput($"Camera Elevation Y location: {CameraElevation.Y}",
               CameraSubCategory,"", UpdateCamEleY);
           
           ShipSubCategory = new HudAPIv2.MenuSubCategory("Ship Settings", SettingsMenu, "Ship Settings");
           ShipAzimuthXInput = new HudAPIv2.MenuTextInput($"Ship Azimuth X location: {ShipAzimuth.X}", ShipSubCategory,
               "",UpdateShipAziX);
           ShipAzimuthYInput = new HudAPIv2.MenuTextInput($"Ship Azimuth Y location: {ShipAzimuth.Y}", ShipSubCategory,
               "",UpdateShipAziY);
           ShipElevationXInput = new HudAPIv2.MenuTextInput($"Ship Elevation X location: {ShipElevation.X}",
               ShipSubCategory,"" ,UpdateShipEleX);
           ShipElevationYInput = new HudAPIv2.MenuTextInput($"Ship Elevation Y location: {ShipElevation.Y}",
               ShipSubCategory,"", UpdateShipEleY);
        }

        private void ShowEnableCamera()
        {
            ShowCameraNumbers = !ShowCameraNumbers;
            EnableCameraItem.Text = $"Enable azi and Ele in Camera: {CameraAzimuth}";
            Save(this);
        }

        private void UpdateCamAziX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var azimuth = CameraAzimuth;
            azimuth.X = getter;
            CameraAzimuth = azimuth;
            CameraAzimuthXInput.Text = $"Camera Azimuth X location: {CameraAzimuth.X}";
            Save(this);  
        }

        private void UpdateCamAziY(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var azimuth = CameraAzimuth;
            azimuth.Y = getter;
            CameraAzimuth = azimuth;
            CameraAzimuthYInput.Text = $"Camera Azimuth Y location: {CameraAzimuth.Y}";
            Save(this);
        }

        private void UpdateCamEleX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var elevation = CameraElevation;
            elevation.X = getter;
            CameraElevation = elevation;
            CameraElevationXInput.Text = $"Camera Elevation X location: {CameraElevation.X}";
            Save(this);
        }
        
        private void UpdateCamEleY(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var elevation = CameraElevation;
            elevation.Y = getter;
            CameraElevation = elevation;
            CameraElevationYInput.Text = $"Camera Elevation Y location: {CameraElevation.Y}";
            Save(this);
        }
        
        private void UpdateShipAziX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var shipAzimuth = ShipAzimuth;
            shipAzimuth.X = getter;
            ShipAzimuth = shipAzimuth;
            ShipAzimuthXInput.Text = $"Ship Azimuth X location: {ShipAzimuth.X}";
            Save(this);
        }

        private void UpdateShipAziY(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var shipAzimuth = ShipAzimuth;
            shipAzimuth.Y = getter;
            ShipAzimuth = shipAzimuth;
            ShipAzimuthYInput.Text = $"Ship Azimuth Y location: {ShipAzimuth.Y}";
            Save(this);
        }

        private void UpdateShipEleX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var elevation = ShipElevation;
            elevation.X = getter;
            ShipElevation = elevation;
            ShipElevationXInput.Text = $"Ship Elevation X location: {ShipElevation.X}";
            Save(this);
        }

        private void UpdateShipEleY(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var elevation = ShipElevation;
            elevation.Y = getter;
            ShipElevation = elevation;
            ShipElevationYInput.Text = $"Ship Elevation Y location: {ShipElevation.Y}";
            Save(this);
        }
        
    }
}