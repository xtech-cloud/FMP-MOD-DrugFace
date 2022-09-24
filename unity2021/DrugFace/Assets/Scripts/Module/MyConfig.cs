
using System.Xml.Serialization;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Camera
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 800;

            [XmlAttribute("height")]
            public int height { get; set; } = 600;

            [XmlAttribute("fps")]
            public int fps { get; set; } = 30;
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";

            [XmlElement("Camera")]
            public Camera camera { get; set; } = new Camera();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

