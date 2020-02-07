using System;
using System.Collections.Generic;

namespace Zoho_Desk_AD_Sync
{
    //Contact object, directly maps to the equivalent Zoho v1 API Contact object
    public class Contact
    {
        public long id { get; set; }
        public string cf { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string facebook { get; set; }
        public string twitter { get; set; }
        public string email { get; set; }
        public string secondaryEmail { get; set; }
        public string mobile { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string street { get; set; }
        public string zip { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public int ownerId { get; set; }
        public Owner owner { get; set; }
        public long accountId { get; set; }
        public string title { get; set; }
        public string phone { get; set; }
        public bool isDeleted { get; set; }
        public bool isTrashed { get; set; }
        public bool isSpam { get; set; }
        public string photoURL { get; set; }
        public string webUrl { get; set; }
        private DateTime createddt;
        //Convert the timestamp to and from a DateTime object, so it can be used
        //easily if desired
        public string createdTime
        {
            get
            {
                return createddt.ToString("s");
            }
            set
            {
                createddt = DateTime.ParseExact(value, "s", null);
            }
        }

        private DateTime modifieddt;
        //Convert the timestamp to and from a DateTime object, so it can be used
        //easily if desired
        public string modifiedTime
        {
            get
            {
                return modifieddt.ToString("s");
            }
            set
            {
                modifieddt = DateTime.ParseExact(value, "s", null);
            }
        }
    }
    //Contact object, directly maps to the equivalent Zoho v1 API Account object
    public class Account
    {
        public long id { get; set; }
        public string cf { get; set; }
        public string accountName { get; set; }
        public string email { get; set; }
        public string website { get; set; }
        public string fax { get; set; }
        public long ownerId { get; set; }
        public Owner owner { get; set; }
        public List<int> associatedSLAIds { get; set; }
        public string industry { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string street { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public string phone { get; set; }
        public double annualrevenue { get; set; }
        private DateTime createddt;
        //Convert the timestamp to and from a DateTime object, so it can be used
        //easily if desired
        public string createdTime
        {
            get
            {
                return createddt.ToString("s");
            }
            set
            {
                createddt = DateTime.ParseExact(value, "s", null);
            }
        }

        private DateTime modifieddt;
        //Convert the timestamp to and from a DateTime object, so it can be used
        //easily if desired
        public string modifiedTime
        {
            get
            {
                return modifieddt.ToString("s");
            }
            set
            {
                modifieddt = DateTime.ParseExact(value, "s", null);
            }
        }

        public bool isTrashed { get; set; }
        public string webUrl { get; set; }
    }
    //Convert the sub-object used by Account/Contact into an Owner object - maps to Zoho v1 API
    public class Owner
    {
        public long id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
    }

    //Object needed to contain a list from a Zoho rest response for Contacts
    public class RootContactObject
    {
        public List<Contact> data { get; set; }
    }
    //Object needed to contain a list from a Zoho rest response for Accounts
    public class RootAccountObject
    {
        public List<Account> data { get; set; }
    }
}
