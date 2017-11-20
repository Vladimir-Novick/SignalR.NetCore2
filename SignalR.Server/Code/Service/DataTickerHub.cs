using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalR.Server.Model;

namespace SignalR.NetCore2.Code.Service
{
    public class DataTickerHub : Hub
    {
        private readonly DataTicker _dataTicker;

        public DataTickerHub(DataTicker dataTicker)
        {
            _dataTicker = dataTicker;
            _dataTicker.OpenBroadcastServer().Wait();
        }

        public IEnumerable<MessageData> GetAllData() => _dataTicker.GetAllData();

        public void SetNewKey(string key) => _dataTicker.SetNewKey(key);


        public void DeleteKey(string key) => _dataTicker.DeleteKey(key);

        public IObservable<MessageData> GetDataStreaming() => _dataTicker.StreamData(Context.ConnectionId);


        public override Task OnConnectedAsync()
        {
            String ConnectionId = Context.ConnectionId;
            _dataTicker.Connection(ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            String ConnectionId = Context.ConnectionId;
            _dataTicker.Disconnection(ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}