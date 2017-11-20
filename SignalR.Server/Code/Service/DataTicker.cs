using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR;
using SignalR.Server.Model;

namespace SignalR.NetCore2.Code.Service
{
    public class DataTicker
    {
        private readonly SemaphoreSlim _broadcastServerStateLock = new SemaphoreSlim(1, 1);

        private ConcurrentDictionary<string, MessageData> _messageData =
                 new ConcurrentDictionary<string, MessageData>();

        private ConcurrentDictionary<string, ConcurrentQueue<MessageData>> ObserverQueue =
         new ConcurrentDictionary<string, ConcurrentQueue<MessageData>>();

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(250);

        private volatile BroadcastDataServerState _broadcastServerState;

        public DataTicker(IHubContext<DataTickerHub> clients)
        {
            Clients = clients;
        }

        private IHubContext<DataTickerHub> Clients
        {
            get;
            set;
        }

        public BroadcastDataServerState BroadcastServerState
        {
            get { return _broadcastServerState; }
            private set { _broadcastServerState = value; }
        }

        public void Connection(String connectionID)
        {
            ConcurrentQueue<MessageData> ConcurrentQueue = new ConcurrentQueue<MessageData>();
            ObserverQueue.TryAdd(connectionID, ConcurrentQueue);
        }

        public void Disconnection(String connectionID)
        {
            ConcurrentQueue<MessageData> oldConcurrentQueue;
            ObserverQueue.TryRemove(connectionID, out oldConcurrentQueue);
        }

        public IEnumerable<MessageData> GetAllData()
        {
            return _messageData.Values;
        }


        public void AddMessageToQue(MessageData message)
        {
            foreach (var observerQueue in ObserverQueue.Values)
            {
                observerQueue.Enqueue(message);
            }
        }
        public IObservable<MessageData> StreamData(String connectionID)
        {

            return Observable.Create(
                async (IObserver<MessageData> observer) =>
                {
                    while (BroadcastServerState == BroadcastDataServerState.Open)
                    {

                        try
                        {
                            ConcurrentQueue<MessageData> observerQueue;
                            if (ObserverQueue.TryGetValue(connectionID, out observerQueue))
                            {

                                MessageData message;

                                if (observerQueue.TryDequeue(out message))
                                {
                                    observer.OnNext(message);
                                }
                            } else {
                                break;
                            }

                        }
                        catch
                        {

                        }
                        await Task.Delay(_updateInterval);
                    }
                });
        }

        public async Task OpenBroadcastServer()
        {
            await _broadcastServerStateLock.WaitAsync();
            try
            {
                if (BroadcastServerState != BroadcastDataServerState.Open)
                {

                    BroadcastServerState = BroadcastDataServerState.Open;

                    await BroadcastBroadcastServerStateChange(BroadcastDataServerState.Open);
                }
            }
            finally
            {
                _broadcastServerStateLock.Release();
            }
        }




        public void SetNewKey(String dataKey)
        {
            var sKeyMessage = new MessageData { DataKey = dataKey, DataValue = dataKey };
            _messageData.TryAdd(sKeyMessage.DataKey, sKeyMessage);
            {
                AddMessageToQue(sKeyMessage);
            }
            return;
        }

        public void DeleteKey(string key)
        {
            MessageData deletedMessage;

            _messageData.TryRemove(key, out deletedMessage);
            if (deletedMessage != null)
            {
                deletedMessage.DataValue = "";
                AddMessageToQue(deletedMessage);
            }
        }

        private async Task BroadcastBroadcastServerStateChange(BroadcastDataServerState serverState)
        {
            switch (serverState)
            {
                case BroadcastDataServerState.Open:
                    await Clients.Clients.All.InvokeAsync("serverOpened");
                    break;
                case BroadcastDataServerState.Closed:
                    await Clients.Clients.All.InvokeAsync("serverClosed");
                    break;
                default:
                    break;
            }
        }



    }

    public enum BroadcastDataServerState
    {
        Closed,
        Open
    }
}