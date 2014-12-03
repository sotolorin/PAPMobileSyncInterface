namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class Root
    {
        public string YardId { get; set; }
        public string UserId { get; set; }
        public DeviceConfiguration DeviceConfiguration { get; set; }
        public UpdateVin UpdatedVins { get; set; }
        public Packs Packs { get; set; }
        public PackLists PackLists { get; set; }
        public InventoryItems.Item[] InventoryItems { get; set; }
        //public AvailableContracts.Contract[] AvailableContracts { get; set; }
    }

    //public class RootRead
    //{
    //    public DeviceConfiguration DeviceConfiguration { get; set; }
    //    public UpdateVin UpdatedVins { get; set; }
    //    public Packs Packs { get; set; }
    //    public PackLists PackLists { get; set; }
    //    public InventoryItems InventoryItems { get; set; }
    //    public string YardId { get; set; }
    //    public string UserId { get; set; }
    //}
}
