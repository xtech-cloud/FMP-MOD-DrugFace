
using System.Xml.Serialization;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

