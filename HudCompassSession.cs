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
        
        

        public HudAPIv2 HudApi;
        

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

        public override void UpdateBeforeSimulation()//yeet all the stuff here
        {
            bool inCockpit = false;
            var shipController = MyAPIGateway.Session.Player.Controller.ControlledEntity as IMyShipController;
            if (Tools.IsDedicatedServer)
                return;

            double shipElevation = 0f;
            double shipAzimuth = 0f;
            float shipRollAngleFloat = 0f;
            
            MatrixD cameraViewMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D cameraForward = cameraViewMatrix.Forward;
            double cameraAlignmentX = cameraForward.Dot(Vector3D.Right);
            double cameraAlignmentY = cameraForward.Dot(Vector3D.Up);
            double cameraAlignmentZ = cameraForward.Dot(Vector3D.Backward);
            
            //now to convert that into bearings, this will be fun
            //x and y are the azimuth
            //z is the elevation
            // will need a roll angle
            
            //camera orientation
            var cameraAzimuth = Math.Atan2(cameraAlignmentX, cameraAlignmentY);
            var cameraXY = new Vector3D
            {
                X = cameraAlignmentX,
                Y = cameraAlignmentY
            };
            var cameraXYDist = Vector3D.Distance(Vector3.Zero, cameraXY);
            var cameraElevation = Math.Atan2(cameraXYDist, cameraAlignmentZ);
            cameraAzimuth = cameraAzimuth * (180.0 / Math.PI);
            cameraAzimuth = (cameraAzimuth + 360) % 360;
            cameraElevation = cameraElevation * (180.0 / Math.PI);
            cameraElevation = ((cameraElevation + 360) % 360)+ 90;


            if (shipController != null)
            {
                inCockpit = true;
                //1 means pointing +ve axis -1 means pointing -ve axis
                Vector3D shipForward = shipController.WorldMatrix.Forward;
                double shipAlignmentX = shipForward.Dot(Vector3D.Right);    // World X-Axis
                double shipAlignmentY = shipForward.Dot(Vector3D.Up);       // World Y-Axis
                double shipAlignmentZ = shipForward.Dot(Vector3D.Backward); // World Z-Axis
                //thanks Oz for the code
                
                //ship orientation
                //azimuth
                //remove the Z axis, just X and Y
                shipAzimuth = Math.Atan2(shipAlignmentX, shipAlignmentY);
                var XY = new Vector3D
                {
                    X = shipAlignmentX,
                    Y = shipAlignmentY
                };
                var XYDist = Vector3D.Distance(Vector3.Zero, XY);
                shipElevation = Math.Atan2(XYDist, shipAlignmentZ);
                shipAzimuth = shipAzimuth * (180.0 / Math.PI);
                shipAzimuth = (shipAzimuth + 360) % 360;
                shipElevation = shipElevation * (180.0 / Math.PI);
                shipElevation = ((shipElevation + 360) % 360)+ 90;
                //compass bearings done, time to do the roll indicator
                //only visible when in a ship cockpit
                //always aligned with the world matrix up axis
                var shipUp = shipController.WorldMatrix.Up;
                var shipRollAngle = Math.Atan2(shipUp.X, shipUp.Z);//outputs in radians     
                shipRollAngleFloat = Convert.ToSingle(shipRollAngle);
            }
            
            
            
            Draw(shipAzimuth, shipElevation, cameraAzimuth, cameraElevation, shipRollAngleFloat, inCockpit);
        }

        #endregion
        
        private void Draw(double shipAzimuth, double shipElevation, double cameraAzimuth, double cameraElevation, float rollAngle, bool inCockpit)
        {
            if (HudApi.Heartbeat)
            {
                HudCompassConfig config = HudCompassConfig.Instance;
                
                var shipAziInfo = new StringBuilder(Convert.ToInt32(shipAzimuth));
                var hudShipAzimuth = new HudAPIv2.HUDMessage(shipAziInfo, config.ShipAzimuth, null, 1, config.TextSize, true, true);
                var shipEleInfo = new StringBuilder(Convert.ToInt32(shipElevation));
                var hudShipElevation = new HudAPIv2.HUDMessage(shipEleInfo, config.ShipElevation, null, 1, config.TextSize, true, true);
                var cameraAziInfo = new StringBuilder(Convert.ToInt32(cameraAzimuth));
                var hudCameraAzimuth = new HudAPIv2.HUDMessage(cameraAziInfo, config.CameraAzimuth, null, 1, config.TextSize, true, true);
                var cameraEleInfo = new StringBuilder(Convert.ToInt32(cameraElevation));
                var hudCameraElevation = new HudAPIv2.HUDMessage(cameraEleInfo, config.CameraElevation, null, 1, config.TextSize, true, true);
        
                //var shipRollIndicatorTexture = MyStringId.GetOrCompute("RollIndicator");
                //var shipRollIndicator = new HudAPIv2.BillBoardHUDMessage(shipRollIndicatorTexture, Vector2D.Zero
                //    , Color.White, null, 1, 1D, 1F, 1F, rollAngle);
                        
                if (config.ShowCameraNumbers)
                {
                    hudCameraAzimuth.Visible = true;
                    hudCameraElevation.Visible = true;
                }
                //if (inCockpit)
                //{
                //    hudShipAzimuth.Visible = true;
                //    hudShipElevation.Visible = true;
                //    shipRollIndicator.Visible = true;
                //}
            }
        }
    }
}