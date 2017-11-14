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

        public IEnumerable<MessageData> GetAllData()
        {
            return _dataTicker.GetAllData();
        }

        public IObservable<MessageData> GetDataStreaming()
        {
            return _dataTicker.StreamData();
        }



        public async Task Reset()
        {
            await _dataTicker.Reset();
        }
    }
}