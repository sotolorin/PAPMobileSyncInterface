namespace ScrapDragon.Custom.Pap.WebService.Information
{
    public class AvailableContracts
    {
        public class Contract
        {
            public string ContractId { get; set; }

            public string CustomerName { get; set; }

            public string ContractDescription { get; set; }

            public string ContractNumber { get; set; }

            public string IsTransfer { get; set; }

            public Inventory[] InventoryItems { get; set; }

            public class Inventory
            {
                public string InventoryId { get; set; }
            }

        }
    }


}
