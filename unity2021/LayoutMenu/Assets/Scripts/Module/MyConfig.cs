
using System.Xml.Serialization;
using UnityEngine;

namespace XTC.FMP.MOD.LayoutMenu.LIB.Unity
{
    /// <summary>
    /// 配置类
    /// </summary>
    public class MyConfig : MyConfigBase
    {
        public class Border
        {
            [XmlAttribute("left")]
            public int left { get; set; } = 0;
            [XmlAttribute("right")]
            public int right { get; set; } = 0;
            [XmlAttribute("top")]
            public int top { get; set; } = 0;
            [XmlAttribute("bottom")]
            public int bottom { get; set; } = 0;
        }

        public class Offset2
        {
            [XmlAttribute("x")]
            public int x { get; set; } = 0;
            [XmlAttribute("y")]
            public int y { get; set; } = 0;
        }

        public class Label
        {
            [XmlAttribute("fontSize")]
            public int fontSize { get; set; } = 12;
            [XmlAttribute("offset")]
            public int offset { get; set; } = 24;
            [XmlAttribute("width")]
            public int width { get; set; } = 100;
            [XmlAttribute("height")]
            public int height { get; set; } = 32;
            [XmlAttribute("color")]
            public string color { get; set; } = "#000000FF";
        }

        public class Cell
        {
            [XmlAttribute("width")]
            public int width { get; set; } = 0;
            [XmlAttribute("height")]
            public int height { get; set; } = 0;
            [XmlElement("Label")]
            public Label label { get; set; } = new Label();
        }

        public class GridLayout
        {
            [XmlAttribute("column")]
            public int column { get; set; } = 0;
            [XmlAttribute("row")]
            public int row { get; set; } = 0;
            [XmlElement("Anchor")]
            public Anchor anchor { get; set; } = new Anchor();
            [XmlElement("Padding")]
            public Border padding { get; set; } = new Border();
            [XmlElement("Spacing")]
            public Offset2 spacing { get; set; } = new Offset2();
        }

        public class Style
        {
            [XmlAttribute("name")]
            public string name { get; set; } = "";
            [XmlAttribute("layout")]
            public string layout { get; set; } = "";
            [XmlElement("GridLayout")]
            public GridLayout gridLayout { get; set; } = new GridLayout();
            [XmlElement("Cell")]
            public Cell cell { get; set; } = new Cell();
        }


        [XmlArray("Styles"), XmlArrayItem("Style")]
        public Style[] styles { get; set; } = new Style[0];
    }
}

