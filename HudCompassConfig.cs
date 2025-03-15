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

        public static readonly HudCompassConfig Default = new HudCompassConfig
        {
            ShowCameraNumbers = true,
            ShipAzimuth = new Vector2D(0, 0.95),
            ShipElevation = new Vector2D(-0.95, 0),
            CameraAzimuth = new Vector2D(0, 0.91),
            CameraElevation = new Vector2D(-0.91, 0),
            TextSize = 1f,
            ShipAzimuthTicker = new Vector2D(0, 0.99),
            ShipElevationTicker = new Vector2D(-0.99, 0),
        };

        [ProtoMember(1)]
        public bool ShowCameraNumbers { get; set; } = true;
        [ProtoMember(2)]
        public Vector2D ShipAzimuth { get; set; } = new Vector2D(0, 0.95);
        [ProtoMember(3)]
        public Vector2D ShipElevation { get; set; } = new Vector2D(-0.95, 0);
        [ProtoMember(4)]
        public Vector2D CameraAzimuth { get; set; } = new Vector2D(0, 0.90);
        [ProtoMember(5)]
        public Vector2D CameraElevation { get; set; } = new Vector2D(-0.90, 0);
        [ProtoMember(6)] 
        public float TextSize { get; set; } = 1f;
        [ProtoMember(7)]
        public Vector2D ShipAzimuthTicker { get; set; } = new Vector2D(0, 0.99);
        [ProtoMember(8)]
        public Vector2D ShipElevationTicker { get; set; } = new Vector2D(-0.99, 0);
        
        #endregion
        
        #region HudAPI Fields

        private HudAPIv2.MenuRootCategory SettingsMenu;
        private HudAPIv2.MenuSubCategory CameraSubCategory, ShipSubCategory;
        private HudAPIv2.MenuItem EnableCameraItem;
        private HudAPIv2.MenuTextInput TextSizeInput, ShipAzimuthXInput, ShipAzimuthYInput,
            CameraAzimuthXInput, CameraAzimuthYInput, ShipElevationXInput, ShipElevationYInput,
            CameraElevationXInput, CameraElevationYInput, ShipAzimuthTickerXInput, ShipAzimuthTickerYInput,
            ShipElevationTickerXInput, ShipElevationTickerYInput;
        #endregion

        public static void InitConfig()
        {
            string Filename = "HudCompassConfig.cfg";

            try
            {
                var localFileExists = MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, 
                    typeof(HudCompassConfig));
                if (!Tools.IsDedicatedServer && localFileExists)
                {
                    MyLog.Default.WriteLineAndConsole($"HudCompass: starting config. Local Exists: {localFileExists}");
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, 
                        typeof(HudCompassConfig));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    if (text.Length == 0)//if someone has been messing with it and its blank
                    {
                        MyAPIGateway.Utilities.ShowMessage("HudCompass",
                            "Error with config file, overwriting with default.");
                        MyLog.Default.Error($"HudCompass: Error with config file, overwriting with default");
                        Save(Default);
                    }
                    else
                    {
                        HudCompassConfig config = MyAPIGateway.Utilities.SerializeFromXML<HudCompassConfig>(text);
                        Save(config);
                    }
                }
                else //there is no config present
                {
                    MyLog.Default.WriteLineAndConsole($"HudCompass: Local config doesn't exist. Creating default");
                    Save(Default);
                }
            }
            catch (Exception ex)
            {
                Save(Default);
                MyAPIGateway.Utilities.ShowMessage("HudCompass",
                    "Error with config file, overwriting with default." + ex);
                MyLog.Default.Error($"HudCompass: Error with config file, overwriting with default {ex}");

            }
            
        }
        public static void Save(HudCompassConfig config)
        {
            var Filename = "HudCompassConfig.cfg";
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
           CameraSubCategory = new HudAPIv2.MenuSubCategory("Camera Settings >>", SettingsMenu,
               "Camera Settings");
           EnableCameraItem = new HudAPIv2.MenuItem($"Enable azi and Ele in Camera: {ShowCameraNumbers}", 
               CameraSubCategory, ShowEnableCamera);
           CameraAzimuthXInput = new HudAPIv2.MenuTextInput($"Camera Azimuth X location: {CameraAzimuth.X}",
               CameraSubCategory,"-1 to 1", UpdateCamAziX);
           CameraAzimuthYInput = new HudAPIv2.MenuTextInput($"Camera Azimuth Y location: {CameraAzimuth.Y}",
               CameraSubCategory,"-1 to 1", UpdateCamAziY);
           CameraElevationXInput = new HudAPIv2.MenuTextInput($"Camera Elevation X location: {CameraElevation.X}",
               CameraSubCategory,"-1 to 1", UpdateCamEleX);
           CameraElevationYInput = new HudAPIv2.MenuTextInput($"Camera Elevation Y location: {CameraElevation.Y}",
               CameraSubCategory,"-1 to 1", UpdateCamEleY);
           
           ShipSubCategory = new HudAPIv2.MenuSubCategory("Ship Settings", SettingsMenu,
               "Ship Settings");
           ShipAzimuthXInput = new HudAPIv2.MenuTextInput($"Ship Azimuth X location: {ShipAzimuth.X}", ShipSubCategory,
               "-1 to 1",UpdateShipAziX);
           ShipAzimuthYInput = new HudAPIv2.MenuTextInput($"Ship Azimuth Y location: {ShipAzimuth.Y}", ShipSubCategory,
               "-1 to 1",UpdateShipAziY);
           ShipElevationXInput = new HudAPIv2.MenuTextInput($"Ship Elevation X location: {ShipElevation.X}",
               ShipSubCategory,"-1 to 1" ,UpdateShipEleX);
           ShipElevationYInput = new HudAPIv2.MenuTextInput($"Ship Elevation Y location: {ShipElevation.Y}",
               ShipSubCategory,"-1 to 1", UpdateShipEleY);
           ShipAzimuthTickerXInput = new HudAPIv2.MenuTextInput($"Ship Azimuth Ticker X location: {ShipAzimuthTicker.X}"
               ,ShipSubCategory, "-1 to 1", UpdateShipAziTickerX);
           ShipAzimuthTickerYInput = new HudAPIv2.MenuTextInput($"Ship Azimuth Ticker Y Location: {ShipAzimuthTicker.Y}"
               , ShipSubCategory, "-1 to 1", UpdateShipAziTickerY);
           ShipElevationTickerXInput = new HudAPIv2.MenuTextInput(
               $"Ship Elevation Ticker X Location: {ShipElevationTicker.X}",
               ShipSubCategory, "-1 to 1", UpdateShipElevationTickerX);
           ShipElevationTickerYInput = new HudAPIv2.MenuTextInput(
               $"Ship Azimuth Ticker Y Location: {ShipElevationTicker.Y}",
               ShipSubCategory, "-1 to 1", UpdateShipElevationTickerY);
        }

        private void ShowEnableCamera()
        {
            ShowCameraNumbers = !ShowCameraNumbers;
            EnableCameraItem.Text = $"Enable azi and Ele in Camera: {ShowCameraNumbers}";
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

        private void UpdateShipAziTickerX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var aziTicker = ShipAzimuthTicker;
            aziTicker.X = getter;
            ShipAzimuthTicker = aziTicker;
            ShipAzimuthTickerXInput.Text = $"Ship Azimuth Ticker X location: {ShipAzimuthTicker.X}";
            Save(this);
        }

        private void UpdateShipAziTickerY(string obj)
        {
            double getter;
            if (!double.TryParse(obj, out getter))
                return;
            var aziTicker = ShipAzimuthTicker;
            aziTicker.Y = getter;
            ShipAzimuthTicker = aziTicker;
            ShipAzimuthTickerYInput.Text = $"Ship Azimuth Ticker Y location: {ShipAzimuthTicker.Y}";
            Save(this);
        }

        private void UpdateShipElevationTickerY(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var eleTicker = ShipElevationTicker;
            eleTicker.Y = getter;
            ShipElevationTicker = eleTicker;
            ShipElevationTickerYInput.Text = $"Ship Elevation Ticker Y location: {ShipElevationTicker.Y}";
            Save(this);
        }

        private void UpdateShipElevationTickerX(string obj)
        {
            float getter;
            if (!float.TryParse(obj, out getter))
                return;
            var eleTicker = ShipElevationTicker;
            eleTicker.X = getter;
            ShipElevationTicker = eleTicker;
            ShipElevationTickerXInput.Text = $"Ship Elevation Ticker X location: {ShipElevationTicker.X}";
            Save(this);
        }
        
    }
}