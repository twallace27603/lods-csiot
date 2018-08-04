﻿using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace iotWebApp.Models
{
    public class EventHubContext
    {
        private StorageContext storage;
        private string tableName = "defaultreadings";
        private string containerName = "eventhub2";
        private DeviceWebAPIParameters parms;
        public EventHubContext(DeviceWebAPIParameters parms)
        {
            this.parms = parms;
            storage = new StorageContext(parms.EhubStorage, tableName, containerName);
        }
        public EventHubContext(DeviceWebAPIParameters parms, string tableName, string containerName)
        {
            this.tableName = tableName;
            this.containerName = containerName;
            this.parms = parms;
            storage = new StorageContext(parms.EhubStorage, tableName,  containerName);
        }
        public async Task<EvaluationResult> ReceiveEvents()
        {
            var result = new EvaluationResult { Code = 0, Message = "Received messages", Passed = true };
            try
            {
                var regUtil = new IotUtilities.IotRegistry(parms.IotConnection);
                var deviceNames = await regUtil.GetDeviceNames();

                var priorRowKeys = await storage.RetrieveLastKeys(deviceNames);
                var priorRowKey = priorRowKeys[0];
                var currentRowKey = priorRowKey;
                var eventProcessorHost = new EventProcessorHost(
                    parms.HubName,
                    PartitionReceiver.DefaultConsumerGroupName,
                    parms.EhubConnection,
                    parms.EhubStorage,
                    this.containerName);
                try
                {
                    //await eventProcessorHost.RegisterEventProcessorAsync<EventHubProcessor>();
                    await eventProcessorHost.RegisterEventProcessorFactoryAsync(new MyEventProcessorFactory(parms.EhubStorage,tableName,containerName ));
                    var start = DateTime.Now;
                    var currentTime = DateTime.Now;
                    var seconds = (currentTime - start).TotalSeconds;
                    //Wait for the first table entry for 30 seconds.
                    while ((priorRowKey == currentRowKey) && (seconds < 30))
                    {
                        Thread.Sleep(100);
                        currentTime = DateTime.Now;
                        currentRowKey = (await storage.RetrieveLastKeys(deviceNames))[0];
                        seconds = (currentTime - start).TotalSeconds;
                        System.Diagnostics.Trace.WriteLine($"Seconds: {seconds}\tPrior: {priorRowKey}\tCurrent: {currentRowKey}");
                    }
                    if (currentRowKey == priorRowKey)
                    {
                        //No rows found
                        result.Code = -1;
                        result.Passed = false;
                        result.Message = "No errors occurred, but no events were processed.";

                    }
                    else
                    {
                        //Wait for events to come in.  If synchronous, this will be 0 seconds.  This can be no more than 10 seconds.
                        if (parms.EventReceiveDelay>10) { parms.EventReceiveDelay = 10; }
                        if (parms.EventReceiveDelay > 0) { Thread.Sleep(parms.EventReceiveDelay*1000); }
                        List<DeviceReadingEntity> data = new List<DeviceReadingEntity>();
                        for (int i = 0; i < deviceNames.Count; i++)
                        {
                             data.AddRange((await storage.RetrieveTableData(5, deviceNames)).Data);
                        }
                        result.Data = data;


                    }
                }
                finally
                {
                    await eventProcessorHost.UnregisterEventProcessorAsync();
                }
            }
            catch (Exception outer)
            {
                result.Passed = false;
                result.Code = outer.HResult;
                result.Message = $"Error: {outer.Message}";
            }
            return result;
        }

        //TODO: Delete if successfully builds
        //private async Task<List<long>> getLatestTableRowKeys(CloudTable table, List<string> partitionKeys)
        //{
        //    List<long> results = new List<long>();
        //    foreach (string partitionKey in partitionKeys)
        //    {
        //        var query = new TableQuery<TableEntity>();
        //        query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

        //        var seg = await table.ExecuteQuerySegmentedAsync(query,default(TableContinuationToken));
        //        var firstRow = seg.FirstOrDefault();
        //         results.Add(firstRow != null ? long.Parse(firstRow.RowKey) : long.MaxValue);

        //    }

        //    return results;

        //}
       


    }
}