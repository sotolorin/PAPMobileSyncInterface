using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CODE.Framework.Core.Utilities;
using ScrapDragon.Custom.PAP.Information;
using ScrapDragon.Custom.PAP.RnR;
using ScrapDragon.Service.Contracts.BackOffice;

namespace ScrapDragon.Custom.PAP
{
    public class PAPService : IPAPService
    {
        public static Guid YardId = Guid.Parse("1612C2EA-4891-4F5A-84F6-B8C5F73CEB7C");

        public string TestConnection(string name)
        {
            return "You Sent: " + name + "!";
        }

        public SendPapDataResponse SendPapData(SendPapDataRequest request)
        {
            try
            {
                var response = new SendPapDataResponse();
                var xmlresponse = WritePapXml();
                response.FailureInformation = xmlresponse.FailureInformation;
                if (!xmlresponse.Success) return response;
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("SendPapData");
                LoggingMediator.Log(ex);
                return new SendPapDataResponse {Success = false, FailureInformation = "Error sending PAP data."};
            }
        }

        public ReceivePapDataResponse ReceivePapData(ReceivePapDataRequest request)
        {
            try
            {
                var response = new ReceivePapDataResponse();
                var xmlresponse = ReadPapXml();
                response.FailureInformation = xmlresponse.FailureInformation;
                if (!xmlresponse.Success) return response;
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("ReceivePapData");
                LoggingMediator.Log(ex);
                return new ReceivePapDataResponse
                       {
                           Success = false,
                           FailureInformation = "Error in PAPService:ReceivePapData"
                       };
            }
        }

        private static GenericResponse UpdatePack(PackInformation pkinfo)
        {
            try
            {
                var response = new GenericResponse();
                using (var context = new Entities())
                {
                    var yd = (from yard in context.Yards
                        where yard.Id == pkinfo.YardId
                        select yard.CustomerNumberPrefix).First() ?? string.Empty;
                    pkinfo.NumberPrefix = yd;
                    pkinfo.InternalPackNumber = yd.Trim() + pkinfo.TagNumber;

                    var pack = (from pk in context.Packs
                        where pk.Id == pkinfo.Id
                        select pk).FirstOrDefault();
                    if (pack == null)
                    {
                        var pk = new Pack();
                        Mapper.Map(pkinfo, pk);
                        context.Packs.AddObject(pk);
                    }
                    else Mapper.Map(pkinfo, pack);
                    context.SaveChanges();
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdatePack");
                LoggingMediator.Log(ex);
                return new GenericResponse {Success = false, FailureInformation = "Error in PAPService:UpdatePack"};
            }
        }

        private static GenericResponse UpdatePackList(PackListInformation listinfo)
        {
            try
            {
                var response = new GenericResponse();
                using (var context = new Entities())
                {
                    var plist = (from list in context.PackListHeads
                        where list.Id == listinfo.Id
                        select list).FirstOrDefault();
                    if (plist == null)
                    {
                        var list = new PackListHead();
                        Mapper.Map(listinfo, list);
                        context.PackListHeads.AddObject(list);
                    }
                    else Mapper.Map(listinfo, plist);
                    context.SaveChanges();
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdatePack");
                LoggingMediator.Log(ex);
                return new GenericResponse {Success = false, FailureInformation = "Error in PAPService:UpdatePackList"};
            }
        }

        private static GenericResponse UpdateTransferPackList(PackListInformation listinfo)
        {
            try
            {
                var response = new GenericResponse();
                using (var context = new Entities())
                {
                    var plist = (from list in context.TransferPackListHeads
                                 where list.Id == listinfo.Id
                                 select list).FirstOrDefault();
                    if (plist == null)
                    {
                        var list = new TransferPackListHead();
                        Mapper.Map(listinfo, list);
                        context.TransferPackListHeads.AddObject(list);
                    }
                    else Mapper.Map(listinfo, plist);
                    context.SaveChanges();
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdatePack");
                LoggingMediator.Log(ex);
                return new GenericResponse {Success = false, FailureInformation = "Error in PAPService:UpdateTransferPackList"};
            }
        }

        private static AvailablePacks.Pack[] GetPacksByYard(Entities context)
        {
            try
            {
                AvailablePacks.Pack[] response = null;
                var query = (from pack in context.Packs
                    where pack.YardId == YardId
                          &&
                          (pack.PackStatus == (int) PackStatus.Closed || pack.PackStatus == (int) PackStatus.Held ||
                           pack.PackStatus == (int) PackStatus.Manifest)
                    select pack);
                
                response = LoadWritePack(query.ToList()) ;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetPacksByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static AvailablePackLists.PackList[] GetPackListsByYard(Entities context)
        {
            try
            {
                AvailablePackLists.PackList[] response = null;
                var query = (from plist in context.PackListHeads
                    where plist.ContractHead.YardId == YardId
                          &&
                          (plist.PackListStatus == (int) PackListStatus.Held ||
                           plist.PackListStatus == (int) PackListStatus.OnShipment)
                    select plist);

                response = LoadWritePackLists(query.ToList());
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetPackListsByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static AvailablePackLists.PackList[] GetTransferPackListsByYard(Entities context)
        {
            try
            {
                AvailablePackLists.PackList[] response = null;
                var query = (from plist in context.TransferPackListHeads
                    where plist.TransferSentHead.YardId == YardId
                          &&
                          (plist.PackListStatus == (int) PackListStatus.Held ||
                           plist.PackListStatus == (int) PackListStatus.OnShipment)
                    select plist);

                response = LoadWriteTransferPackLists(query.ToList());
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetTransferPackListsByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static InventoryItems.Item[] GetAvailableInventoryByYard(Entities context)
        {
            try
            {
                InventoryItems.Item[] response = null;
                var query = (from inv in context.Inventories
                    where inv.YardId == YardId
                    select inv);
                response = LoadWriteInventory(query.ToList());
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetAvailableInventoryByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static AvailableContracts.Contract[] GetOpenContractsByYard(Entities context)
        {
            try
            {
                AvailableContracts.Contract[] response = null;
                var query = (from contract in context.ContractHeads
                    where contract.YardId == YardId
                          && !contract.IsFinished
                          && contract.ContractStatus != (int) ContractStatus.Void
                    select contract);

                response = LoadWriteContract(query.ToList(), context);
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetOpenContractsByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static AvailableContracts.Contract[] GetHeldTransferSentHeadByYard(Entities context)
        {
            try
            {
                AvailableContracts.Contract[] response = null;
                var query = (from transfer in context.TransferSentHeads
                    where transfer.YardId == YardId
                          &&
                          (transfer.TransferStatus == (int) TransferSentStatus.Held ||
                           transfer.TransferStatus == (int) TransferSentStatus.Closed)
                    select transfer);

                response = LoadWriteTransfer(query.ToList());
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetHeldTransferSentHeadByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private GenericResponse ReadPapXml()
        {
            try
            {
                var response = new GenericResponse();
                var xmlDoc = XDocument.Load(@"Files\ReceivePAP.xml");
                var packxmldata = new Packs();
                var packlistxmldata = new PackLists();
                var settings = new XmlReaderSettings {IgnoreWhitespace = true};
                var buffer = Encoding.ASCII.GetBytes(xmlDoc.ToString());
                var xmlStream = new MemoryStream(buffer);
                using (var xmlReader = XmlReader.Create(xmlStream, settings))
                {
                    var packXmlSerializer = new XmlSerializer(packxmldata.GetType());
                    var packlistXmlSerializer = new XmlSerializer(packlistxmldata.GetType());
                    packxmldata = (Packs) packXmlSerializer.Deserialize(xmlReader);
                    packlistxmldata = (PackLists) packlistXmlSerializer.Deserialize(xmlReader);
                    response.FailureInformation = CreatePacksFromXml(packxmldata.PackCollection).FailureInformation;
                    response.FailureInformation += CreatePackListsFromXml(packlistxmldata.PackListCollection);
                }

                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("ReadPapXml");
                LoggingMediator.Log(ex);
                throw;
            }

        }

        private GenericResponse CreatePacksFromXml(IEnumerable<PackReadInformation> packcollection)
        {
            var response = new GenericResponse();
            var userId = Guid.Parse("9BB44614-26C8-4761-BD54-2204E0E76C2D");
            foreach (var pk in packcollection)
            {
                var pack = new PackInformation
                           {
                               Id = Guid.Parse(pk.Id),
                               GrossWeight = Convert.ToDecimal(pk.GrossWeight),
                               TareWeight = Convert.ToDecimal(pk.TareWeight),
                               ScaleGrossWeight = Convert.ToDecimal(pk.ScaleGrossWeight),
                               ScaleTareWeight = Convert.ToDecimal(pk.ScaleTareWeight),
                               TagNumber = Convert.ToInt32(pk.TagNumber),
                               PrintDescription = pk.PrintDescription,
                               UnitOfMeasure = pk.UnitOfMeasure,
                               DateCreated = Convert.ToDateTime(pk.DateCreated),
                               DateClosed = Convert.ToDateTime(pk.DateClosed),
                               Quantity = Convert.ToInt16(pk.Quantity),
                               CommodityType = Convert.ToInt16(pk.CommodityType),
                               PackStatus = Convert.ToInt16(pk.PackStatus),
                               InventoryId = Guid.Parse(pk.InventoryId),
                               YardId = YardId,
                               NumberPrefix = string.Empty,
                               InternalPackNumber = string.Empty,
                               Cost = Convert.ToDecimal(pk.Cost),
                               CreatedByUserId = userId,
                           };
                var pkresponse = UpdatePack(pack);
                if (!pkresponse.Success) response.FailureInformation += pkresponse.FailureInformation;
            }
            return response;
        }

        private GenericResponse CreatePackListsFromXml(IEnumerable<PackListReadInformation> listcollection)
        {
            var response = new GenericResponse();
            var userId = Guid.Parse("9BB44614-26C8-4761-BD54-2204E0E76C2D");
            using (var context = new Entities())
            {
                foreach (var pl in listcollection)
                {
                    var isTransfer = context.TransferSentHeads.FirstOrDefault(trans => trans.Id == Guid.Parse(pl.ContractId)) != null;
                    var plist = new PackListInformation
                                {
                                    Id = Guid.Parse(pl.PackListId),
                                    ContractHeadId = isTransfer?Guid.Empty:Guid.Parse(pl.ContractId),
                                    TransferSentHeadId = isTransfer ? Guid.Parse(pl.ContractId):Guid.Empty,
                                    CreatedByUserId = userId,
                                    PackListNumber = Convert.ToInt32(pl.PackListNumber),
                                    DateCreated = DateTime.Now,
                                    SealNumber = string.Empty,
                                    Units = 0,
                                    PackListStatus = (int) PackListStatus.Held,
                                };
                    var pkresponse = isTransfer?UpdateTransferPackList(plist):UpdatePackList(plist);
                    if (!pkresponse.Success) response.FailureInformation += pkresponse.FailureInformation;
                }
            }

            return response;
        }

        private GenericResponse WritePapXml()
        {
            try
            {
                var response = new GenericResponse();
                using (var context = new Entities())
                {
                    var availablePacks = GetPacksByYard(context);
                    var availablePackLists = GetPackListsByYard(context);
                    var availableTransfersPackLists = GetTransferPackListsByYard(context);
                    var availableContracts = GetOpenContractsByYard(context);
                    var availableTransfers = GetHeldTransferSentHeadByYard(context);
                    var availableInventory = GetAvailableInventoryByYard(context);

                    var allAvailableContracts = availableContracts.Concat(availableTransfers).ToArray();
                    var allAvailablePacklists= availablePackLists.Concat(availableTransfersPackLists).ToArray();

                    var sendData = new Root
                                   {
                                       AvailableContracts = allAvailableContracts,
                                       InventoryItems = availableInventory,
                                       PackLists = allAvailablePacklists,
                                       AvailablePacks = availablePacks
                                   };
                    var writer = new XmlSerializer(typeof (Root));
                    var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var fullFileName = Path.Combine(desktopFolder, "SendPAP.xml");
                    using (var fs = new FileStream(fullFileName, FileMode.Create))
                    {
                        writer.Serialize(fs, sendData);
                        fs.Close();
                    }

                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("ReadPapXml");
                LoggingMediator.Log(ex);
                throw;
            }
        }

        private static AvailablePacks.Pack[] LoadWritePack(List<Pack> availablePacks)
        {
            AvailablePacks.Pack[] availpkData;
            var packlist = new List<AvailablePacks.Pack>();
            availablePacks.ForEach(pk =>
                                   {
                                       var pack = new AvailablePacks.Pack
                                                  {
                                                      PackNumber = pk.TagNumber.ToString(),
                                                      PackComment = pk.PrintDescription,
                                                      GrossWeight = pk.GrossWeight.ToString(),
                                                      TareWeight = pk.TareWeight.ToString(),
                                                      ScaleGrossWeight = pk.ScaleGrossWeight.ToString(),
                                                      ScaleTareWeight = pk.ScaleTareWeight.ToString(),
                                                      PackId = pk.Id.ToString(),
                                                      UnitOfMeasure = pk.UnitOfMeasure,
                                                      DateCreated = pk.DateCreated.ToString(),
                                                      DateClosed = pk.DateClosed.ToString(),
                                                      Quantity = pk.Quantity.ToString(),
                                                      Status = pk.PackStatus.ToString(),
                                                      InventoryId = pk.InventoryId.ToString()
                                                  };
                                       packlist.Add(pack);

                                   });
            availpkData = packlist.ToArray();
            return availpkData;
        }

        private static AvailablePackLists.PackList[] LoadWritePackLists(List<PackListHead> availableLists)
        {
            AvailablePackLists.PackList[] availpkListData;
            var packlist = new List<AvailablePackLists.PackList>();
            availableLists.ForEach(pk =>
                                   {
                                       var packids = new List<AvailablePackLists.PackList.PackIds>();
                                       pk.PackListItems.ToList().ForEach(itm =>
                                                                         {
                                                                             var pid =
                                                                                 new AvailablePackLists.
                                                                                     PackList.PackIds
                                                                                 {
                                                                                     Packid = itm.PackId.ToString()
                                                                                 };
                                                                             packids.Add(pid);
                                                                         });
                                       var pack = new AvailablePackLists.PackList
                                                  {
                                                      PackNumber = pk.PackListNumber.ToString(),
                                                      ContractId = pk.ContractHeadId.ToString(),
                                                      PackListId = pk.Id.ToString(),
                                                      PackIDs = packids.ToArray()
                                                  };
                                       packlist.Add(pack);

                                   });
            availpkListData = packlist.ToArray();
            return availpkListData;
        }

        private static AvailablePackLists.PackList[] LoadWriteTransferPackLists(List<TransferPackListHead> availableLists)
        {
            AvailablePackLists.PackList[] availpkListData;
            var packlist = new List<AvailablePackLists.PackList>();
            availableLists.ForEach(pk =>
            {
                var packids = new List<AvailablePackLists.PackList.PackIds>();
                pk.TransferPackListItems.ToList().ForEach(itm =>
                {
                    var pid =
                        new AvailablePackLists.
                            PackList.PackIds
                        {
                            Packid = itm.PackId.ToString()
                        };
                    packids.Add(pid);
                });
                var pack = new AvailablePackLists.PackList
                {
                    PackNumber = pk.PackListNumber.ToString(),
                    ContractId = pk.TransferSentHeadId.ToString(),
                    PackListId = pk.Id.ToString(),
                    PackIDs = packids.ToArray()
                };
                packlist.Add(pack);

            });
            availpkListData = packlist.ToArray();
            return availpkListData;
        }

        private static InventoryItems.Item[] LoadWriteInventory(List<Inventory> availableInventory)
        {
            InventoryItems.Item[] availInventoryData;
            var itemlist = new List<InventoryItems.Item>();
            availableInventory.ForEach(inv =>
            {
                var inventory = new InventoryItems.Item
                {
                    InventoryId = inv.Id.ToString(),
                    PrintDescription = inv.PrintDescription,
                    DefaultLocationId = inv.DefaultLocationId.ToString(),
                };
                itemlist.Add(inventory);
            });
            availInventoryData = itemlist.ToArray();
            return availInventoryData;
        }

        private static AvailableContracts.Contract[] LoadWriteContract(List<ContractHead> availableContracts, Entities context)
        {
            AvailableContracts.Contract[] availContracts;
            var contractlist = new List<AvailableContracts.Contract>();
            availableContracts.ForEach(con =>
                                       {
                                           var customer = context.Customers.First(cust => cust.Id == con.BillToId);
                                           var billto = string.IsNullOrEmpty(customer.Company)
                                               ? customer.FirstName + " " + customer.LastName
                                               : customer.Company;
                var invIds = new List<AvailableContracts.Contract.Inventory>();
                con.ContractItems.ToList().ForEach(itm =>
                {
                    var pid =
                        new AvailableContracts.Contract.Inventory
                        {
                            InventoryId = itm.InventoryId.ToString()
                        };
                    invIds.Add(pid);
                });
                var contract = new AvailableContracts.Contract
                {
                    ContractId = con.Id.ToString(),
                    CustomerName = billto,
                    ContractDescription = con.ContractDescription,
                    ContractNumber = con.ContractNumber.ToString(),
                    IsTransfer = false.ToString(),
                    InventoryItems = invIds.ToArray()
                };
                contractlist.Add(contract);
            });
            availContracts = contractlist.ToArray();
            return availContracts;
        }

        private static AvailableContracts.Contract[] LoadWriteTransfer(List<TransferSentHead> availableTransfers)
        {
            AvailableContracts.Contract[] availContracts;
            var contractlist = new List<AvailableContracts.Contract>();
            availableTransfers.ForEach(con =>
            {
                var invIds = new List<AvailableContracts.Contract.Inventory>();
                con.TransferSentItems.ToList().ForEach(itm =>
                {
                    var pid =
                        new AvailableContracts.Contract.Inventory
                        {
                            InventoryId = itm.InventoryId.ToString()
                        };
                    invIds.Add(pid);
                });
                var contract = new AvailableContracts.Contract
                {
                    ContractId = con.Id.ToString(),
                    CustomerName = string.IsNullOrEmpty(con.Customer.Company)?con.Customer.FirstName +" " + con.Customer.LastName:con.Customer.Company,
                    ContractDescription = con.Description,
                    ContractNumber = con.TransferNumber.ToString(),
                    IsTransfer = false.ToString(),
                    InventoryItems = invIds.ToArray()
                };
                contractlist.Add(contract);
            });
            availContracts = contractlist.ToArray();
            return availContracts;
        }
    }

    public class GenericResponse
    {
        public GenericResponse()
        {
            Success = false;
            FailureInformation = string.Empty;
        }

        [DataMember(IsRequired = true)]
        public bool Success { get; set; }

        [DataMember(IsRequired = true)]
        public string FailureInformation { get; set; }
    }
}
