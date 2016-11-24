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
            string salt = await retrsalt(authenticate.username);
            string hash = await retrhash(authenticate.username);
            bool authentic = security.verifyhash(authenticate.password, salt, hash);
            if (authentic)
            {
                List<contosodb> items = await contosodbTable.Where(check => check.username == authenticate.username).ToListAsync();
                authenticate = items.ElementAt(0);
            }
            else
            {
                authenticate = new contosodb();
            }
            return authenticate;

        }
        public async Task<string> retrhash(string username)
        {
            List<contosodb> user = await contosodbTable.Where(check => check.username == username).ToListAsync();
            string hash = user[0].password;
            return hash;
        }
        public async Task<string> retrsalt(string username)
        {
            List<contosodb> user = await contosodbTable.Where(check => check.username == username).ToListAsync();
            string salt = user[0].salt;
            return salt;
        }

        public async Task<string> uniqueusernamegenerate(string username)
        {   

            List<contosodb> existinguser = await contosodbTable.Where(check => check.username == username).ToListAsync();
            while (existinguser.Count()>0)
            {
                username = security.genuser();
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