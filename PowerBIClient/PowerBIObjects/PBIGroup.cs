﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using gbrueckl.PowerBI.API.PowerBIObjects;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Microsoft.PowerBI.Api.V2.Models;

namespace gbrueckl.PowerBI.API.PowerBIObjects
{
    public class PBIGroup : IPBIObject
    {
        #region Private Properties for Serialization
        [JsonProperty(PropertyName = "@odata.context", NullValueHandling = NullValueHandling.Ignore, Required = Required.Default)]
        private string ODataContext;

        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "isReadOnly", Required = Required.Always)]
        public bool IsReadOnly { get; set; }

        private string _apiURL = null;
        #endregion

        #region Public Properties
        public List<PBIDataset> Datasets
        {
            get
            {
                PBIObjectList<PBIDataset> objList = JsonConvert.DeserializeObject<PBIObjectList<PBIDataset>>(ParentPowerBIAPI.SendGETRequest(ApiURL, PBIAPI.DataSets).ResponseToString());

                foreach (var item in objList.Items)
                {
                    item.ParentPowerBIAPI = this.ParentPowerBIAPI;
                    item.ParentObject = this;

                    if (!(this is PBIAPIClient)) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                        item.ParentGroup = this;
                }

                return objList.Items;
            }
        }     

        public List<PBIDashboard> Dashboards
        {
            get
            {
                PBIObjectList<PBIDashboard> objList = JsonConvert.DeserializeObject<PBIObjectList<PBIDashboard>>(ParentPowerBIAPI.SendGETRequest(ApiURL, PBIAPI.Dashboards).ResponseToString());

                foreach (var item in objList.Items)
                {
                    item.ParentPowerBIAPI = this.ParentPowerBIAPI;
                    item.ParentObject = this;

                    if (!(this is PBIAPIClient)) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                        item.ParentGroup = this;
                }

                return objList.Items;
            }
        }

        public List<PBIReport> Reports
        {
            get
            {
                PBIObjectList<PBIReport> objList = JsonConvert.DeserializeObject<PBIObjectList<PBIReport>>(ParentPowerBIAPI.SendGETRequest(ApiURL, PBIAPI.Reports).ResponseToString());

                foreach (var item in objList.Items)
                {
                    item.ParentPowerBIAPI = this.ParentPowerBIAPI;
                    item.ParentObject = this;

                    if (!(this is PBIAPIClient)) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                        item.ParentGroup = this;
                }

                return objList.Items;
            }
        }

        public List<PBIImport> Imports
        {
            get
            {
                PBIObjectList<PBIImport> objList = JsonConvert.DeserializeObject<PBIObjectList<PBIImport>>(ParentPowerBIAPI.SendGETRequest(ApiURL, PBIAPI.Imports).ResponseToString());

                foreach (var item in objList.Items)
                {
                    item.ParentPowerBIAPI = this.ParentPowerBIAPI;
                    item.ParentObject = this;

                    if (!(this is PBIAPIClient)) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                        item.ParentGroup = this;
                }

                return objList.Items;
            }
        }

        public List<PBIGroupMember> GroupMembers
        {
            get
            {
                PBIObjectList<PBIGroupMember> objList = JsonConvert.DeserializeObject<PBIObjectList<PBIGroupMember>>(ParentPowerBIAPI.SendGETRequest(ApiURL, PBIAPI.Users).ResponseToString());

                return objList.Items;
            }
        }


        [JsonIgnore]
        public PBIAPIClient ParentPowerBIAPI { get; set; }

        [JsonIgnore]
        public PBIGroup ParentGroup { get; set; }
        [JsonIgnore]
        public string ApiURL
        {
            get
            {
                if(string.IsNullOrEmpty(_apiURL))
                    return string.Format("/v1.0/myorg/groups/{0}", Id);
                return _apiURL;
            }
            protected set { _apiURL = value; }
        }

        [JsonIgnore]
        public IPBIObject ParentObject { get; set; }
        #endregion

        #region Public Methods
        public PBIDataset GetDatasetByID(string id)
        {
            try
            {
                return Datasets.Single(x => string.Equals(x.Id, id, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                //return null;
                throw new KeyNotFoundException(string.Format("No Dataset with ID '{0}' could be found in PowerBI!", id), e);
            }
        }

        public PBIDataset GetDatasetByName(string name)
        {
            try
            {
                return Datasets.Single(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase)); 
            }
            catch (Exception e)
            {
                //return null;
                throw new KeyNotFoundException(string.Format("No Dataset with name '{0}' could be found in PowerBI!", name), e);
            }
        }

        public PBIReport GetReportByName(string name)
        {
            try
            {
                return Reports.Single(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                //return null;
                throw new KeyNotFoundException(string.Format("No Report with name '{0}' could be found in PowerBI!", name), e);
            }
        }

        public PBIDashboard GetDashboardByName(string name)
        {
            try
            {
                return Dashboards.Single(x => string.Equals(x.DisplayName, name, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception e)
            {
                //return null;
                throw new KeyNotFoundException(string.Format("No Dashboard with name '{0}' could be found in PowerBI!", name), e);
            }
        }

        public void AddGroupMember(string username, PBIGroupAccessRight accessRight)
        {
            PBIGroupMember newMember = new PBIGroupMember() { Name = username, AccessRight = accessRight };

            AddGroupMember(newMember);
        }

        public void AddGroupMember(PBIGroupMember groupMember)
        {
            if (this is PBIAPIClient) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                throw new Exception("Cannot add users to 'My Workspace'!");

            using (HttpWebResponse response = ParentPowerBIAPI.SendPOSTRequest(ApiURL, PBIAPI.Users, PBIJsonHelper.SerializeObject(groupMember)))
            {
                string result = response.ResponseToString();
            }
        }

        public void RemoveGroupMember(string username)
        {
            if (this is PBIAPIClient) // if the caller is a PBIClient, we do not have a ParentGroup but need to use "My Workspace" instead
                throw new Exception("Cannot remove users from 'My Workspace'!");

            using (HttpWebResponse response = ParentPowerBIAPI.SendDELETERequest(ApiURL, PBIAPI.Users, username))
            {
                string result = response.ResponseToString();
            }
        }

        public PBIImport ImportPBIX(string displayName, string pbixPath)
        {
            string fullUrl = string.Format("{0}/{1}?datasetDisplayName={2}", ApiURL, PBIAPI.Imports.ToString().ToLower(), displayName);

            FileStream content = File.Open(pbixPath, FileMode.Open);

            using (HttpWebResponse response = ParentPowerBIAPI.SendPOSTRequest(fullUrl, content))
            {
                string result = response.ResponseToString();

                return JsonConvert.DeserializeObject<PBIImport>(result);
            }

            /*
            // create REST URL with import name in quer string
            string restUrlImportPbix = ProgramConstants.PowerBiServiceRootUrl + "imports?datasetDisplayName=" + importName;
            // load PBIX file into StreamContent object
            var pbixBodyContent = new StreamContent(File.Open(pbixPath, FileMode.Open));
            // add headers for request bod content
            pbixBodyContent.Headers.Add("Content-Type", "application/octet-stream");
            pbixBodyContent.Headers.Add("Content-Disposition",
                                         @"form-data; name=""file""; filename=""" + pbixFilePath + @"""");
            // load PBIX content into body using multi-part form data
            MultipartFormDataContent requestBody = new MultipartFormDataContent(Guid.NewGuid().ToString());
            requestBody.Add(pbixBodyContent);
            // create and configure HttpClient
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);
            // post request
            var response = client.PostAsync(restUrlImportPbix, requestBody).Result;
            // check for success
            if (response.StatusCode.ToString().Equals("Accepted"))
            {
                Console.WriteLine("Import process complete: " + response.Content.ReadAsStringAsync().Result);
            }
            */
        }
        #endregion

    }

    
}
