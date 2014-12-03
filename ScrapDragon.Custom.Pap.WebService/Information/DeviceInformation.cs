using System;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class DeviceInformation
    {
        public class ScaleRawReading
        {
            public class Reading
            {
                public string Value { get; set; }

                public string Date { get; set; }
            }
        }

        public class DeviceProperty
        {
            public string Name { get; set; }

            public string Value { get; set; }
        }

        public string DeviceId { get; set; }
    }

    [Serializable]
    public class ScaleReadInformation
    {
        [XmlElement("Value")]
        public string Value { get; set; }

        [XmlElement("Date")]
        public string Date { get; set; }

    }

    [Serializable]
    public class DevicePropertyReadInformation
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Value")]
        public string Value { get; set; }

    }

    [Serializable]
    [XmlRoot("Root")]
    public class DeviceConfiguration
    {
        [XmlArray("ScaleRawReading")]
        [XmlArrayItem("Reading", typeof(ScaleReadInformation))]
        public ScaleReadInformation[] Reading { get; set; }

        [XmlArray("DeviceProperties")]
        [XmlArrayItem("DeviceProperty", typeof(DevicePropertyReadInformation))]
        public DevicePropertyReadInformation[] DevicePropertyCollection { get; set; }

        [XmlElement("DeviceID")]
        public string DeviceId { get; set; }
    }
}