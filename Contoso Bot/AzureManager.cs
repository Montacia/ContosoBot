using Contoso_Bot.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Contoso_Bot
{
    public class AzureManager
    {
        private static AzureManager instance;
        private MobileServiceClient client;
        private IMobileServiceTable<contosodb> contosodbTable;

        private AzureManager()
        {
            this.client = new MobileServiceClient("https://buticantusexamarin.azurewebsites.net/");
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

        public async Task getuserinfo(contosodb authenticate)
        {            
            List<contosodb> items = await contosodbTable.Where(check => check.username == authenticate.username && check.password == authenticate.password).ToListAsync();
            if (items.Count() < 1)
            {
                authenticate = new contosodb();
            }
            else
            {
                authenticate = items[0];
            }
            
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