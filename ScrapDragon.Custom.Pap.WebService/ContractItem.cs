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
    
    public partial class ContractItem
    {
        public ContractItem()
        {
            this.PackListItems = new HashSet<PackListItem>();
        }
    
        public System.Guid Id { get; set; }
        public System.Guid ContractHeadId { get; set; }
        public System.Guid InventoryId { get; set; }
        public System.Guid UnitOfMeasureId { get; set; }
        public int ContractItemStatus { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal ExtendedAmount { get; set; }
        public decimal OrderWeight { get; set; }
        public System.DateTime DateCreated { get; set; }
        public int Sequence { get; set; }
        public Nullable<int> ExpectedShipments { get; set; }
    
        public virtual ContractHead ContractHead { get; set; }
        public virtual Inventory Inventory { get; set; }
        public virtual ICollection<PackListItem> PackListItems { get; set; }
    }
}
