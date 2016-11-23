using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MobileServices;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Contoso_Bot.DataModels;

namespace Contoso_Bot
{
    [Serializable]
    public class loginDialog : IDialog<object>
    {
        protected string username;
        protected string password;
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            PromptDialog.Text(
                context,
                storeuser,
                "Please type in your Username."
                );
            PromptDialog.Text(
                context,
                storepass,
                "Please type in your Password."
                );
            await context.PostAsync(this.username + this.password);
            context.Wait(MessageReceivedAsync);
        }
        public async Task storeuser(IDialogContext context, IAwaitable<string> argument)
        {
            var username = await argument;

            this.username = username;

            context.Wait(MessageReceivedAsync);
        }
        public async Task storepass(IDialogContext context, IAwaitable<string> argument)
        {
            var password = await argument;

            this.password = password;

            context.Wait(MessageReceivedAsync);
        }
    }
    

    [Serializable]
    public class updateDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            
            var message = await argument;
            await context.PostAsync("You said: " + message.Text);
            context.Wait(MessageReceivedAsync);
        }
    }

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                if (activity.Text == "help")
                {
                    Activity reply = activity.CreateReply($"I can tell you about the current exchange rates, accounts, cards, kiwisaver, loans, mortgages and term deposits. \n I can also do this.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else
                {
                    MobileServiceClient dbclient = AzureManager.AzureManagerInstance.AzureClient;
                    intent.RootObject rootObject;
                    HttpClient luisclient = new HttpClient();
                    string x = await luisclient.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/0bc944f0-ba4d-4c3c-9588-7587eabcd1d8?subscription-key=fe392207fc69410cb8f52911ac8b4599&q=" + activity.Text));
                    rootObject = JsonConvert.DeserializeObject<intent.RootObject>(x);
                    string intent = rootObject.topScoringIntent.intent;
                    //intent: none
                    Activity reply = activity.CreateReply($"Sorry, I don't understand. Please rephrase or type 'help' to see what I can do for you.");

                    //intent: help
                    if (intent == "help")
                    {

                    }

                    //intent: greeting
                    if (intent == "greeting")
                    {
                        reply = activity.CreateReply($"Hello, how may I help you today?");
                    }

                    //intent: query
                    if (intent == "query")
                    {
                        List<intent.Entity> entities = rootObject.entities;
                        string entity = entities[0].type;
                        if (entity.Contains("exchangerate"))
                        {
                            reply = activity.CreateReply($"mystomachhurts");
                        }
                        if (entity.Contains("accounts"))
                        {

                        }
                        if (entity.Contains("cards"))
                        {

                        }
                        if (entity.Contains("kiwisaver"))
                        {

                        }
                        if (entity.Contains("loans"))
                        {

                        }
                        if (entity.Contains("mortgages"))
                        {

                        }
                        if (entity.Contains("termdeposits"))
                        {

                        }
                    }


                    //intent: open
                    if (intent == "open")
                    {

                    }

                    //intent: updateinfo
                    if (intent == "updateinfo")
                    {   
                        if (userData.GetProperty<bool>("loggedin"))
                        {
                            await Conversation.SendAsync(activity, () => new updateDialog());
                        }
                        else
                        {
                            await Conversation.SendAsync(activity, () => new loginDialog());
                            userData.SetProperty<bool>("loggedin", true);
                            await Conversation.SendAsync(activity, () => new updateDialog());
                        }
                        
                        reply=activity.CreateReply($"");
                    }

                    //intent: disablecard
                    if (intent == "disablecard")
                    {

                    }

                    //intent: close
                    if (intent == "close")
                    {

                    }


                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}