using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using CODE.Framework.Core.Utilities;
using ScrapDragon.Custom.Pap.WebService;
using ScrapDragon.Custom.Pap.WebService.Information;
using ScrapDragon.Custom.Pap.WebService.RnR;
using ScrapDragon.Service.Contracts.BackOffice;


namespace ScrapDragon.Custom.Pap.WebService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class PapWebService : IPAPService
    {
        private bool _debug;

        private DeviceConfiguration _readDeviceConfiguration;

        private UpdateVin _readVinInformation;

        private static Guid _yardId;

        private static Guid _userId;

        public string TestConnection(string name)
        {
            return "You Sent: " + name + "!";
        }

        public SendPapDataResponse SendPapData(SendPapDataRequest request)
        {
            try
            {
                _debug = false;
                if (_debug)
                {
                    var sb = new StringBuilder();
                    string line;
                    var fullFileName = (@"C:\SDX\SDX4\Scrap Dragon\External_Projects\PAPInterface\PAP_AddPack.xml");
                    var fullFileName2 = Path.Combine(Environment.CurrentDirectory, @"Files\SendPAPExample2.xml");
                    var fullFileName3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        @"PAP XML FILES\4075.txt");
                    var reader = new StreamReader(fullFileName3);

                    while ((line = reader.ReadLine()) != null)

                    {
                        sb.Append(line);
                    }
                    reader.Close();

                    request.Data = sb.ToString();
                }
                var response = new SendPapDataResponse();
                var receiveResponse = ReceivePapData(new ReceivePapDataRequest {Data = request.Data});
                response.FailureInformation = receiveResponse.FailureInformation;
                if (!receiveResponse.Success)return response;
                var xmlresponse = WritePapXml();
                response.FailureInformation += xmlresponse.FailureInformation;
                if (!xmlresponse.Success) return response;
                response.DataResponse = xmlresponse.Data;
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
                // Read in xml and get default data (yard id, user id, device config)
                var xmlresponse = ReadPapXml(request.Data);
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
                           FailureInformation = ex.Message
                       };
            }
        }

        private GenericResponse ReadPapXml(string xmlFile)
        {
            try
            {
                var response = new GenericResponse();
                var info = new Root();
                var settings = new XmlReaderSettings { IgnoreWhitespace = true };
                var buffer = Encoding.ASCII.GetBytes(xmlFile);
                var xmlStream = new MemoryStream(buffer);
                using (var xmlReader = XmlReader.Create(xmlStream, settings))
                {
                    var infoData = new XmlSerializer(info.GetType());
                    info = (Root)infoData.Deserialize(xmlReader);
                    if (string.IsNullOrEmpty(info.YardId))
                    {
                        response.FailureInformation = "Error loading yard id";
                        return response;
                    }
                    if (string.IsNullOrEmpty(info.UserId))
                    {
                        response.FailureInformation = "Error loading user id";
                        return response;
                    }
                    _yardId = Guid.Parse(info.YardId);
                    _userId = Guid.Parse(info.UserId);
                    _readDeviceConfiguration = info.DeviceConfiguration;
                    _readVinInformation = info.UpdatedVins;
                    using (var context = new Entities())
                    {
                        var pkGuids = info.Packs.Pack ==null?new List<string>() : info.Packs.Pack.Select(pk => pk.Id).ToList();
                        var listValidGuids = ValidateGuid(pkGuids);
                        if (listValidGuids.Any(pk => pk == false))
                        {
                            var invalid = listValidGuids.FirstOrDefault(gd => gd == false);
                            response.FailureInformation = "Invalid guid for pack #" +
                                                          listValidGuids.IndexOf(invalid).ToString();
                            return response;
                        }
                        var pkInvGuids = info.Packs.Pack == null ? new List<string>() : info.Packs.Pack.Select(pk => pk.InventoryId).ToList();
                        var listValidInvGuids = ValidateGuid(pkInvGuids);
                        if (listValidInvGuids.Any(inv => inv == false))
                        {
                            var invalid = listValidInvGuids.FirstOrDefault(gd => gd == false);
                            response.FailureInformation = "Invalid guid for inventory #" +
                                                          listValidInvGuids.IndexOf(invalid).ToString();
                            return response;
                        }
                        if (info.Packs != null && info.Packs.Pack!=null)
                        {
                            var packsResponse = CreatePacksFromXml(info.Packs, context);
                            response.FailureInformation = packsResponse.FailureInformation;
                            if (!packsResponse.Success) return response;
                            ((IObjectContextAdapter)context).ObjectContext.Refresh(RefreshMode.ClientWins, context.Packs);
                        }
                        if (info.PackLists != null && info.PackLists.PackListCollection!=null)
                        {
                            var plistResponse = CreatePackListsFromXml(info.PackLists, context);
                            response.FailureInformation += plistResponse.FailureInformation;
                            if (!plistResponse.Success) return response;
                        }
                        context.SaveChanges();
                        response.Success = true;
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("ReadPapXml");
                LoggingMediator.Log(ex);
                return new GenericResponse { FailureInformation = ex.Message, Success = false };
            }
        }

        private static GenericResponse UpdatePack(PackInformation pkinfo, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                //using (var context = new Entities())
                //{
                //Refuse negative net weight packs for close or 
                if (pkinfo.PackStatus != (int)PackStatus.Held && pkinfo.PackStatus != (int)PackStatus.Void && pkinfo.NetWeight < 0)
                {
                    response.FailureInformation = "Failed to update pack #" + pkinfo.InternalPackNumber +
                                                  ", " + ((PackStatus)pkinfo.PackStatus).ToString() +  " status cannot accept negative net weight " + pkinfo.NetWeight;
                    response.Success = true;
                    return response;
                }
                var yd = (from yard in context.Yards
                    where yard.Id == pkinfo.YardId
                    select yard.CustomerNumberPrefix).First() ?? string.Empty;
                pkinfo.NumberPrefix = yd;
                pkinfo.InternalPackNumber = (pkinfo.InternalPackNumber == null || pkinfo.InternalPackNumber == "0")
                    ? yd.Trim() + pkinfo.TagNumber
                    : pkinfo.InternalPackNumber;

                var pack = (from pk in context.Packs
                    where pk.Id == pkinfo.Id
                    select pk).FirstOrDefault();

                var logs = (from log in context.PackLogs
                    where log.PackId == pkinfo.Id
                    select log).ToList().OrderByDescending(lg => lg.ActionDateTime);
                var latestlog = logs.FirstOrDefault();
                if (latestlog != null && latestlog.ActionDateTime > pkinfo.DateModified)
                {
                    response.FailureInformation = "Date modified for pack #" + pkinfo.InternalPackNumber +
                                                  " is prior to latest pack update. Pack not synced.";
                    response.Success = true;
                    return response;
                }

                if (pack == null)
                {
                    var nextNumber =
                        GetNextNumber(
                            new GetNextNumberRequest {NumberType = NextNumberType.Tag, YardId = _yardId}, context);
                    if (!nextNumber.Success)
                    {
                        response.FailureInformation = nextNumber.FailureInformation;
                        return response;
                    }
                    pkinfo.TagNumber = nextNumber.NextNumber;
                    pkinfo.InternalPackNumber = (pkinfo.InternalPackNumber == null || pkinfo.InternalPackNumber == "0")
                        ? yd.Trim() + pkinfo.TagNumber
                        : pkinfo.InternalPackNumber;
                    var packType = context.Inventories.First(inv => inv.Id == pkinfo.InventoryId).PackTypeUdlvId;
                    if (packType == null)
                    {
                        var udlv =
                            context.UserDefinedListValues.Where(
                                lv => lv.UserDefinedList.CodeMastEnum == UserDefinedType.PackType.ToString())
                                .Select(itm => itm.Id)
                                .ToList();
                        if (udlv.Any()) packType = udlv.First();
                    }
                    if (packType == null) return response;
                    var pk = new Pack {PackTypeUdlvId = (Guid) packType};
                    Mapper.Map(pkinfo, pk);
                    context.Packs.Add(pk);
                    var logresponse =
                        SavePackLog(
                            new SavePackLogRequest {Action = (int) PackAction.Pack_Added, Pack = pk, UserId = _userId},
                            context);
                    if (!logresponse.Success)
                    {
                        response.FailureInformation = logresponse.FailureInformation;
                        return response;
                    }
                }
                else
                {

                    Mapper.Map(pkinfo, pack);
                    var logresponse =
                        SavePackLog(
                            new SavePackLogRequest
                            {
                                Action = (int) PackAction.Pack_Changed,
                                Pack = pack,
                                UserId = _userId
                            }, context);
                    if (!logresponse.Success)
                    {
                        response.FailureInformation = logresponse.FailureInformation;
                        return response;
                    }
                }
                //}
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

        private static GenericResponse SavePackLog(SavePackLogRequest request, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var packlog = new PackLog
                              {
                                  Id = Guid.NewGuid(),
                                  PackId = request.Pack.Id,
                                  InventoryId = request.Pack.InventoryId,
                                  UserId = request.UserId,
                                  ActionDateTime = DateTime.Now,
                                  Action = request.Action,
                                  GrossWeight = request.Pack.GrossWeight,
                                  TareWeight = request.Pack.TareWeight,
                                  NetWeight = request.Pack.NetWeight,
                                  Status = request.Pack.PackStatus,
                                  UnitOfMeasure = request.Pack.UnitOfMeasure,
                                  Quantity = request.Pack.Quantity,
                              };
                context.PackLogs.Add(packlog);
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in InventoryService::SavePackLog"
                       };
            }
        }

        private static GenericResponse UpdatePackList(PackListInformation listinfo, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var plist = (from list in context.PackListHeads
                    where list.Id == listinfo.Id
                    select list).FirstOrDefault();
                if (plist == null)
                {
                    var nextNumber =
                        GetNextNumber(
                            new GetNextNumberRequest {NumberType = NextNumberType.PackingList, YardId = _yardId},
                            context);
                    if (!nextNumber.Success)
                    {
                        response.FailureInformation = nextNumber.FailureInformation;
                        return response;
                    }
                    listinfo.PackListNumber = nextNumber.NextNumber;
                    var list = new PackListHead();
                    Mapper.Map(listinfo, list);
                    context.PackListHeads.Add(list);
                }
                else Mapper.Map(listinfo, plist);

                var itemResponse = SavePackListItemsFromPackListRead(listinfo.Packs, listinfo.Id, context);
                if (!itemResponse.Success)
                {
                    response.FailureInformation = itemResponse.FailureInformation;
                    return response;
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdatePack");
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in PAPService:UpdatePackList"
                       };
            }
        }

        private static GenericResponse SavePackListItemsFromPackListRead(IEnumerable<Guid> packIds, Guid packlistId,
            Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var items = context.Packs.Where(itm => packIds.Contains(itm.Id)).ToList();
                var existingItemsToUpdate = (from item in context.PackListItems
                                             where item.PackListHeadId == packlistId
                                             select item).ToList();
                var allPacklistItems = (from item in context.PackListItems
                                      select item).ToList();
                var packList = context.PackListHeads.FirstOrDefault(itm => itm.Id == packlistId);
                var contract = context.ContractHeads.FirstOrDefault(itm => packList.ContractHeadId == itm.Id);
                if (contract == null || packList == null)
                {
                    response.FailureInformation = "Packlist or contract corresponding to pack list item not found";
                    return response;
                }
                //Remove all existing items and close associated packs
                foreach (var item in existingItemsToUpdate)
                {
                    context.PackListItems.Remove(item);
                    var updatePackResponse = UpdatePackStatus(item.PackId, (int) PackStatus.Closed, context);
                    if (!updatePackResponse.Success)
                    {
                        response.FailureInformation = updatePackResponse.FailureInformation;
                        return response;
                    }
                }

                //Add new items
                foreach (var item in items)
                {
                    if (item.PackStatus == (int) PackStatus.Manifest)
                    {
                        //Remove item from old pack list and place on this one
                        var existingItem = allPacklistItems.FirstOrDefault(itm => itm.PackId == item.Id);
                        if(existingItem!=null)context.PackListItems.Remove(existingItem);
                    }
                    var contractItem =
                        contract.ContractItems.FirstOrDefault(citm => citm.InventoryId == item.InventoryId);
                    if (contractItem == null)
                    {
                        response.FailureInformation = "Contract Item corresponding to pack list item not found";
                        return response;
                    }
                    var newitem = new PackListItem
                                  {
                                      Id = Guid.NewGuid(),
                                      ContractItemId = contractItem.Id,
                                      PackListHeadId = packlistId,
                                      PackId = item.Id,
                                  };
                    context.PackListItems.Add(newitem);
                    var updatePackResponse = UpdatePackStatus(item.Id, (int) PackStatus.Manifest, context);
                    if (!updatePackResponse.Success)
                    {
                        response.FailureInformation = updatePackResponse.FailureInformation;
                        return response;
                    }
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("SavePackListItems");
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in ShippingService.cs::SavePackListItems"
                       };
            }
        }

        private static GenericResponse SaveTransferPackListItemsFromTransferPackListRead(IEnumerable<Guid> packIds,
            Guid packlistId, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var items = context.Packs.Where(itm => packIds.Contains(itm.Id)).ToList();
                var existingItemsToUpdate = (from item in context.TransferPackListItems
                    where item.TransferPackListHeadId == packlistId
                    select item).ToList();
                var packList = context.TransferPackListHeads.FirstOrDefault(itm => itm.Id == packlistId);
                var contract = context.TransferSentHeads.FirstOrDefault(itm => packList.TransferSentHeadId == itm.Id);
                var allPacklistItems = (from item in context.TransferPackListItems
                                        select item).ToList();
                if (contract == null || packList == null)
                {
                    response.FailureInformation =
                        "Packlist or contract corresponding to transfer pack list item not found";
                    return response;
                }
                //Remove all existing items and close associated packs
                foreach (var item in existingItemsToUpdate)
                {
                    context.TransferPackListItems.Remove(item);
                    var updatePackResponse = UpdatePackStatus(item.PackId, (int) PackStatus.Closed, context);
                    if (!updatePackResponse.Success)
                    {
                        response.FailureInformation = updatePackResponse.FailureInformation;
                        return response;
                    }
                }

                //Add new items
                foreach (var item in items)
                {
                    if (item.PackStatus == (int)PackStatus.Manifest)
                    {
                        //Remove item from old pack list and place on this one
                        var existingItem = allPacklistItems.FirstOrDefault(itm => itm.PackId == item.Id);
                        if (existingItem != null) context.TransferPackListItems.Remove(existingItem);
                    }
                    var transferSentItem =
                        contract.TransferSentItems.FirstOrDefault(citm => citm.InventoryId == item.InventoryId);
                    if (transferSentItem == null)
                    {
                        response.FailureInformation = "Transfer Item corresponding to pack list item not found";
                        return response;
                    }

                    var newitem = new TransferPackListItem
                                  {
                                      Id = Guid.NewGuid(),
                                      TransferSentItemId = transferSentItem.Id,
                                      TransferPackListHeadId = packlistId,
                                      PackId = item.Id,
                                  };
                    context.TransferPackListItems.Add(newitem);
                    var updatePackResponse = UpdatePackStatus(item.Id, (int) PackStatus.Manifest, context);
                    if (!updatePackResponse.Success)
                    {
                        response.FailureInformation = updatePackResponse.FailureInformation;
                        return response;
                    }
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("SavePackListItems");
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in ShippingService.cs::SavePackListItems"
                       };
            }
        }

        private static GenericResponse UpdatePackStatus(Guid packId, int packStatus, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var pack = context.Packs.FirstOrDefault(pk => pk.Id == packId);
                if (pack != null)
                {
                    if (pack.PackStatus == packStatus)
                    {
                        response.Success = true;
                        return response;
                    }
                    pack.PackStatus = packStatus;
                    var logresponse =
                        SavePackLog(
                            new SavePackLogRequest
                            {
                                Action = (int) PackAction.Pack_Changed,
                                Pack = pack,
                                UserId = _userId
                            }, context);
                    if (!logresponse.Success)
                    {
                        response.FailureInformation = logresponse.FailureInformation;
                        return response;
                    }
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdatePackStatus");
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in BackOfficeService.cs::UpdatePackStatus"
                       };
            }
        }

        private static GenericResponse UpdateTransferPackList(PackListInformation listinfo, Entities context)
        {
            try
            {
                var response = new GenericResponse();
                var plist = (from list in context.TransferPackListHeads
                    where list.Id == listinfo.Id
                    select list).FirstOrDefault();
                if (plist == null)
                {
                    var nextNumber =
                        GetNextNumber(
                            new GetNextNumberRequest {NumberType = NextNumberType.Transfer, YardId = _yardId},
                            context);
                    if (!nextNumber.Success)
                    {
                        response.FailureInformation = nextNumber.FailureInformation;
                        return response;
                    }
                    listinfo.PackListNumber = nextNumber.NextNumber;
                    var list = new TransferPackListHead();
                    Mapper.Map(listinfo, list);
                    context.TransferPackListHeads.Add(list);
                }
                else Mapper.Map(listinfo, plist);

                var itemResponse = SaveTransferPackListItemsFromTransferPackListRead(listinfo.Packs, listinfo.Id,
                    context);
                if (!itemResponse.Success)
                {
                    response.FailureInformation = itemResponse.FailureInformation;
                    return response;
                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("UpdateTransferPackList");
                LoggingMediator.Log(ex);
                return new GenericResponse
                       {
                           Success = false,
                           FailureInformation = "Error in PAPService:UpdateTransferPackList"
                       };
            }
        }

        private static Packs GetPacksByYard(Entities context)
        {
            try
            {
                Packs response = null;
                var query = (from pack in context.Packs
                    where pack.YardId == _yardId
                          &&
                          (pack.PackStatus == (int) PackStatus.Closed || pack.PackStatus == (int) PackStatus.Held ||
                           pack.PackStatus == (int) PackStatus.Manifest)
                    select pack);

                response = LoadWritePack(query.ToList());
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetPacksByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static PackLists GetPackListsByYard(Entities context)
        {
            try
            {
                PackLists response = null;
                var query = (from plist in context.PackListHeads
                    join item in context.PackListItems on plist.Id equals item.PackListHeadId
                    where item.ContractItem.Inventory.YardId == _yardId
                        //plist.ContractHead.YardId == _yardId
                          &&
                          (plist.PackListStatus == (int) PackListStatus.Held ||
                           plist.PackListStatus == (int) PackListStatus.OnShipment)
                    select plist);

                response = LoadWritePackLists(query.ToList(), context);
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("GetPackListsByYard");
                LoggingMediator.Log(ex);
                return null;
            }
        }

        private static PackLists GetTransferPackListsByYard(Entities context)
        {
            try
            {
                PackLists response = null;
                var query = (from plist in context.TransferPackListHeads
                    join item in context.TransferPackListItems on plist.Id equals item.TransferPackListHeadId
                    where item.TransferSentItem.Inventory.YardId == _yardId
                        //plist.TransferSentHead.YardId == _yardId
                          &&
                          (plist.PackListStatus == (int) PackListStatus.Held ||
                           plist.PackListStatus == (int) PackListStatus.OnShipment)
                    select plist);

                response = LoadWriteTransferPackLists(query.ToList(), context);
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
                    where inv.YardId == _yardId
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
                    where contract.YardId == _yardId
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
                    where transfer.YardId == _yardId
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

        private List<bool> ValidateGuid(List<string> theGuid)
        {
            var listresponse = new bool[theGuid.Count];
            for (int i = 0; i < theGuid.Count; i++)
            {
                var index = i;
                try
                {
                    var aG = new Guid(theGuid[i]);
                    listresponse[index] = true;
                }
                catch (Exception)
                {
                    listresponse[index] = false;
                }
            }
            return listresponse.ToList();
        }

        private GenericResponse CreatePacksFromXml(Packs availablePacks, Entities context)
        {
            var response = new GenericResponse();
            if (availablePacks == null)
            {
                response.Success = true;
                return response;
            }
                foreach (
                    var pk in
                        availablePacks.Pack.Where(itm => itm.Id != string.Empty && itm.InventoryId != string.Empty))
                {
                    decimal grossWeight;
                    decimal tareWeight;
                    decimal scaleGrossWeight;
                    decimal scaleTareWeight;
                    long tagNumber;
                    DateTime dateCreated;
                    DateTime dateClosed;
                    int quantity;
                    int packStatus;
                    decimal cost;
                    DateTime dateModified;
                    Decimal.TryParse(pk.GrossWeight, out grossWeight);
                    Decimal.TryParse(pk.TareWeight, out tareWeight);
                    Decimal.TryParse(pk.ScaleGrossWeight, out scaleGrossWeight);
                    Decimal.TryParse(pk.ScaleTareWeight, out scaleTareWeight);
                    Int64.TryParse(pk.TagNumber, out tagNumber);
                    DateTime.TryParse(pk.DateCreated, out dateCreated);
                    DateTime.TryParse(pk.DateClosed, out dateClosed);
                    int.TryParse(pk.Quantity, out quantity);
                    int.TryParse(pk.PackStatus, out packStatus);
                    Decimal.TryParse(pk.AveragePrice, out cost);
                    DateTime.TryParse(pk.DateModified, out dateModified);

                    var pack = new PackInformation
                               {
                                   Id = Guid.Parse(pk.Id),
                                   GrossWeight = grossWeight,
                                   TareWeight = tareWeight,
                                   NetWeight = grossWeight - tareWeight,
                                   ScaleGrossWeight = scaleGrossWeight,
                                   ScaleTareWeight = scaleTareWeight,
                                   TagNumber = tagNumber,
                                   PrintDescription = string.IsNullOrEmpty(pk.PrintDescription) ? "" : pk.PrintDescription,
                                   UnitOfMeasure = string.IsNullOrEmpty(pk.UnitOfMeasure) ? "LB" : pk.UnitOfMeasure,
                                   DateCreated = dateCreated == DateTime.MinValue? DateTime.Now: dateCreated,
                                   DateClosed = dateClosed == DateTime.MinValue ? DateTime.Now : dateClosed,
                                   Quantity = quantity,
                                   //CommodityType = Convert.ToInt16(pk.CommodityType),
                                   PackStatus = Enumerable.Range(0, Enum.GetNames(typeof(PackStatus)).Length).Contains(packStatus) ? packStatus : (int)PackStatus.Held,     
                                   InventoryId = Guid.Parse(pk.InventoryId),
                                   YardId = _yardId,
                                   NumberPrefix = string.Empty,
                                   InternalPackNumber = pk.TagNumber,
                                   Cost = cost,
                                   CreatedByUserId = _userId,
                                   DateModified = dateModified == DateTime.MinValue ? DateTime.Now : dateModified
                               };
                    var pkresponse = UpdatePack(pack, context);
                    response.FailureInformation += pkresponse.FailureInformation;
                    if (!pkresponse.Success) return response;
                }
            response.Success = true;
            return response;
        }

        private static GetNextNumberResponse GetNextNumber(GetNextNumberRequest request, Entities context)
        {
            try
            {
                var response = new GetNextNumberResponse();
                UniqueNumber nextNumber = null;

                var yard = context.Yards.First(yd => yd.Id == _yardId);
                nextNumber = (from number in context.UniqueNumbers
                    where number.NumberType == (int) request.NumberType
                    select number).FirstOrDefault();

                response.CustomerNumberPrefix = yard.CustomerNumberPrefix;
                if (nextNumber == null)
                {
                    nextNumber = new UniqueNumber
                                 {
                                     Id = Guid.NewGuid(),
                                     NumberType = (int) request.NumberType,
                                     Type = request.NumberType.ToString(),
                                     NextNumber = 1
                                 };
                    if (request.YardId != Guid.Empty) nextNumber.YardId = request.YardId;

                    context.UniqueNumbers.Add(nextNumber);
                }

                response.NextNumber = nextNumber.NextNumber;

                nextNumber.NextNumber += request.IncrementNumberBy;
                //context.SaveChanges();
                response.Success = true;
                return response;
            }
            catch (Exception)
            {
                return new GetNextNumberResponse
                       {
                           Success = false,
                           FailureInformation = "Error in BackOfficeService::GetNextNumber()"
                       };
            }
        }

        private GenericResponse CreatePackListsFromXml(PackLists listcollection, Entities context)
        {
            var response = new GenericResponse();
            if (listcollection.PackListCollection == null)
            {
                response.Success = true;
                return response;
            }
            foreach (
                var pl in
                    listcollection.PackListCollection.Where(
                        itm => itm.PackListId != string.Empty && itm.PackCollection.Any()))
            {
                long packlistNumber;
                Int64.TryParse(pl.PackListNumber, out packlistNumber);
                var isTransfer = pl.IsTransfer.ToLowerInvariant()=="true";
                var listId = Guid.Parse(pl.SavedPackListId);
                var packIdsToUpdate = pl.PackCollection.ToList().Select(Guid.Parse).ToArray();
                var plist = new PackListInformation
                            {
                                Id =
                                    (listId == Guid.Empty | listId == null)
                                        ? Guid.NewGuid()
                                        : Guid.Parse(pl.SavedPackListId),
                                ContractHeadId = isTransfer ? Guid.Empty : Guid.Parse(pl.PackListId),
                                TransferSentHeadId = isTransfer ? Guid.Parse(pl.PackListId) : Guid.Empty,
                                CreatedByUserId = _userId,
                                PackListNumber = packlistNumber,
                                DateCreated = DateTime.Now,
                                SealNumber = string.Empty,
                                Units = 0,
                                PackListStatus = (int) PackListStatus.Held,
                                Packs = packIdsToUpdate
                            };
                var pkresponse = isTransfer ? UpdateTransferPackList(plist, context) : UpdatePackList(plist, context);
                response.FailureInformation += pkresponse.FailureInformation;
                if (!pkresponse.Success) return response;
                ((IObjectContextAdapter)context).ObjectContext.Refresh(RefreshMode.ClientWins, context.PackListItems);
                ((IObjectContextAdapter)context).ObjectContext.Refresh(RefreshMode.ClientWins, context.TransferPackListItems);
            }
            response.Success = true;
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
                    var availableInventory = GetAvailableInventoryByYard(context);
                    var allAvailablePacklists = availablePackLists.PackListCollection.Concat(availableTransfersPackLists.PackListCollection).ToArray();
                    var readDeviceConfiguration = _readDeviceConfiguration ?? new DeviceConfiguration();
                    var readVinInformation = _readVinInformation ?? new UpdateVin();

                    var sendData = new Root
                                   {
                                       YardId = _yardId.ToString(),
                                       UserId = _userId.ToString(),
                                       DeviceConfiguration = readDeviceConfiguration,
                                       UpdatedVins = readVinInformation,
                                       InventoryItems = availableInventory,
                                       PackLists = new PackLists{PackListCollection = allAvailablePacklists},
                                       Packs = availablePacks

                                   };
                   
                    response.Data = SerializeObject(sendData);
                    if (_debug)
                    {
                        var writer = new XmlSerializer(typeof(Root));
                        var fullFileName = Path.Combine(Environment.CurrentDirectory, @"Log\SendPAPExample2" + DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss") + ".xml");
                        using (var fs = new FileStream(fullFileName, FileMode.Create))
                        {
                            writer.Serialize(fs, sendData);
                            fs.Close();
                        }
                    }


                }
                response.Success = true;
                return response;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("ReadPapXml");
                LoggingMediator.Log(ex);
                return new GenericResponse { FailureInformation = ex.Message, Success = false };
                throw;
            }
        }

        private static string SerializeObject<T>(T toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            var textWriter = new StringWriter();

            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }

        private static Packs LoadWritePack(List<Pack> availablePacks)
        {
            Packs availpkData;
            var packlist = new List<PackReadInformation>();
            availablePacks.ForEach(pk =>
            {
                var pack = new PackReadInformation
                {
                    TagNumber = pk.InternalPackNumber.ToString(),
                    PrintDescription = pk.PrintDescription,
                    GrossWeight = pk.GrossWeight.ToString(),
                    TareWeight = pk.TareWeight.ToString(),
                    ScaleGrossWeight = pk.ScaleGrossWeight.ToString(),
                    ScaleTareWeight = pk.ScaleTareWeight.ToString(),
                    Id = pk.Id.ToString(),
                    UnitOfMeasure = pk.UnitOfMeasure,
                    DateCreated = pk.DateCreated.ToString(),
                    DateClosed = pk.DateClosed.ToString(),
                    Quantity = pk.Quantity.ToString(),
                    PackStatus = pk.PackStatus.ToString(),
                    InventoryId = pk.InventoryId.ToString(),
                    AveragePrice = "",
                    DateModified = DateTime.Now.ToString()
                };
                packlist.Add(pack);

            });
            availpkData = new Packs{Pack = packlist.ToArray()};
            return availpkData;
        }

        private static PackLists LoadWritePackLists(List<PackListHead> availableLists, Entities context)
        {
            var availpkListData = new PackLists();
            var packlist = new List<PackListReadInformation>();
            var contractPacklist = (from contract in context.ContractHeads
                                    join citems in context.ContractItems on contract.Id equals citems.ContractHeadId
                                    let items =
                                        contract.ContractItems.Where(itm => itm.ContractItemStatus != (int) ContractItemStatus.Void)
                                            .Select(item => item.Id.ToString())
                                    where citems.ContractItemStatus != (int)ContractItemStatus.Void
                                          && citems.Inventory.YardId == _yardId
                                          && contract.ContractStatus == (int) ContractStatus.Open
                                          && !contract.IsFinished
                                    select new {contract, items}).AsEnumerable().Select(c =>
                                        new PackListReadInformation
                                        {
                                            PackListNumber = "",
                                            CustomerName =
                                                string.IsNullOrEmpty(c.contract.Customer.Company)
                                                    ? c.contract.Customer.FirstName + " " +
                                                      c.contract.Customer.LastName
                                                    : c.contract.Customer.Company,
                                            Description = c.contract.ContractDescription,
                                            IsTransfer = "false",
                                            //ContractId = pk.TransferSentHeadId.ToString(),
                                            PackListId = c.contract.Id.ToString(),
                                            SavedPackListId = "",
                                            InventoryCollection = c.items.ToArray()
                                        }
                                    ).ToList();

            availableLists.ForEach(pk =>
            {

                var packids = new List<string>();
                pk.PackListItems.ToList().ForEach(itm => packids.Add(itm.PackId.ToString()));
                var invids = new List<string>();
                pk.ContractHead.ContractItems.ToList().ForEach(itm => invids.Add(itm.InventoryId.ToString()));
                var pack = new PackListReadInformation
                {
                    PackListNumber = pk.PackListNumber.ToString(),
                    CustomerName = string.IsNullOrEmpty(pk.ContractHead.Customer.Company) ? pk.ContractHead.Customer.FirstName + " " + pk.ContractHead.Customer.LastName : pk.ContractHead.Customer.Company,
                    Description = pk.ContractHead.ContractDescription,
                    IsTransfer = "false",
                    //ContractId = pk.TransferSentHeadId.ToString(),
                    PackListId = pk.ContractHeadId.ToString(),
                    SavedPackListId = pk.Id.ToString(),
                    PackCollection = packids.ToArray(),
                    InventoryCollection = invids.ToArray()
                };
                packlist.Add(pack);

            });
            var totalList = packlist.Concat(contractPacklist);
            availpkListData.PackListCollection = totalList.ToArray();
            return availpkListData;
        }

        private static PackLists LoadWriteTransferPackLists(
            List<TransferPackListHead> availableLists, Entities context)
        {
            var availpkListData = new PackLists();
            var packlist = new List<PackListReadInformation>();
            var transferPacklist = (from transferhead in context.TransferSentHeads
                                    join titems in context.TransferSentItems on transferhead.Id equals titems.TransferSentHeadId
                                    let items =
                                        transferhead.TransferSentItems.Where(itm => itm.ItemStatus != (int) TransferItemStatus.Void)
                                            .Select(item => item.Id.ToString())
                                    where titems.ItemStatus != (int)TransferItemStatus.Void
                                          && 
                                          titems.Inventory.YardId == _yardId 
                                          &&
                                          (transferhead.TransferStatus == (int) TransferSentStatus.Held ||
                                          transferhead.TransferStatus == (int) TransferSentStatus.Closed)
                                    select new {transferhead, items}).AsEnumerable().Select(t => new PackListReadInformation
                                                                                                 {
                                                                                                     PackListNumber = "",
                                                                                                     CustomerName =
                                                                                                         string.IsNullOrEmpty(
                                                                                                             t.transferhead.Customer.Company)
                                                                                                             ? t.transferhead.Customer
                                                                                                         .FirstName + " " +
                                                                                                               t.transferhead.Customer
                                                                                                         .LastName
                                                                                                             : t.transferhead.Customer
                                                                                                         .Company,
                                                                                                     Description =
                                                                                                         t.transferhead.Description,
                                                                                                     IsTransfer = "true",
                                                                                                     //ContractId = pk.TransferSentHeadId.ToString(),
                                                                                                     PackListId =
                                                                                                         t.transferhead.Id.ToString(),
                                                                                                     SavedPackListId = "",
                                                                                                     InventoryCollection = t.items.ToArray()
                                                                                                 }).ToList();


            availableLists.ForEach(pk =>
            {
                var packids = new List<string>();
                pk.TransferPackListItems.ToList().ForEach(itm => packids.Add(itm.PackId.ToString()));
                var invids = new List<string>();
                pk.TransferSentHead.TransferSentItems.ToList().ForEach(itm => invids.Add(itm.InventoryId.ToString()));
                var pack = new PackListReadInformation
                {
                    PackListNumber = pk.PackListNumber.ToString(),
                    CustomerName = string.IsNullOrEmpty(pk.TransferSentHead.Customer.Company) ? pk.TransferSentHead.Customer.FirstName + " " + pk.TransferSentHead.Customer.LastName : pk.TransferSentHead.Customer.Company,
                    Description = pk.TransferSentHead.Description,
                    IsTransfer = "true",
                    //ContractId = pk.TransferSentHeadId.ToString(),
                    PackListId = pk.TransferSentHeadId.ToString(),
                    SavedPackListId = pk.Id.ToString(),
                    PackCollection = packids.ToArray(),
                    InventoryCollection = invids.ToArray()
                };
                packlist.Add(pack);

            });
            var totalList = packlist.Concat(transferPacklist);
            availpkListData.PackListCollection = totalList.ToArray();
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
                    Code = inv.Code
                };
                itemlist.Add(inventory);
            });
            availInventoryData = itemlist.ToArray();
            return availInventoryData;
        }

        private static AvailableContracts.Contract[] LoadWriteContract(List<ContractHead> availableContracts,
            Entities context)
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
                    CustomerName =
                        string.IsNullOrEmpty(con.Customer.Company)
                            ? con.Customer.FirstName + " " + con.Customer.LastName
                            : con.Customer.Company,
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

    #region Additional Service Classes
    public class SavePackLogRequest
    {
        public SavePackLogRequest()
        {
            Pack = new Pack();
            UserId = Guid.Empty;
            Action = 0;

        }

        public Pack Pack { get; set; }

        public Guid UserId { get; set; }

        public int Action { get; set; }
    }

    public class GenericResponse
    {
        public GenericResponse()
        {
            Data = string.Empty;
            Success = false;
            FailureInformation = string.Empty;
        }

        [DataMember(IsRequired = true)]
        public bool Success { get; set; }

        [DataMember(IsRequired = true)]
        public string FailureInformation { get; set; }

        [DataMember(IsRequired = true)]
        public string Data { get; set; }
    }

    public class GetNextNumberRequest
    {
        public GetNextNumberRequest()
        {
            IncrementNumberBy = 1;
            NumberType = NextNumberType.None;
            YardId = Guid.Empty;
        }

        public int IncrementNumberBy { get; set; }

        public NextNumberType NumberType { get; set; }

        public Guid YardId { get; set; }
    }

    [DataContract]
    public class GetNextNumberResponse
    {
        public GetNextNumberResponse()
        {
            NextNumber = 0;
            CustomerNumberPrefix = string.Empty;
            Success = false;
            FailureInformation = string.Empty;
        }

        public long NextNumber { get; set; }

        public string CustomerNumberPrefix { get; set; }

        public bool Success { get; set; }

        public string FailureInformation { get; set; }
    }
    #endregion
}

