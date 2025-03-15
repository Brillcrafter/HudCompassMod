using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Draygo.API;
using HudCompass.Data.Scripts.HudCompass;
using HudCompassMod;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace HudCompass
{
    
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class HudCompassSession : MySessionComponentBase
    {
        public const string Keyword = "/HudC";
        public const string ModName = "HudCompass";
        public static HudCompassSession Instance { get; private set; }
        public string SessionName = "";
        public string HudCompassFile = "HudCompass";
        private HudAPIv2 HudApi;
        private bool FirstDraw;
        private double _TickerScale = 1D;
        
        
        #region HUD Variables
        
        private List<CompassDivisionClass> _HeadingCompassClass = new List<CompassDivisionClass>();
        private List<CompassDivisionClass> _ElevationCompassClass = new List<CompassDivisionClass>();
        private HudAPIv2.HUDMessage hudShipAzimuth;
        private HudAPIv2.HUDMessage hudShipElevation;
        private HudAPIv2.HUDMessage hudCameraAzimuth;
        private HudAPIv2.HUDMessage hudCameraElevation;
        private HudAPIv2.BillBoardHUDMessage shipRollIndicator;
        
        #endregion
        #region Session Overrides

        public override void LoadData()
        {
            if (Tools.IsDedicatedServer)
                return;
            Instance = this;
        }

        public override void BeforeStart()
        {
            SessionName = string.Concat(Session.Name.Split(Path.GetInvalidFileNameChars()));
            if (Tools.IsDedicatedServer)
                return;
            
            //load the config
            HudCompassConfig.InitConfig();
            HudApi = new HudAPIv2(HudCompassConfig.Instance.InitMenu);
            if (HudApi == null)
                MyAPIGateway.Utilities.ShowMessage("HudCompass", "TextHudAPI failed to register");
        }

        public override void SaveData()
        {
            if (Tools.IsDedicatedServer)
                return;
            //save the current config
            HudCompassConfig.Save(HudCompassConfig.Instance);
        }

        protected override void UnloadData()
        {
            if (Tools.IsDedicatedServer)
                return;
            //save config
            HudCompassConfig.Save(HudCompassConfig.Instance);
        }

        public override void Draw()//yeet all the stuff here
        {
            var inCockpit = false;
            var shipController = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyShipController;
            if (Tools.IsDedicatedServer)
                return;
            
            var shipRollAngleFloat = 0f;
            double ShipAzimuth = 0;
            double ShipElevation = 0;
            
            var cameraViewMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var cameraForward = cameraViewMatrix.Forward;
            
            //camera orientation
            var cameraAzimuth = Math.Atan2(cameraForward.X, cameraForward.Y) * (180.0 / Math.PI);
            var cameraElevation = Math.Asin(cameraForward.Z) * (180.0 / Math.PI);
            cameraAzimuth = (cameraAzimuth + 360) % 360;
            cameraElevation = MathHelperD.Clamp(cameraElevation, -90, 90);
            
            if (shipController != null)
            {
                inCockpit = true;
                var shipForward = shipController.WorldMatrix.Forward;
                var shipUp = shipController.WorldMatrix.Up;
                ShipAzimuth = Math.Atan2(shipForward.X, shipForward.Y) * (180.0 / Math.PI);
                ShipElevation = Math.Asin(shipForward.Z) * (180.0 / Math.PI);
                ShipAzimuth = (ShipAzimuth + 360) % 360;
                ShipElevation = MathHelperD.Clamp(ShipElevation, -90, 90);
                var shipRollAngle = Math.Atan2(shipUp.X, shipUp.Z);
                //compass bearings done, time to do the roll indicator
                shipRollAngleFloat = Convert.ToSingle(shipRollAngle);
                if (shipForward.Z < 0) shipRollAngleFloat = -shipRollAngleFloat;
            }
            DrawMessages(ShipAzimuth, ShipElevation, cameraAzimuth, cameraElevation, shipRollAngleFloat, inCockpit);
            DrawHeadingTicker(ShipAzimuth, inCockpit);
            DrawElevationTicker(ShipElevation, inCockpit);
        }

        #endregion
        
        private void DrawMessages(double shipAzimuth, double shipElevation, double cameraAzimuth, double cameraElevation,
            float rollAngle, bool inCockpit)
        {
            if (HudApi.Heartbeat)
            {
                var config = HudCompassConfig.Default;
                if (!FirstDraw)
                {
                    CreateHudMessageOffsets();
                    var shipAziInfo = new StringBuilder(Convert.ToInt32(shipAzimuth).ToString());
                    hudShipAzimuth = new HudAPIv2.HUDMessage(shipAziInfo, config.ShipAzimuth, null,
                        -1, config.TextSize, true, true);
                    var shipEleInfo = new StringBuilder(Convert.ToInt32(shipElevation).ToString());
                    hudShipElevation = new HudAPIv2.HUDMessage(shipEleInfo, config.ShipElevation, null,
                        -1, config.TextSize, true, true);
                    var cameraAziInfo = new StringBuilder(Convert.ToInt32(cameraAzimuth).ToString());
                    hudCameraAzimuth = new HudAPIv2.HUDMessage(cameraAziInfo, config.CameraAzimuth, null,
                        -1, config.TextSize, true, true);
                    var cameraEleInfo = new StringBuilder(Convert.ToInt32(cameraElevation).ToString());
                    hudCameraElevation = new HudAPIv2.HUDMessage(cameraEleInfo, config.CameraElevation, null,
                        -1, config.TextSize, true, true);
        
                    var shipRollIndicatorTexture = MyStringId.GetOrCompute("RollIndicator");
                    shipRollIndicator = new HudAPIv2.BillBoardHUDMessage(shipRollIndicatorTexture, Vector2D.Zero
                        , Color.White, null, 1, 1D, 1F, 1F, rollAngle);
                    FirstDraw = true;
                }
                else
                {
                    hudShipAzimuth.Message = new StringBuilder(Convert.ToInt32(shipAzimuth).ToString());
                    hudShipAzimuth.Origin = config.ShipAzimuth;
                    hudShipElevation.Message = new StringBuilder(Convert.ToInt32(shipElevation).ToString());
                    hudShipElevation.Origin = config.ShipElevation;
                    hudCameraAzimuth.Message = new StringBuilder(Convert.ToInt32(cameraAzimuth).ToString());
                    hudCameraAzimuth.Origin = config.CameraAzimuth;
                    hudCameraElevation.Message = new StringBuilder(Convert.ToInt32(cameraElevation).ToString());
                    hudCameraElevation.Origin = config.CameraElevation;
                    shipRollIndicator.Rotation = rollAngle;
                }
                if (config.ShowCameraNumbers)
                {
                    hudCameraAzimuth.Visible = true;
                    hudCameraElevation.Visible = true;
                }
                else
                {
                    hudCameraAzimuth.Visible = false;
                    hudCameraElevation.Visible = false;
                }

                if (inCockpit)
                {
                    hudShipAzimuth.Visible = true;
                    hudShipElevation.Visible = true;
                    shipRollIndicator.Visible = true;
                }
                else
                {
                    hudShipAzimuth.Visible = false;
                    hudShipElevation.Visible = false;
                    shipRollIndicator.Visible = false;
                }
            }
            else
            {
                MyLog.Default.Error($"HudCompass: No heartbeat and {inCockpit}");
            }
        }

        private void DrawHeadingTicker(double shipAzimuth, bool inCockpit)
        {
            if (!inCockpit)
            {
                foreach (var division in _HeadingCompassClass)
                {
                    division.Division.Message = new StringBuilder(1);
                }
                return;
            }
            var config = HudCompassConfig.Default;
            var tickerOffset = shipAzimuth * 0.01;
            foreach (var division in _HeadingCompassClass)
            {
                if (tickerOffset - division.Offset < -0.45D || tickerOffset - division.Offset > 0.45D)
                    division.Division.Message = new StringBuilder(1);
                else
                    division.Division.Message = new StringBuilder(division.Character);
                division.Division.Offset = new Vector2D(division.Offset - tickerOffset , division.Division.Offset.Y);
                division.Division.Origin = config.ShipAzimuthTicker;
            }
        }

        private void DrawElevationTicker(double shipElevation, bool inCockpit)
        {
            if (!inCockpit)
            {
                foreach (var division in _ElevationCompassClass)
                {
                    division.Division.Message = new StringBuilder(1);
                }
                return;
            }
            var config = HudCompassConfig.Default;
            var tickerOffset = shipElevation * 0.01;
            foreach (var division in _ElevationCompassClass)
            {
                if (tickerOffset - division.Offset < -0.45D || tickerOffset - division.Offset > 0.45D)
                    division.Division.Message = new StringBuilder(1);
                else
                    division.Division.Message = new StringBuilder(division.Character);
                division.Division.Offset = new Vector2D(division.Division.Offset.X, division.Offset - tickerOffset);
                division.Division.Origin = config.ShipElevationTicker;
            }
        }

        private void CreateHudMessageOffsets()
        {
            var config = HudCompassConfig.Default;
            //for heading ticker
            for (var i = -45; i < 405; i += 5)
            {
                //starting from the left
                if(i == 0)//these are for the cardinal directions
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,"N", (double)i/100 
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100 , 0D),
                        -1,_TickerScale)));
                else if(i == 90)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "E", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else if(i == 180)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "S", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else if (i == 270)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "W", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else if (i == 360)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "N", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else if (i < 0)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,(360 - i * -1).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else if (i > 360)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,(i - 360).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                        -1,_TickerScale)));
                else 
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,i.ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipAzimuthTicker, new Vector2D((double)i/100, 0D),
                    -1,_TickerScale)));
            }
            // for elevation ticker
            for (var i = -135; i < 135; i += 5)
            {
                if (i < -90)
                    _ElevationCompassClass.Add(new CompassDivisionClass(true,(90 - i * -1).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipElevationTicker, new Vector2D(0D, (double)i/100),
                        -1,_TickerScale)));
                else if (i > 90)
                    _ElevationCompassClass.Add(new CompassDivisionClass(true,(i - 90).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipElevationTicker, new Vector2D(0D, (double)i/100),
                        -1,_TickerScale)));
                else 
                    _ElevationCompassClass.Add(new CompassDivisionClass(true,i.ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        config.ShipElevationTicker, new Vector2D(0D, (double)i/100),
                    -1,_TickerScale)));
            }
        }
    }
}