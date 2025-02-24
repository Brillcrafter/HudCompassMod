using System;
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
        
        private HudAPIv2.HUDMessage hudShipAzimuth;
        private HudAPIv2.HUDMessage hudShipElevation;
        private HudAPIv2.HUDMessage hudCameraAzimuth;
        private HudAPIv2.HUDMessage hudCameraElevation;
        private HudAPIv2.BillBoardHUDMessage shipRollIndicator;
        
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

            double shipElevation = 0f;
            double shipAzimuth = 0f;
            var shipRollAngleFloat = 0f;
            
            var cameraViewMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var cameraForward = cameraViewMatrix.Forward.Normalized();
            var cameraForwardHori = new Vector3D(cameraForward.X, cameraForward.Y, 0).Normalized();
            
            //camera orientation
            var cameraAzimuth = Math.Atan2(cameraForwardHori.Y, cameraForwardHori.X);
            cameraAzimuth = MathHelperD.ToDegrees(cameraAzimuth);
            if (cameraAzimuth < 0) cameraAzimuth += 360; 
            var cameraElevation = Math.Asin(cameraForward.Z);
            cameraElevation = MathHelperD.ToDegrees(cameraElevation);


            if (shipController != null)
            {
                inCockpit = true;
                //1 means pointing +ve axis -1 means pointing -ve axis
                var shipForward = shipController.WorldMatrix.Forward.Normalized();
                var shipForwardHori = new Vector3D(shipForward.X, shipForward.Y, 0).Normalized();
                var shipUp = shipController.WorldMatrix.Up.Normalized();

                shipAzimuth = Math.Atan2(shipForwardHori.Y, shipForwardHori.X);
                shipAzimuth = MathHelperD.ToDegrees(shipAzimuth);
                if (shipAzimuth < 0) shipAzimuth += 360;
                
                shipElevation = Math.Asin(shipForward.Z);
                shipElevation = MathHelperD.ToDegrees(shipElevation);
                //compass bearings done, time to do the roll indicator
                var shipRight = Vector3D.Cross(shipForward, shipUp).Normalized();
                var shipRollAngle = Math.Acos(Vector3D.Dot(shipRight, new Vector3D(0, 0, 1)));
                shipRollAngleFloat = Convert.ToSingle(MathHelperD.ToDegrees(shipRollAngle));
                if (shipForward.Z < 0) shipRollAngleFloat = -shipRollAngleFloat;
            }
            Draw(shipAzimuth, shipElevation, cameraAzimuth, cameraElevation, shipRollAngleFloat, inCockpit);
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

        private void DrawHeadingTicker()
        {
            //this is where the heading ticker is drawn
            //taking inspiration from vtolVR for the look
            //im thinking 90 degrees FOV so there is always at least 1 compass heading in view
            //how to actually draw them on the screen
            //have it be a vertical line denoting a division, sprite or text? text, less resource intensive I think
            
        }

        private void DrawElevationTicker()
        {
            
        }
    }
}