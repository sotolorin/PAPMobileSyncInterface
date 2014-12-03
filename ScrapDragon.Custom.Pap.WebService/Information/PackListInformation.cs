using System;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class AvailablePackLists
    {
        public class PackList
        {
            public string CustomerName { get; set; }

            public string Description { get; set; }

            public string IsTransfer { get; set; }

            public string PackNumber { get; set; }

            public string ListName { get; set; }

            //public string ContractId { get; set; }

            //Contract Id on send, new Guid on receive
            public string PackListId { get; set; }

            public string SavedPackListId { get; set; }

            public PackIds[] PackIDs { get; set; }

            public InventoryIds[] InventoryIDs { get; set; }

            public class PackIds
            {
                public string Packid { get; set; }
            }

            public class InventoryIds
            {
                public string Inventoryid { get; set; }
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

        public long PackListNumber { get; set; }

        public DateTime DateCreated { get; set; }

        public string SealNumber { get; set; }

        public int Units { get; set; }

        public int PackListStatus { get; set; }

        public Guid[] Packs { get; set; }
    }

    [Serializable]
    public class PackListReadInformation
    {
        [XmlElement("PackListId")]
        public string PackListId { get; set; }
        //[XmlElement("ContractId")]
        //public string ContractId { get; set; }
        [XmlElement("PackListNumber")]
        public string PackListNumber { get; set; }
        [XmlElement("CustomerName")]
        public string CustomerName { get; set; }
        [XmlElement("Description")]
        public string Description { get; set; }
        [XmlElement("IsTransfer")]
        public string IsTransfer { get; set; }
        [XmlElement("SavedPackListId")]
        public string SavedPackListId { get; set; }

        [XmlArray("PackIDs")]
        [XmlArrayItem("PackId", typeof(string))]
        public string[] PackCollection { get; set; }

        [XmlArray("InventoryItems")]
        [XmlArrayItem("InventoryId", typeof(string))]
        public string[] InventoryCollection { get; set; }
    }

    [Serializable]
    [XmlRoot("PackLists")]
    public class PackLists
    {
        [XmlElement("PackList", typeof(PackListReadInformation))]
        public PackListReadInformation[] PackListCollection { get; set; }
    }
}
