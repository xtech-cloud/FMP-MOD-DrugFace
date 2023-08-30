
using System.Xml.Serialization;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
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
            [XmlAttribute("degree")]
            public string degree { get; set; } = "NORMAL";
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

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";

            [XmlElement("Camera")]
            public Camera camera { get; set; } = new Camera();
            [XmlElement("MergeMatrix")]
            public MergeMatrix mergeMatrix { get; set; } = new MergeMatrix();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

