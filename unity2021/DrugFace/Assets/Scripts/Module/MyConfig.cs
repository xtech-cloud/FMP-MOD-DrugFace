
using System.Xml.Serialization;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Debug
        {
            [XmlAttribute("useDebugPhoto")]
            public bool useDebugPhoto { get; set; } = false;
            [XmlAttribute("saveResultImage")]
            public bool saveResultImage { get; set; } = false;
        }

        public class Image
        {
            [XmlAttribute("file")]
            public string file { get; set; } = "";
        }

        public class Row
        {
            [XmlArray("FemaleImageS"), XmlArrayItem("Image")]
            public Image[] femaleImageS { get; set; } = new Image[0];
            [XmlArray("MaleImageS"), XmlArrayItem("Image")]
            public Image[] maleImageS { get; set; } = new Image[0];
        }

        public class MergeMatrix
        {
            [XmlAttribute("row")]
            public int row { get; set; } = 0;
            [XmlAttribute("column")]
            public int column { get; set; } = 0;
            [XmlAttribute("version")]
            public string version { get; set; } = "1.0";
            [XmlAttribute("degree")]
            public string degree { get; set; } = "NORMAL";
            [XmlAttribute("templateQuality")]
            public string templateQuality { get; set; } = "NONE";
            [XmlAttribute("targetQuality")]
            public string targetQuality { get; set; } = "NONE";
            [XmlArray("RowS"), XmlArrayItem("Row")]
            public Row[] rowS { get; set; } = new Row[0];
        }

        public class Camera
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 800;

            [XmlAttribute("height")]
            public int height { get; set; } = 600;

            [XmlAttribute("fps")]
            public int fps { get; set; } = 30;
        }

        public class IdleTimer
        {
            [XmlAttribute("timeout")]
            public int timeout { get; set; } = 30;
            [XmlAttribute("appear")]
            public int appear { get; set; } = 20;
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";

            [XmlElement("Debug")]
            public Debug debug { get; set; } = new Debug();

            [XmlElement("Camera")]
            public Camera camera { get; set; } = new Camera();
            [XmlElement("MergeMatrix")]
            public MergeMatrix mergeMatrix { get; set; } = new MergeMatrix();
            [XmlElement("IdelTimer")]
            public IdleTimer idleTimer { get; set; } = new IdleTimer();
        }



        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

