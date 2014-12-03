using System;
using System.Configuration;
using System.Xml.Serialization;

namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class AvailablePacks
    {
        public class Pack
        {
            public string Id { get; set; }
            
            public string PackNumber { get; set; }

            public string PackComment { get; set; }

            public string GrossWeight { get; set; }

            public string TareWeight { get; set; }

            public string ScaleGrossWeight { get; set; }

            public string ScaleTareWeight { get; set; }

            public string PackId { get; set; }

            public string UnitOfMeasure { get; set; }

            public string DateCreated { get; set; }

            public string DateClosed { get; set; }

            public string Quantity { get; set; }

            public string CommodityType { get; set; }

            public string Status { get; set; }

            public string InventoryId { get; set; }

            public string AveragePrice { get; set; }

            public string DateModified { get; set; }
        }

    }

    public class PackInformation
    {
        public PackInformation()
        {
            Id = Guid.Empty;
            GrossWeight = 0m;
            TareWeight = 0m;
            ScaleGrossWeight = 0m;
            ScaleTareWeight = 0m;
            TagNumber = 0;
            PrintDescription = string.Empty;
            UnitOfMeasure = string.Empty;
            DateCreated = DateTime.Now;
            DateClosed = DateTime.Now;
            Quantity = 0;
            CommodityType = 0;
            PackStatus = 0;
            InventoryId = Guid.Empty;
            YardId = Guid.Empty;
            NumberPrefix = string.Empty;
            InternalPackNumber = string.Empty;
            Cost = 0m;
            CreatedByUserId = Guid.Empty;
            DateModified = DateTime.Now;
        }

        public Guid Id { get; set; }

        public decimal GrossWeight { get; set; }

        public decimal TareWeight { get; set; }

        public decimal NetWeight { get; set; }

        public decimal ScaleGrossWeight { get; set; }

        public decimal ScaleTareWeight { get; set; }

        public long TagNumber { get; set; }

        public string UnitOfMeasure { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime DateClosed { get; set; }

        public int Quantity { get; set; }

        public int CommodityType { get; set; }

        public int PackStatus { get; set; }

        public Guid InventoryId { get; set; }

        public string PrintDescription { get; set; }

        public Guid YardId { get; set; }

        public string NumberPrefix { get; set; }

        public string InternalPackNumber { get; set; }

        public decimal Cost { get; set; }

        public Guid CreatedByUserId { get; set; }

        public DateTime DateModified { get; set; }
    }

    [Serializable]
    public class PackReadInformation
    {
        [XmlElement("PackNumber")]
        public string TagNumber { get; set; }

        [XmlElement("PackComment")]
        public string PrintDescription { get; set; }

        [XmlElement("GrossWeight")]
        public string GrossWeight { get; set; }

        [XmlElement("TareWeight")]
        public string TareWeight { get; set; }

        [XmlElement("ScaleGrossWeight")]
        public string ScaleGrossWeight { get; set; }

        [XmlElement("ScaleTareWeight")]
        public string ScaleTareWeight { get; set; }

        [XmlElement("PackId")]
        public string Id { get; set; }
       
        [XmlElement("UnitOfMeasure")]
        public string UnitOfMeasure { get; set; }

        [XmlElement("DateCreated")]
        public string DateCreated { get; set; }

        [XmlElement("DateClosed")]
        public string DateClosed { get; set; }

        [XmlElement("Quantity")]
        public string Quantity { get; set; }

        [XmlElement("Status")]
        public string PackStatus { get; set; }     

        [XmlElement("InventoryId")]
        public string InventoryId { get; set; }       

        [XmlElement("AveragePrice")]
        public string AveragePrice { get; set; }

        [XmlElement("DateModified")]
        public string DateModified { get; set; }
    }

    [Serializable]
    [XmlRoot("Packs")]
    public class Packs
    {
        [XmlElement("Pack", typeof(PackReadInformation))]
        public PackReadInformation[] Pack { get; set; }
    }
}
