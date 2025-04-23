using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Draygo.API;
using Sandbox.ModAPI;
using SeamlessClient.Messages;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using RichHudFramework.Client;
using RichHudFramework.Internal;
using RichHudFramework.IO;

namespace HudCompassMod
{
    
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public sealed partial class HudCompassSession : ModBase
    {
        private const string ModName = "Flight HUD";
        private const ushort SeamlessClientNetId = 2936;
        private static HudCompassSession Instance { get; set; }
        private HudAPIv2 _hudApi;
        private bool _firstDraw;
        private const double TickerScale = 1D;
        private bool _registeredController;
        private IMyShipController _controllableEntity;
        private static HcConfig Cfg => HcConfig.Current;
        
        
        #region HUD Variables
        
        private readonly List<CompassDivisionClass> _headingCompassClass = new List<CompassDivisionClass>();
        private readonly List<CompassDivisionClass> _elevationCompassClass = new List<CompassDivisionClass>();
        private HudAPIv2.HUDMessage _hudShipAzimuth;
        private HudAPIv2.HUDMessage _hudShipElevation;
        private HudAPIv2.HUDMessage _hudCameraAzimuth;
        private HudAPIv2.HUDMessage _hudCameraElevation;
        private HudAPIv2.BillBoardHUDMessage _shipRollIndicator;
        
        #endregion
        #region Session Overrides
        
        
        public HudCompassSession() : base(false,true)
        {
            if (Instance == null)
                Instance = this;
            else
                throw new Exception("Only one instance of HudCompassSession can exist at any given time.");

            LogIO.FileName = "HudCompass.txt";
            HcConfig.FileName = "HudCompassConfig.xml";

            ExceptionHandler.ModName = ModName;
            ExceptionHandler.PromptForReload = true;
            ExceptionHandler.RecoveryLimit = 3;
        }
        
        public override void BeforeStart()
        {
            if (Tools.IsDedicatedServer)
                return;
            _hudApi = new HudAPIv2();
            if (_hudApi == null)
                MyAPIGateway.Utilities.ShowMessage("Flight HUD", "TextHudAPI failed to register");
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(SeamlessClientNetId, MessageHandler);
        }

        protected override void AfterInit()
        {
            if (!ExceptionHandler.IsClient) return;
            CanUpdate = false;
            RichHudClient.Init(ModName, HudInit, ClientReset);
        }

        public override void BeforeClose()
        {
            HcConfig.Save();
            if (!ExceptionHandler.Unloading) return;
            if (_registeredController)
            {
                MyAPIGateway.Session.Player.Controller.ControlledEntityChanged -= GridChange;
                _registeredController = false;
            }
            _hudApi?.Unload();
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(SeamlessClientNetId, MessageHandler);
            Instance = null;
        }
        
        private void MessageHandler(ushort packetID, byte[] data, ulong sender, bool fromServer)
        {
            if (!fromServer || sender == 0)
                return;

            var msg = MyAPIGateway.Utilities.SerializeFromBinary<ClientMessage>(data);
            if (msg == null)
                return;

            if (msg.MessageType != ClientMessageType.FirstJoin) return;
            _registeredController = false;
            MyLog.Default.WriteLine(ModName + " Seamless message - First Join");
        }

        private void HudInit()
        {
            CanUpdate = true;
            HcConfig.Load();
            InitSettingsMenu();
        }

        private void ClientReset() {}

        public override void Draw()//yeet all the stuff here
        {
            if (!_registeredController && MyAPIGateway.Session?.Player?.Controller != null)
            {
                _registeredController = true;
                MyAPIGateway.Session.Player.Controller.ControlledEntityChanged += GridChange;
                MyLog.Default.WriteLine(ModName + " Registered Client Grid Change");
                GridChange(null, MyAPIGateway.Session.Player.Controller.ControlledEntity);
            }
            var inCockpit = false;
            if (Tools.IsDedicatedServer)
                return;
            var shipAzimuth = 0d;
            var shipElevation = 0d;
            var rollFloat = 0f;
            var cameraViewMatrix = MatrixD.Zero;
            if (MyAPIGateway.Session != null)
            {
                cameraViewMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            }
            var cameraForward = cameraViewMatrix.Forward;
            //camera orientation
            var cameraAzimuth = Math.Atan2(cameraForward.X, cameraForward.Y) * (180.0 / Math.PI);
            var cameraElevation = Math.Asin(cameraForward.Z) * (180.0 / Math.PI);
            cameraAzimuth = (cameraAzimuth + 360) % 360;
            cameraElevation = MathHelperD.Clamp(cameraElevation, -90, 90);
            
            if (_controllableEntity != null)
            {
                inCockpit = true;
                var shipForward = _controllableEntity.WorldMatrix.Forward.Normalized();
                rollFloat = CalculateRoll(shipForward, _controllableEntity.WorldMatrix.Up, _controllableEntity.WorldMatrix.Right);
                shipAzimuth = Math.Atan2(shipForward.X, shipForward.Y) * (180.0 / Math.PI);
                shipElevation = Math.Asin(shipForward.Z) * (180.0 / Math.PI);
                shipAzimuth = (shipAzimuth + 360) % 360;
                shipElevation = MathHelperD.Clamp(shipElevation, -90, 90);
            }
            DrawMessages(shipAzimuth, shipElevation, cameraAzimuth, cameraElevation, rollFloat , inCockpit);
            DrawHeadingTicker(shipAzimuth, inCockpit);
            DrawElevationTicker(shipElevation, inCockpit);
        }

        #endregion
        
        private void DrawMessages(double shipAzimuth, double shipElevation, double cameraAzimuth, double cameraElevation,
            float rollAngle, bool inCockpit)
        {
            if (_hudApi.Heartbeat)
            {
                if (!_firstDraw)
                {
                    CreateHudMessageOffsets();
                    var shipAziInfo = new StringBuilder(Convert.ToInt32(shipAzimuth).ToString());
                    _hudShipAzimuth = new HudAPIv2.HUDMessage(shipAziInfo, Cfg.Ship.ShipAzi, null,
                        -1,1f, true, true);
                    var shipEleInfo = new StringBuilder(Convert.ToInt32(shipElevation).ToString());
                    _hudShipElevation = new HudAPIv2.HUDMessage(shipEleInfo, Cfg.Ship.ShipEle, null,
                        -1,1f, true, true);
                    var cameraAziInfo = new StringBuilder(Convert.ToInt32(cameraAzimuth).ToString());
                    _hudCameraAzimuth = new HudAPIv2.HUDMessage(cameraAziInfo, Cfg.Camera.CameraAzi, null,
                        -1, 1f, true, true);
                    var cameraEleInfo = new StringBuilder(Convert.ToInt32(cameraElevation).ToString());
                    _hudCameraElevation = new HudAPIv2.HUDMessage(cameraEleInfo, Cfg.Camera.CameraEle, null,
                        -1, 1f, true, true);
        
                    var shipRollIndicatorTexture = MyStringId.GetOrCompute("RollIndicator");
                    _shipRollIndicator = new HudAPIv2.BillBoardHUDMessage(shipRollIndicatorTexture, Vector2D.Zero
                        , Color.White, null, -1, 0.15D, 1F, 1F, rollAngle);
                    _firstDraw = true;
                }
                else
                {
                    _hudShipAzimuth.Message = new StringBuilder(Convert.ToInt32(shipAzimuth).ToString());
                    _hudShipAzimuth.Origin = Cfg.Ship.ShipAzi;
                    _hudShipElevation.Message = new StringBuilder(Convert.ToInt32(shipElevation).ToString());
                    _hudShipElevation.Origin = Cfg.Ship.ShipEle;
                    _hudCameraAzimuth.Message = new StringBuilder(Convert.ToInt32(cameraAzimuth).ToString());
                    _hudCameraAzimuth.Origin = Cfg.Camera.CameraAzi;
                    _hudCameraElevation.Message = new StringBuilder(Convert.ToInt32(cameraElevation).ToString());
                    _hudCameraElevation.Origin = Cfg.Camera.CameraEle;
                    _shipRollIndicator.Rotation = rollAngle;
                }
                if (Cfg.Camera.EnableCamera)
                {
                    _hudCameraAzimuth.Visible = true;
                    _hudCameraElevation.Visible = true;
                }
                else
                {
                    _hudCameraAzimuth.Visible = false;
                    _hudCameraElevation.Visible = false;
                }

                if (inCockpit)
                {
                    _hudShipAzimuth.Visible = true;
                    _hudShipElevation.Visible = true;
                    _shipRollIndicator.Visible = true;
                }
                else
                {
                    _hudShipAzimuth.Visible = false;
                    _hudShipElevation.Visible = false;
                    _shipRollIndicator.Visible = false;
                }
            }
            else
            {
                MyLog.Default.Error($"Flight HUD: No heartbeat and {inCockpit}");
            }
        }

        private float CalculateRoll(Vector3D Forward, Vector3D Up, Vector3D Right)
        {
            var worldUp = new Vector3D(0, 0, 1);
            var shipUp = Vector3D.Normalize(Up);
            var shipRight = Vector3D.Normalize(Right);
            var shipForward = Vector3D.Normalize(Forward);
            var projectedWorldUp = worldUp - Vector3D.Dot(worldUp, shipForward) * shipForward;
            if (projectedWorldUp.LengthSquared() < 1e-6f)
            {
                return 0f;
            }
            projectedWorldUp = Vector3D.Normalize(projectedWorldUp);
            var cosTheta = Vector3D.Dot(shipUp, projectedWorldUp);
            var sinTheta = Vector3D.Dot(shipRight, projectedWorldUp);
            var rollAngle = (float)Math.Atan2(sinTheta, cosTheta);
            return rollAngle;
        }
        
        private void DrawHeadingTicker(double shipAzimuth, bool inCockpit)
        {
            if (!inCockpit)
            {
                foreach (var division in _headingCompassClass)
                {
                    division.Division.Message = new StringBuilder(1);
                }
                return;
            }
            var tickerOffset = shipAzimuth * 0.01;
            foreach (var division in _headingCompassClass)
            {
                if (tickerOffset - division.Offset < -0.45D || tickerOffset - division.Offset > 0.45D)
                    division.Division.Message = new StringBuilder(1);
                else
                    division.Division.Message = new StringBuilder(division.Character);
                division.Division.Offset = new Vector2D(division.Offset - tickerOffset , division.Division.Offset.Y);
                division.Division.Origin = Cfg.ShipTicker.ShipAziTicker;
            }
        }

        private void DrawElevationTicker(double shipElevation, bool inCockpit)
        {
            if (!inCockpit)
            {
                foreach (var division in _elevationCompassClass)
                {
                    division.Division.Message = new StringBuilder(1);
                }
                return;
            }
            var tickerOffset = shipElevation * 0.01;
            foreach (var division in _elevationCompassClass)
            {
                if (tickerOffset - division.Offset < -0.45D || tickerOffset - division.Offset > 0.45D)
                    division.Division.Message = new StringBuilder(1);
                else
                    division.Division.Message = new StringBuilder(division.Character);
                division.Division.Offset = new Vector2D(division.Division.Offset.X, division.Offset - tickerOffset);
                division.Division.Origin = Cfg.ShipTicker.ShipEleTicker;
            }
        }

        private void CreateHudMessageOffsets()
        {
            //for heading ticker
            for (var i = -45; i < 405; i += 5)
            {
                //starting from the left
                if(i == 0)//these are for the cardinal directions
                    _headingCompassClass.Add(new CompassDivisionClass(true,"N", (double)i/100 
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100 , 0D),
                        -1,TickerScale)));
                else if(i == 90)
                    _headingCompassClass.Add(new CompassDivisionClass(true, "E", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else if(i == 180)
                    _headingCompassClass.Add(new CompassDivisionClass(true, "S", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else if (i == 270)
                    _headingCompassClass.Add(new CompassDivisionClass(true, "W", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else if (i == 360)
                    _headingCompassClass.Add(new CompassDivisionClass(true, "N", (double)i/100
                        , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else if (i < 0)
                    _headingCompassClass.Add(new CompassDivisionClass(true,(360 - i * -1).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else if (i > 360)
                    _headingCompassClass.Add(new CompassDivisionClass(true,(i - 360).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                        -1,TickerScale)));
                else 
                    _headingCompassClass.Add(new CompassDivisionClass(true,i.ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipAziTicker, new Vector2D((double)i/100, 0D),
                    -1,TickerScale)));
            }
            // for elevation ticker
            for (var i = -135; i < 135; i += 5)
            {
                if (i < -90)
                    _elevationCompassClass.Add(new CompassDivisionClass(true,(90 - i * -1).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                        Cfg.ShipTicker.ShipEleTicker, new Vector2D(0D, (double)i/100),
                        -1,TickerScale)));
                else if (i > 90)
                    _elevationCompassClass.Add(new CompassDivisionClass(true,(i - 90).ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipEleTicker, new Vector2D(0D, (double)i/100),
                        -1,TickerScale)));
                else 
                    _elevationCompassClass.Add(new CompassDivisionClass(true,i.ToString(),
                        (double)i/100 , new HudAPIv2.HUDMessage(new StringBuilder(1),
                            Cfg.ShipTicker.ShipEleTicker, new Vector2D(0D, (double)i/100),
                    -1,TickerScale)));
            }
        }
        private void GridChange(VRage.Game.ModAPI.Interfaces.IMyControllableEntity previousEnt,
            VRage.Game.ModAPI.Interfaces.IMyControllableEntity newEnt)
        {
            if (newEnt is IMyCharacter)
            {
                _controllableEntity = null;
            }
            else if (newEnt is IMyShipController)
            {
                _controllableEntity = (IMyShipController)newEnt;
            }
        }
    }
}