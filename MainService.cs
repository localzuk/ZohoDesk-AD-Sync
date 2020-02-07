using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zoho_Desk_AD_Sync
{
    public partial class MainService : ServiceBase
    {
        public RestClient zohoClient { get; set; }
        public string OAuthToken { get; set; }
        public DateTime OAuthTokenTime { get; set; }
        public string OrgId { get; set; }
        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            OrgId = GetOrganizationId();
            OAuthToken = GetRestOAuthToken();
        }

        protected override void OnStop()
        {
        }

        //Returns a string Rest response based on the path sent, and an optional orgId
        //OrgId is needed for all requests other than getting Organization Ids
        public string GetRestResponse(string requestPath, string orgId = null)
        {
            if(OAuthTokenTime.AddMinutes(59) > DateTime.Now) {
                OAuthToken = GetRestOAuthToken();
            }
            string authToken = OAuthToken;
            string serviceURL = Properties.Settings.Default.ZohoAPIURL;
            RestClient restClient = new RestClient(serviceURL);
            RestRequest request = new RestRequest("requestPath");
            if (orgId != null)
            {
                request.AddHeader("orgId", orgId);
            }
            request.AddHeader("Authorization", authToken);
            var tResponse = restClient.Execute(request);
            if (tResponse.ResponseStatus == ResponseStatus.Completed)
            {
                return tResponse.Content;
            } else
            {
                throw new RestRequestException() { HttpStatusCode = tResponse.StatusCode, ResponseStatus = tResponse.ResponseStatus };
            }
        }

        //Send an updated Contact to Zoho
        public string UpdateContact(Contact contact, string orgId)
        {
            if (OAuthTokenTime.AddMinutes(59) > DateTime.Now)
            {
                OAuthToken = GetRestOAuthToken();
            }
            string authToken = OAuthToken;
            string serviceURL = Properties.Settings.Default.ZohoAPIURL;

            RestClient restClient = new RestClient(serviceURL);
            RestRequest request = new RestRequest("contact/" + contact.id) { Method = Method.PATCH };
            request.AddHeader("orgId", orgId);
            request.AddHeader("Authorization", authToken);
            request.AddJsonBody(JsonConvert.SerializeObject(contact));
            var tResponse = restClient.Execute(request);
            if (tResponse.ResponseStatus == ResponseStatus.Completed)
            {
                return tResponse.Content;
            }
            else
            {
                throw new RestRequestException() { HttpStatusCode = tResponse.StatusCode, ResponseStatus = tResponse.ResponseStatus };
            }
        }

        //Returns a List of Contact objects, using the Base OU and domain specified in settings
        public List<Contact> GetADUsers()
        {
            List<Contact> newUsers = new List<Contact>();
            string baseOU = Properties.Settings.Default.ADBaseOU;
            string domain = Properties.Settings.Default.ADDomain;

            List<Account> accounts = GetZohoAccounts();
            using (var context = new PrincipalContext(ContextType.Domain, domain, baseOU))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                        Contact newUser = new Contact();
                        newUser.firstName = Convert.ToString(de.Properties["givenName"].Value);
                        newUser.lastName = Convert.ToString(de.Properties["sn"].Value);
                        newUser.email = Convert.ToString(de.Properties["mail"].Value);
                        newUser.secondaryEmail = Convert.ToString(de.Properties["wWWHomePage"].Value);
                        newUser.accountId = GetAccountIdByName(accounts, Convert.ToString(de.Properties["company"]));
                        newUser.title = Convert.ToString(de.Properties["title"].Value);
                        newUser.phone = Convert.ToString(de.Properties["ipPhone"].Value);
                        newUsers.Add(newUser);
                    }
                }
            }
            return newUsers;
        }

        //Returns a string with the first Organisation from Zoho
        //If you have multiple organisations, this entire project won't work for you 
        //as it is currently coded
        public string GetOrganizationId()
        {
            var responseJson = GetRestResponse("organizations");
            dynamic parsedResponse = JObject.Parse(responseJson);
            string orgId = parsedResponse.data[0].id;
            return orgId;
        }

        //Downloads the current Contacts stored in Zoho, deserialised from Json into a list
        public List<Contact> GetZohoContacts()
        {
            var responseJson = GetRestResponse("contacts", OrgId);
            RootContactObject json = JsonConvert.DeserializeObject<RootContactObject>(responseJson);
            return json.data;
        }

        //Updates the Contacts stored in Zoho by downloading them, downloading the list of valid
        //contacts from Active Directory, and then comparing them - updating the Zoho contact objects
        //Then uploading the list of changed entities to Zoho.
        public void UpdateZohoContacts()
        {
            List<Contact> users = GetADUsers();
            List<Contact> usersToReturn = new List<Contact>();
            List<Contact> newUsers = new List<Contact>();

            List<Contact> retrievedUsers = GetZohoContacts();
            foreach(Contact user in users)
            {
                Contact recordToUpdate = retrievedUsers.Where(p => p.email == user.email)
                                .Select(p => p).FirstOrDefault();
                if (recordToUpdate != null)
                {
                    recordToUpdate.firstName = user.firstName;
                    recordToUpdate.lastName = user.lastName;
                    recordToUpdate.secondaryEmail = user.secondaryEmail;
                    recordToUpdate.phone = user.phone;
                    recordToUpdate.title = user.title;
                    recordToUpdate.accountId = user.accountId;
                    usersToReturn.Add(recordToUpdate);
                } else
                {
                    newUsers.Add(user);
                }

            }

            foreach(Contact user in usersToReturn)
            {
                UpdateContact(user,OrgId);
            }
            foreach(Contact user in newUsers)
            {
                AddContact(user, OrgId);
            }
        }

        //Send a new Contact to Zoho
        private string AddContact(Contact user, string orgId)
        {
            if (OAuthTokenTime.AddMinutes(59) > DateTime.Now)
            {
                OAuthToken = GetRestOAuthToken();
            }
            string authToken = OAuthToken;
            string serviceURL = Properties.Settings.Default.ZohoAPIURL;

            RestClient restClient = new RestClient(serviceURL);
            RestRequest request = new RestRequest("contacts") { Method = Method.POST };
            request.AddHeader("orgId", orgId);
            request.AddHeader("Authorization", authToken);
            request.AddJsonBody(JsonConvert.SerializeObject(user));
            var tResponse = restClient.Execute(request);
            if (tResponse.ResponseStatus == ResponseStatus.Completed)
            {
                return tResponse.Content;
            }
            else
            {
                throw new RestRequestException() { HttpStatusCode = tResponse.StatusCode, ResponseStatus = tResponse.ResponseStatus };
            }
        }

        //Searches for the account with the name passed to it, and returns the account ID or 
        public long GetAccountIdByName(List<Account> accounts, string name)
        {
            return accounts.Where(p => p.accountName == name)
                           .Select(p => p.id).FirstOrDefault();
        }

        //Returns a list of Account objects from Zoho, deserialised from Json into a List
        public List<Account> GetZohoAccounts()
        {
            var responseJson = GetRestResponse("accounts", OrgId);
            RootAccountObject json = JsonConvert.DeserializeObject<RootAccountObject>(responseJson);
            return json.data;
        }

        //Gets the latest list of new Accounts and calls AddAccount for each
        public void UpdateZohoAccounts()
        {
            List<Account> newAccounts = GetNewADAccounts();
            
            foreach(Account account in newAccounts)
            {
                AddAccount(account, OrgId);
            }
        }

        //Uploads a single new Account to Zoho
        private string AddAccount(Account account, string orgId)
        {
            if (OAuthTokenTime.AddMinutes(59) > DateTime.Now)
            {
                OAuthToken = GetRestOAuthToken();
            }
            string authToken = OAuthToken;
            string serviceURL = Properties.Settings.Default.ZohoAPIURL;

            RestClient restClient = new RestClient(serviceURL);
            RestRequest request = new RestRequest("accounts") { Method = Method.POST };
            request.AddHeader("orgId", orgId);
            request.AddHeader("Authorization", authToken);
            request.AddJsonBody(JsonConvert.SerializeObject(account));
            var tResponse = restClient.Execute(request);
            if (tResponse.ResponseStatus == ResponseStatus.Completed)
            {
                return tResponse.Content;
            }
            else
            {
                throw new RestRequestException() { HttpStatusCode = tResponse.StatusCode, ResponseStatus = tResponse.ResponseStatus };
            }
        }

        //Gets all users, and checks if their company property is already an account in Zoho
        //If it isn't, it gets added to a list and returned.
        private List<Account> GetNewADAccounts()
        {
            List<Account> newAccounts = new List<Account>();
            string baseOU = Properties.Settings.Default.ADBaseOU;
            string domain = Properties.Settings.Default.ADDomain;

            List<Account> accounts = GetZohoAccounts();
            using (var context = new PrincipalContext(ContextType.Domain, domain, baseOU))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;

                        Account found = accounts.Where(p => p.accountName == Convert.ToString(de.Properties["company"]))
                                                .Select(p => p).FirstOrDefault();
                        if(found == null)
                        {
                            newAccounts.Add(new Account()
                            {
                                accountName = Convert.ToString(de.Properties["company"])
                            });
                        }
                    }
                }
            }

            return newAccounts;
        }

        //Gets an up to date OAuth Access Token from Zoho.
        //Uses properties in app config
        //Returns a string, or null if something goes wrong
        public string GetRestOAuthToken()
        {
            string authURL = Properties.Settings.Default.ZohoAPIAuthURL;
            string clientID = Properties.Settings.Default.ZohoClientID;
            string clientSecret = Properties.Settings.Default.ZohoClientSecret;

            RestClient restClient = new RestClient(authURL);
            RestRequest request = new RestRequest("request/oauth") { Method = Method.POST };
            request.AddHeader("Accept", "application/json");
            request.AddParameter("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", clientID);
            request.AddParameter("clientSecret", clientSecret);
            request.AddParameter("grant_type", "client_credentials");
            var tResponse = restClient.Execute(request);
            if (tResponse.ResponseStatus == ResponseStatus.Completed)
            {
                var responseJson = tResponse.Content;
                var token = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson)["access_token"].ToString();
                OAuthTokenTime = DateTime.Now;
                return token.Length > 0 ? token : null;
            }
            else
            {
                throw new RestRequestException() { HttpStatusCode = tResponse.StatusCode, ResponseStatus = tResponse.ResponseStatus };
            }

        }
    }
}
