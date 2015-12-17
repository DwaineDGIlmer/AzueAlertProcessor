using AzureAlerting.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace AzureAlerts.Controllers
{
    public class AlertsController : ApiController
    {
        #region AlertEventHandler
        private AlertEventHandler _eventHandler = new AlertEventHandler();
        #endregion

        #region REST Endpoints
        // GET: api/Alerting/token
        public HttpResponseMessage GetAlert(string token)
        {
            string accessToken = ConfigurationManager.AppSettings["Microsoft.Azure.AccessToken"];
            if (token == accessToken)
            {
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                string authToken = GetAuthorizationHeader();

                response.Headers.Add(HttpRequestHeader.Authorization.ToString(), "Bearer " + authToken);
                return response;
            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        // POST: api/Alerting
        public void Post(AlertModel value)
        {
            if (!ModelState.IsValid || value == null || value.context == null)
            {
                Trace.WriteLine("AlertEventHandler::Post failed with no data");
                return;
            }
            
            _eventHandler.SendAsync(value.context);
            return;
        }
        #endregion

        #region GetAuthorizationHeader
        private static string GetAuthorizationHeader()
        {
            /// Authenticating Azure Resource Manager requests
            /// https://msdn.microsoft.com/en-us/library/azure/dn790557.aspx
            /// 
            /// NOTE: TenantId is the domain name used in the Active Directory [Your domain name].onmicrosoft.com
            /// 
            string domainname = string.Concat("https://login.windows.net/", ConfigurationManager.AppSettings["Microsoft.Azure.DomainName"]);
            string clientId = ConfigurationManager.AppSettings["Microsoft.Azure.ClientId"];
            string clientSecret = ConfigurationManager.AppSettings["Microsoft.Azure.ClientSecret"];

            try
            {
                AuthenticationContext auth = new AuthenticationContext(domainname);
                ClientCredential cred = new ClientCredential(clientId, clientSecret);
                AuthenticationResult result = auth.AcquireToken("https://management.core.windows.net/", cred);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to obtain the Authorization token.");
                }
                return result.AccessToken;
            }
            catch (Exception e)
            {
                Trace.WriteLine("AlertEventHandler::SendAsync Error: {0}", e.Message);
                throw;
            }
        }
        #endregion
    }

    public sealed class AlertEventHandler
    {
        #region Private properties
        private EventHubClient _hubClient;
        private bool _initialized;
        #endregion

        #region Constuctor methods
        public AlertEventHandler()
        {
            this._initialized = false;
            if (!this.InitializeHubClient())
            {
                Trace.WriteLine("AlertEventHandler::AlertEventHandler The hub client failed to initalize");
                throw new Exception("Unable to continue, please check your paramters and check the tracing output for further troubleshooting");
            }
        }
        private bool InitializeHubClient()
        {
            if (this._initialized)
            {
                Trace.WriteLine("AlertEventHandler::InitializeHubClient The hub client has already been initalized");
                return false;
            }

            string conn = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
            string name = ConfigurationManager.AppSettings["Microsoft.ServiceBus.Eventhub"];

            var mgr = NamespaceManager.CreateFromConnectionString(conn);
            var eventHubDescription = mgr.CreateEventHubIfNotExists(name);

            if ((eventHubDescription.Status == EntityStatus.Active) &&
                (_hubClient = EventHubClient.Create(name)) != null)
            {
                return _initialized = true;
            }
            Trace.WriteLine("AlertEventHandler::InitializeHubClient The hub client was not initalized");
            return false;
        }
        #endregion

        #region Send method
        public void SendAsync(object obj, string partition = "0", int maxSize = 255000, int minSize = 100)
        {
            string s = JsonConvert.SerializeObject(obj);

            EventData data = new EventData(Encoding.UTF8.GetBytes(s));
            data.PartitionKey = partition;

            if ((data.SerializedSizeInBytes > maxSize) || (data.SerializedSizeInBytes < minSize))
            {
                throw new ArgumentOutOfRangeException("The event data size is out of bounds");
            }

            try
            {
                this._hubClient.SendAsync(data);
            }
            catch (TimeoutException exception)
            {
                Trace.WriteLine("AlertEventHandler::SendAsync Time out error: {0}", exception.Message);
            }
        }
        #endregion
    }
}
