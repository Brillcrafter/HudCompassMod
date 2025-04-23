using RichHudFramework.IO;
using System.Xml.Serialization;
using VRageMath;

namespace HudCompassMod
{
    [XmlRoot, XmlType(TypeName = "FlightHudSettings")]
    public class HcConfig : ConfigRoot<HcConfig>
    {
        [XmlElement(ElementName = "CameraSettings")]
        public CameraConfig Camera;
        
        [XmlElement(ElementName = "ShipSettings")]
        public ShipConfig Ship;

        [XmlElement(ElementName = "ShipTickerSettings")]
        public ShipTickerConfig ShipTicker;

        protected override HcConfig GetDefaults()
        {
            return new HcConfig
            {
                Camera = CameraConfig.Defaults,
                Ship = ShipConfig.Defaults,
                ShipTicker = ShipTickerConfig.Defaults
            };
        }
    }

    public class CameraConfig : Config<CameraConfig>
    {
        [XmlElement(ElementName = "EnableCamera")]
        public bool EnableCamera;
        
        [XmlElement(ElementName = "CameraAzi")]
        public Vector2D CameraAzi;
        
        [XmlElement(ElementName = "CameraEle")]
        public Vector2D CameraEle;

        protected override CameraConfig GetDefaults()
        {
            return new CameraConfig
            {
                EnableCamera = true,
                CameraAzi = new Vector2D(0, 0.91),
                CameraEle = new Vector2D(-0.91, 0)
            };
        }

        public override void Validate()
        {
            CameraAzi = Vector2D.Clamp(CameraAzi, new Vector2D(-1, -1), new Vector2D(1, 1));
            CameraEle = Vector2D.Clamp(CameraEle, new Vector2D(-1, -1), new Vector2D(1, 1));
        }
    }
    
    public class ShipConfig : Config<ShipConfig>
    {
        [XmlElement(ElementName = "ShipAzi")]
        public Vector2D ShipAzi;
        
        [XmlElement(ElementName = "ShipEle")]
        public Vector2D ShipEle;
        
        protected override ShipConfig GetDefaults()
        {
            return new ShipConfig
            {
                ShipAzi = new Vector2D(0, 0.95),
                ShipEle = new Vector2D(-0.95, 0),
            };
        }

        public override void Validate()
        {
            ShipAzi = Vector2D.Clamp(ShipAzi, new Vector2D(-1, -1), new Vector2D(1, 1));
            ShipEle = Vector2D.Clamp(ShipEle, new Vector2D(-1, -1), new Vector2D(1, 1));
        }
    }
    
    public class ShipTickerConfig : Config<ShipTickerConfig>
    {
        [XmlElement(ElementName = "ShipAziTicker")]
        public Vector2D ShipAziTicker;
        
        [XmlElement(ElementName = "ShipEleTicker")]
        public Vector2D ShipEleTicker;

        protected override ShipTickerConfig GetDefaults()
        {
            return new ShipTickerConfig
            {
                ShipAziTicker = new Vector2D(0, 0.99),
                ShipEleTicker = new Vector2D(-0.99, 0)
            };
        }

        public override void Validate()
        {
            ShipAziTicker = Vector2D.Clamp(ShipAziTicker, new Vector2D(-1, -1), new Vector2D(1, 1));
            ShipEleTicker = Vector2D.Clamp(ShipEleTicker, new Vector2D(-1, -1), new Vector2D(1, 1));
        }
    }
}