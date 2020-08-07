using PeekaRepo.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace PeekaRepo.Pages
{
    public partial class ConnectRepository
    {
        [Inject]
        IJSRuntime JSRuntime { get; set; }
        public string StatusText { get; set; }
        [Inject]
        public ConnectionData ConnectionData { get; set; }
        [Inject]
        GitApiConnector mConnector { get; set; }
  
        public async Task Connect()
        {
            if(!string.IsNullOrWhiteSpace(ConnectionData.Owner) && !string.IsNullOrWhiteSpace(ConnectionData.Repository))
            {
                string errorMessage = "";
                if(!string.IsNullOrEmpty(ConnectionData.Password) && !string.IsNullOrWhiteSpace(ConnectionData.Username))
                {
                    errorMessage = await mConnector.Connect(ConnectionData.Owner, ConnectionData.Repository, ConnectionData.Username, ConnectionData.Password);
                }
                else
                {
                    errorMessage = await mConnector.Connect(ConnectionData.Owner, ConnectionData.Repository, ConnectionData.Token);
                }
                
                if(string.IsNullOrEmpty(errorMessage))
                {
                    StatusText = "Connection successfull";
                }
                else
                {
                    StatusText = "Connection failed: " + errorMessage;
                }
            }
            else
            {
                StatusText = "Please provide owner and repository";
            }
            this.StateHasChanged();
        }
    }
}
