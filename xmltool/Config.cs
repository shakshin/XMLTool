using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace xmlview
{
    public class Config
    {
        private double _FontSize;

        public double FontSize
        {
            get
            {
                return _FontSize;
            }

            set
            {
                _FontSize = value;
            }
        }

        public double EditorHeight
        {
            get
            {
                return _EditorHeight;
            }

            set
            {
                _EditorHeight = value;
            }
        }

        private double _EditorHeight;

        public Config()
        {
            this.FontSize = 12;
            this.EditorHeight = 0;
        }

        public static Config LoadFromFile()
        {
            string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xmlview-configuration.xml");
            try
            {
                if (File.Exists(file) && (new FileInfo(file)).Length > 0)
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Config));
                    FileStream stream = new FileStream(file, FileMode.Open);
                    Config inst = new Config();
                    inst = (Config)xml.Deserialize(stream);
                    stream.Close();

                    inst.SaveToFile();

                    return inst;
                }
                return new Config(); ;
            }
            catch (Exception)
            {
                return new Config();
            }
        }

        public void SaveToFile()
        {

            XmlSerializer xml = new XmlSerializer(typeof(Config));
            StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "xmlview-configuration.xml"));
            xml.Serialize(stream, this);
            stream.Close();
        }

    }
}
