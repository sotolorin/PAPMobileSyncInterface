using System;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.PAP.Information
{
    public class AvailablePackLists
    {
        public class PackList
        {
            public string PackNumber { get; set; }

            public string ListName { get; set; }

            public string ContractId { get; set; }

            public string PackListId { get; set; }

            public PackIds[] PackIDs { get; set; }

            public class PackIds
            {
                public string Packid { get; set; }
            }

        }
    }

    public class PackListInformation
    {
        public PackListInformation()
        {
            Id = Guid.NewGuid();
            ContractHeadId = Guid.Empty;
            TransferSentHeadId = Guid.Empty;
            CreatedByUserId = Guid.Empty;
            PackListNumber = 0;
            DateCreated = DateTime.Now;
            SealNumber = string.Empty;
            Units = 0;
            PackListStatus = 0;
        }

        public Guid Id { get; set; }

        public Guid ContractHeadId { get; set; }

        public Guid TransferSentHeadId { get; set; }

        public Guid CreatedByUserId { get; set; }

        public int PackListNumber { get; set; }

        public DateTime DateCreated { get; set; }

        public string SealNumber { get; set; }

        public int Units { get; set; }

        public int PackListStatus { get; set; }
    }

    [Serializable]
    public class PackListReadInformation
    {
        [XmlElement("PackListId")]
        public string PackListId { get; set; }
        [XmlElement("ContractId")]
        public string ContractId { get; set; }
        [XmlElement("PackListNumber")]
        public string PackListNumber { get; set; }

        [XmlArray("PackIDs")]
        [XmlArrayItem("Packid", typeof(string))]
        public string[] PackCollection { get; set; }
    }

    [Serializable]
    [XmlRoot("Root")]
    public class PackLists
    {
        [XmlArray("PackLists")]
        [XmlArrayItem("PackList", typeof(PackListReadInformation))]
        public PackListReadInformation[] PackListCollection { get; set; }
    }
}
