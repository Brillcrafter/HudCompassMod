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
        private int _FOV = 90;
        private int _DegreePerDiv = 5;
        private double _HeadingXCentreLocation = 0f;
        private double _HeadingYCentreLocation = 0.5f;
        private double _TickerScale = 1D;
        
        
        #region HUD Variables

        private string _MajorDiv = "|";
        private string _MinorDiv = "•";
        private float _XoffsetPer2Degrees = 0.01f;
        private List<CompassDivisionClass> _HeadingCompassClass = new List<CompassDivisionClass>();
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
            var cameraForward = cameraViewMatrix.Forward.Normalized();
            
            //camera orientation
            double cameraAzimuth;
            double cameraElevation;
            Vector3D.GetAzimuthAndElevation(cameraForward, out cameraAzimuth, out cameraElevation);
            cameraAzimuth = MathHelperD.ToDegrees(cameraAzimuth);
            cameraElevation = MathHelperD.ToDegrees(cameraElevation);
            
            if (shipController != null)
            {
                inCockpit = true;
                //1 means pointing +ve axis -1 means pointing -ve axis
                var shipForward = shipController.WorldMatrix.Forward.Normalized();
                Vector3D.GetAzimuthAndElevation(shipForward, out ShipAzimuth, out ShipElevation);
                var shipUp = shipController.WorldMatrix.Up.Normalized();
                //compass bearings done, time to do the roll indicator
                var shipRight = Vector3D.Cross(shipForward, shipUp).Normalized();
                var shipRollAngle = Math.Acos(Vector3D.Dot(shipRight, new Vector3D(0, 0, 1)));
                shipRollAngleFloat = Convert.ToSingle(shipRollAngle);
                if (shipForward.Z < 0) shipRollAngleFloat = -shipRollAngleFloat;
            }
            Draw(ShipAzimuth, ShipElevation, cameraAzimuth, cameraElevation, shipRollAngleFloat, inCockpit);
            DrawHeadingTicker(ShipAzimuth, inCockpit);
        }

        #endregion
        
        private void Draw(double shipAzimuth, double shipElevation, double cameraAzimuth, double cameraElevation,
            float rollAngle, bool inCockpit)
        {
            if (HudApi.Heartbeat)
            {
                var config = HudCompassConfig.Instance;
                if (!FirstDraw)
                {
                    CreateHudMessageOffsets();
                    var shipAziInfo = new StringBuilder(Convert.ToInt32(shipAzimuth));
                    hudShipAzimuth = new HudAPIv2.HUDMessage(shipAziInfo, config.ShipAzimuth, null,
                        -1, config.TextSize, true, true);
                    var shipEleInfo = new StringBuilder(Convert.ToInt32(shipElevation));
                    hudShipElevation = new HudAPIv2.HUDMessage(shipEleInfo, config.ShipElevation, null,
                        -1, config.TextSize, true, true);
                    var cameraAziInfo = new StringBuilder(Convert.ToInt32(cameraAzimuth));
                    hudCameraAzimuth = new HudAPIv2.HUDMessage(cameraAziInfo, new Vector2D( -0.7, -0.625), null,
                        -1, config.TextSize, true, true);
                    var cameraEleInfo = new StringBuilder(Convert.ToInt32(cameraElevation));
                    hudCameraElevation = new HudAPIv2.HUDMessage(cameraEleInfo, config.CameraElevation, null,
                        -1, config.TextSize, true, true);
        
                    var shipRollIndicatorTexture = MyStringId.GetOrCompute("RollIndicator");
                    shipRollIndicator = new HudAPIv2.BillBoardHUDMessage(shipRollIndicatorTexture, Vector2D.Zero
                        , Color.White, null, 1, 1D, 1F, 1F, rollAngle);
                    FirstDraw = true;
                }
                else
                {
                    hudShipAzimuth.Message = new StringBuilder(Convert.ToInt32(shipAzimuth));
                    hudShipElevation.Message = new StringBuilder(Convert.ToInt32(shipElevation));
                    hudCameraAzimuth.Message = new StringBuilder(Convert.ToInt32(cameraAzimuth));
                    hudCameraElevation.Message = new StringBuilder(Convert.ToInt32(cameraElevation));
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

        private void DrawHeadingTicker(double shipAzimuth, bool InCockpit)
        {
            //this is where the heading ticker is drawn
            //im thinking 90 degrees FOV so there is always at least 1 compass heading in view
            //have it be a vertical line denoting a division, sprite or text? text, less resource intensive I think
            //there are 1 major and 4 minor divisions per 10 degrees
            //each major division is 10 degrees
            //will need 45 different hud items

            if (!InCockpit)
                return;
            
            var headingMinus45 = shipAzimuth - 45;
            if (headingMinus45 < 0)
                headingMinus45 *= -1;
            var headingPlus45 = shipAzimuth + 45;
            if (headingPlus45 > 360)
                headingPlus45 -= 360;
            
            var tickerOffset = shipAzimuth * 0.01;
            foreach (var division in _HeadingCompassClass)
            {
                if (tickerOffset + division.Offset < -0.45D || tickerOffset + division.Offset > 0.45D)
                    division.Division.Message = new StringBuilder();
                else
                    division.Division.Message = new StringBuilder(division.Character);
                division.Division.Offset = new Vector2D(tickerOffset + division.Offset, division.Division.Offset.Y);
            }
        }

        private void DrawElevationTicker(double shipElevation, double cameraAzimuth, bool inCockpit)
        {
        }

        private void CreateHudMessageOffsets()
        {
            //for heading ticker
            var Heading = -90;
            for (var i = -2.7D; i < 2.7D; i += 0.18D)
            {
                //starting from the left
                //then its a major division
                if (Heading < 0)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,Math.Abs(Heading).ToString(), i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                else if (Heading > 360)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,(360-Heading).ToString(), i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                
                else if(i == -1.8D)//these are for the cardinal directions
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,"N", i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                else if(i == -0.9D)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "E", i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                else if(i == 0D)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "S", i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                else if (i == 0.9D)
                    _HeadingCompassClass.Add(new CompassDivisionClass(true, "W", i , new HudAPIv2.HUDMessage(new StringBuilder(),
                        new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                        -1,_TickerScale)));
                else 
                    _HeadingCompassClass.Add(new CompassDivisionClass(true,Heading.ToString(), i , new HudAPIv2.HUDMessage(new StringBuilder(),
                    new Vector2D(_HeadingXCentreLocation, _HeadingYCentreLocation), new Vector2D(i, 0D),
                    -1,_TickerScale)));
            }
        }
    }
}