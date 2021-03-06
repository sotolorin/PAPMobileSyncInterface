//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ScrapDragon.Custom.Pap.WebService
{
    using System;
    using System.Collections.Generic;
    
    public partial class ContractHead
    {
        public ContractHead()
        {
            this.ContractItems = new HashSet<ContractItem>();
            this.PackListHeads = new HashSet<PackListHead>();
        }
    
        public System.Guid Id { get; set; }
        public System.Guid BillToId { get; set; }
        public System.Guid ShipToId { get; set; }
        public System.Guid PaymentTermsId { get; set; }
        public System.Guid YardId { get; set; }
        public System.Guid CreatedByUserId { get; set; }
        public System.Guid UnitOfMeasureId { get; set; }
        public Nullable<System.Guid> InvoiceHeadId { get; set; }
        public Nullable<System.Guid> CarrierId { get; set; }
        public Nullable<System.Guid> TraderUdlvId { get; set; }
        public Nullable<System.Guid> FreightOnBoardUdlvId { get; set; }
        public Nullable<System.Guid> BillOfLadingNameUdlvId { get; set; }
        public Nullable<System.Guid> FreightTermsUdlvId { get; set; }
        public Nullable<System.Guid> ShipViaUdlvId { get; set; }
        public System.Guid ContractTypeBasedOnCommodityUdlvId { get; set; }
        public System.DateTime DateCreated { get; set; }
        public Nullable<System.DateTime> BeginDate { get; set; }
        public Nullable<System.DateTime> EndDate { get; set; }
        public int ContractStatus { get; set; }
        public bool IsFinished { get; set; }
        public long ContractNumber { get; set; }
        public string ShipToOrderNumber { get; set; }
        public string ShipToContact { get; set; }
        public string ShipToCarrier { get; set; }
        public string BillToOrderNumber { get; set; }
        public string BillToContact { get; set; }
        public bool InvoiceOnMillOn { get; set; }
        public bool BillToIsRelatedCompany { get; set; }
        public bool BillToExportShipmentsOn { get; set; }
        public bool IsBillable { get; set; }
        public decimal TotalOrderWeight { get; set; }
        public string UnitOfMeasure { get; set; }
        public string ContractDescription { get; set; }
        public bool InvoicePrinted { get; set; }
        public string BillOfLadingName { get; set; }
        public Nullable<System.DateTime> VoidDate { get; set; }
        public Nullable<System.Guid> VoidedByUserId { get; set; }
        public bool AllowAddMaterials { get; set; }
        public int InventoryTagPrintOption { get; set; }
        public Nullable<System.Guid> ShipToAddressId { get; set; }
        public Nullable<System.Guid> BillToAddressId { get; set; }
        public string BookingNumber { get; set; }
        public bool EmailAtClose { get; set; }
        public decimal MaxShipmentWeight { get; set; }
        public decimal MinShipmentWeight { get; set; }
        public bool AutoCreateFreight { get; set; }
    
        public virtual Customer Customer { get; set; }
        public virtual Customer Customer1 { get; set; }
        public virtual Customer Customer2 { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue1 { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue2 { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue3 { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue4 { get; set; }
        public virtual UserDefinedListValue UserDefinedListValue5 { get; set; }
        public virtual Yard Yard { get; set; }
        public virtual ICollection<ContractItem> ContractItems { get; set; }
        public virtual ICollection<PackListHead> PackListHeads { get; set; }
    }
}
