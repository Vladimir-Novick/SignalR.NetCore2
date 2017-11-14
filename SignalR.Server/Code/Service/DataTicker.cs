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
        private readonly SemaphoreSlim _updateMessageDataLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentDictionary<string, MessageData> _messageData = new ConcurrentDictionary<string, MessageData>();

        // MessageData can go up or down by a percentage of this factor on each change
        private readonly double _rangePercent = 0.002;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(250);
        private readonly Random _updateOrNotRandom = new Random();

        private Timer _timer;
        private volatile bool _updatingMessageDataDataValues;
        private volatile BroadcastDataServerState _broadcastServerState;

        public DataTicker(IHubContext<DataTickerHub> clients)
        {
            Clients = clients;
            LoadDefaultData();
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

        public IEnumerable<MessageData> GetAllData()
        {
            return _messageData.Values;
        }

        public IObservable<MessageData> StreamData()
        {
            return Observable.Create(
                async (IObserver<MessageData> observer) =>
                {
                    while (BroadcastServerState == BroadcastDataServerState.Open)
                    {
                        foreach (var messageData in _messageData.Values)
                        {
                            observer.OnNext(messageData);
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
                    _timer = new Timer(UpdateMessageDataDataValues, null, _updateInterval, _updateInterval);

                    BroadcastServerState = BroadcastDataServerState.Open;

                    await BroadcastBroadcastServerStateChange(BroadcastDataServerState.Open);
                }
            }
            finally
            {
                _broadcastServerStateLock.Release();
            }
        }

        public async Task CloseMarket()
        {
            await _broadcastServerStateLock.WaitAsync();
            try
            {
                if (BroadcastServerState == BroadcastDataServerState.Open)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }

                    BroadcastServerState = BroadcastDataServerState.Closed;

                    await BroadcastBroadcastServerStateChange(BroadcastDataServerState.Closed);
                }
            }
            finally
            {
                _broadcastServerStateLock.Release();
            }
        }

        public async Task Reset()
        {
            await _broadcastServerStateLock.WaitAsync();
            try
            {
                if (BroadcastServerState != BroadcastDataServerState.Closed)
                {
                    throw new InvalidOperationException("BroadcastServer must be closed before it can be reset.");
                }

                LoadDefaultData();
                await BroadcastDataServerReset();
            }
            finally
            {
                _broadcastServerStateLock.Release();
            }
        }

        private void LoadDefaultData()
        {
            _messageData.Clear();

            var messageDatas = new List<MessageData>
            {
                new MessageData { DataKey = "key1", DataValue = "Test1" },
                new MessageData { DataKey = "key2", DataValue = "Test2" },
                new MessageData { DataKey = "key3", DataValue = "test3" }
            };

            messageDatas.ForEach(messageData => _messageData.TryAdd(messageData.DataKey, messageData));
        }

        private async void UpdateMessageDataDataValues(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _updateMessageDataLock.WaitAsync();
            try
            {
                if (!_updatingMessageDataDataValues)
                {
                    _updatingMessageDataDataValues = true;

                    foreach (var messageData in _messageData.Values)
                    {
                        TryUpdateMessageDataDataValue(messageData);
                    }

                    _updatingMessageDataDataValues = false;
                }
            }
            finally
            {
                _updateMessageDataLock.Release();
            }
        }

        private bool TryUpdateMessageDataDataValue(MessageData messageData)
        {
            // Randomly choose whether to udpate this messageData or not
            var r = _updateOrNotRandom.NextDouble();
            if (r > 0.1)
            {
                return false;
            }


            messageData.DataValue = "Value =" +r.ToString();
            return true;
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

        private async Task BroadcastDataServerReset()
        {
            await Clients.Clients.All.InvokeAsync("serverReset");
        }


    }

    public enum BroadcastDataServerState
    {
        Closed,
        Open
    }
}