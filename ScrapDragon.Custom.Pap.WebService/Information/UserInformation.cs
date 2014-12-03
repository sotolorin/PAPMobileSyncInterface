using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.Pap.WebService.Information
{
    [Serializable]
    public class DataReadInformation
    {
        [XmlElement("YardId")]
        public string YardId { get; set; }

        [XmlElement("UserId")]
        public string UserId { get; set; }

    }
}