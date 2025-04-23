using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;

namespace HudCompassMod
{
    public sealed partial class HudCompassSession
    {
        private void InitSettingsMenu()
        {
            RichHudTerminal.Root.Enabled = true;

            RichHudTerminal.Root.AddRange(new IModRootMember[]
            {
                new ControlPage()
                {
                    Name = "Settings",
                    CategoryContainer =
                    {
                        GetCameraSettings(),
                        GetShipSettings(),
                        GetShipTickerSettings(),
                    }
                },
                new ControlPage()
                {
                    Name = "Config",
                    CategoryContainer =
                    {
                        GetConfigSettings()
                    }
                }
                
            });
        }

        private ControlCategory GetCameraSettings()
        {
            Func<char, bool> numFilterFunc = x => (x >= '0' && x <= '9') || x == '.' || x == '-';
            
            var enableCamera = new TerminalOnOffButton()
            {
                Name = "Enable Camera",
                Value = Cfg.Camera.EnableCamera,
                CustomValueGetter = () => Cfg.Camera.EnableCamera,
                ControlChangedHandler = (sender, args) => Cfg.Camera.EnableCamera = ((TerminalOnOffButton)sender).Value,
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Enables/Disables displaying of current camera Elevation and Azimuth values."
                }
            };

            var cameraAziX = new TerminalTextField()
            {
                Name = "Camera Azimuth X",
                Value = Cfg.Camera.CameraAzi.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Camera.CameraAzi.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textField = sender as TerminalTextField;
                    double.TryParse(textField.Value, out Cfg.Camera.CameraAzi.X);
                    Cfg.Validate();
                },
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Sets the X value of the camera Azimuth Display."
                }
            };

            var cameraAziY = new TerminalTextField()
            {
                Name = "Camera Azimuth Y",
                Value = Cfg.Camera.CameraAzi.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Camera.CameraAzi.Y.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Camera.CameraAzi.Y);
                    Cfg.Validate();
                },
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Sets the Y value of the camera Azimuth Display."
                }
            };

            var tile = new ControlTile()
            {
                enableCamera
            };

            var tile1 = new ControlTile()
            {
                cameraAziX,
                cameraAziY
            };

            var cameraEleX = new TerminalTextField()
            {
                Name = "Camera Elevation X",
                Value = Cfg.Camera.CameraEle.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Camera.CameraEle.X.ToString(),
                ControlChangedHandler = (sender, arg) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Camera.CameraEle.X);
                    Cfg.Validate();
                },
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Sets the X value of the camera Elevation Display."
                }
            };

            var cameraEleY = new TerminalTextField()
            {
                Name = "Camera Elevation Y",
                Value = Cfg.Camera.CameraEle.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Camera.CameraEle.Y.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Camera.CameraEle.Y);
                    Cfg.Validate();
                }
            };
            var tile2 = new ControlTile()
            {
                cameraEleX,
                cameraEleY
            };

            return new ControlCategory()
            {
                HeaderText = "Camera Settings",
                SubheaderText = "Control where the camera Azimuth and elevation are displayed.",
                TileContainer = { tile, tile1, tile2 }
            };
        }

        private ControlCategory GetShipSettings()
        {
            Func<char, bool> numFilterFunc = x => (x >= '0' && x <= '9') || x == '.' || x == '-';
            
            var shipAziX = new TerminalTextField()
            {
                Name = "Ship Azimuth X",
                Value = Cfg.Ship.ShipAzi.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Ship.ShipAzi.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Ship.ShipAzi.X);
                    Cfg.Validate();
                }
            };

            var shipAziY = new TerminalTextField()
            {
                Name = "Ship Azimuth Y",
                Value = Cfg.Ship.ShipAzi.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Ship.ShipAzi.Y.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Ship.ShipAzi.Y);
                    Cfg.Validate();
                }
            };
            var tile1 = new ControlTile()
            {
                shipAziX,
                shipAziY
            };

            var shipEleX = new TerminalTextField()
            {
                Name = "Ship Elevation X",
                Value = Cfg.Ship.ShipEle.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Ship.ShipEle.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Ship.ShipEle.X);
                    Cfg.Validate();
                }
            };

            var shipEleY = new TerminalTextField()
            {
                Name = "Ship Elevation Y",
                Value = Cfg.Ship.ShipEle.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.Ship.ShipEle.Y.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.Ship.ShipEle.Y);
                    Cfg.Validate();
                }
            };
            var tile2 = new ControlTile()
            {
                shipEleX,
                shipEleY
            };

            return new ControlCategory()
            {
                HeaderText = "Ship Azimuth and elevation",
                SubheaderText = "Control where the ship Azimuth and elevation are displayed.",
                TileContainer = { tile1, tile2 }
            };
        }

        private ControlCategory GetShipTickerSettings()
        {
            Func<char, bool> numFilterFunc = x => (x >= '0' && x <= '9') || x == '.' || x == '-';

            var shipAziTickerX = new TerminalTextField()
            {
                Name = "Ship Azimuth Ticker X",
                Value = Cfg.ShipTicker.ShipAziTicker.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.ShipTicker.ShipAziTicker.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.ShipTicker.ShipAziTicker.X);
                    Cfg.Validate();
                }
            };

            var shipAziTickerY = new TerminalTextField()
            {
                Name = "Ship Azimuth Ticker Y",
                Value = Cfg.ShipTicker.ShipAziTicker.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.ShipTicker.ShipAziTicker.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.ShipTicker.ShipAziTicker.Y);
                    Cfg.Validate();
                }
            };

            var tile1 = new ControlTile()
            {
                shipAziTickerX,
                shipAziTickerY
            };

            var shipEleTickerX = new TerminalTextField()
            {
                Name = "Ship Elevation Ticker X",
                Value = Cfg.ShipTicker.ShipEleTicker.X.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.ShipTicker.ShipEleTicker.X.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.ShipTicker.ShipEleTicker.X);
                    Cfg.Validate();
                }
            };

            var shipEleTickerY = new TerminalTextField()
            {
                Name = "Ship Elevation Ticker Y",
                Value = Cfg.ShipTicker.ShipEleTicker.Y.ToString(),
                CharFilterFunc = numFilterFunc,
                CustomValueGetter = () => Cfg.ShipTicker.ShipEleTicker.Y.ToString(),
                ControlChangedHandler = (sender, args) =>
                {
                    var textfield = sender as TerminalTextField;
                    double.TryParse(textfield.Value, out Cfg.ShipTicker.ShipEleTicker.Y);
                    Cfg.Validate();
                }
            };
            
            var tile2 = new ControlTile()
            {
                shipEleTickerX,
                shipEleTickerY
            };

            return new ControlCategory()
            {
                HeaderText = "Ship Azimuth and Elevation Ticker",
                SubheaderText = "Control where the ship Azimuth and Elevation Tickers are displayed.",
                TileContainer = { tile1, tile2 }
            };
        }

        private ControlCategory GetConfigSettings()
        {
            var loadCfg = new TerminalButton()
            {
                Name = "Load Config",
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Loads the current config from the config file."
                },
                ControlChangedHandler = (sender, args) => HcConfig.LoadStart()
            };

            var saveCfg = new TerminalButton()
            {
                Name = "Save Config",
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Saves the current config to the config file."
                },
                ControlChangedHandler = (sender, args) => HcConfig.SaveStart()
            };

            var resetCfg = new TerminalButton()
            {
                Name = "Reset Config",
                ToolTip = new RichText(ToolTip.DefaultText)
                {
                    "Resets the current config to the default values."
                },
                ControlChangedHandler = (sender, args) => HcConfig.ResetConfig()
            };
            
            var tile1 = new ControlTile()
            {
                loadCfg,
                saveCfg,
                resetCfg
            };

            return new ControlCategory()
            {
                HeaderText = "Config",
                SubheaderText = "Save, Load and Reset the Config",
                TileContainer = { tile1 }
            };
        }
    }
}