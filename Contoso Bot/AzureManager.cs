using Contoso_Bot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contoso_Bot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<contosodb> contosodbTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("http://buticantusexamarin.azurewebsites.net");
            this.contosodbTable = this.client.GetTable<contosodb>();
        }

        public MobileServiceClient AzureClient
        {
            get { return client; }
        }

        public static AzureManager AzureManagerInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureManager();
                }

                return instance;
            }
        }

        public async Task<contosodb> getuserinfo(contosodb authenticate)
        {            
            List<contosodb> items = await contosodbTable.Where(check => check.username == authenticate.username && check.password == authenticate.password).ToListAsync();
            if (items.Count() < 1)
            {
                authenticate = new contosodb();
            }
            else
            {
                authenticate = items.ElementAt(0);
            }
            return authenticate;
            
        }

        //public async Task verifypassword (contosodb authenicate)
        //{
        //
        //}

        public async Task<string> uniqueusernamegenerate(string username)
        {   

            List<contosodb> existinguser = await contosodbTable.Where(check => check.username == username).ToListAsync();
            while (existinguser.Count()>0)
            {
                Random rng = new Random();
                string newname = "";                    
                for (int i = 0; i < 8; i++) {
                    newname = string.Concat(newname + rng.Next(0, 10).ToString());
                };
                username = newname;
                existinguser = await contosodbTable.Where(check => check.username == username).ToListAsync();
            }

            return username;
                
        }

        public async Task adduser(contosodb newuser)
        {
            await this.contosodbTable.InsertAsync(newuser);
        }

        public async Task updateuserinfo(contosodb newinfo)
        {
            await this.contosodbTable.UpdateAsync(newinfo);
        }
    }
}