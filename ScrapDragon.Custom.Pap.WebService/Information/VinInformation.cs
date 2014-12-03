using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class VinInformation
    {
        public string MasterRowNum { get; set; }

        public class Vin
        {
            public string VinId { get; set; }

            public string RowNum { get; set; }

            public string Packid { get; set; }

            public string UpdateDate { get; set; }
        }
    }

    [Serializable]
    public class VinReadInformation
    {
        [XmlElement("VinId")]
        public string VinId { get; set; }

        [XmlElement("RowNum")]
        public string RowNum { get; set; }

        [XmlElement("Packid")]
        public string Packid { get; set; }

        [XmlElement("UpdateDate")]
        public string UpdateDate { get; set; }

    }

    [Serializable]
    [XmlRoot("Root")]
    public class UpdateVin
    {
        [XmlElement("MasterRowNum")]
        public string MasterRowNum { get; set; }

        [XmlArray("Vins")]
        [XmlArrayItem("Vin", typeof(VinReadInformation))]
        public VinReadInformation[] VinCollection { get; set; }
    }
}