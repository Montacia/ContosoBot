﻿using System;
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
using Contoso_Bot.DataModels;

namespace Contoso_Bot
{
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
                else if (userData.GetProperty<bool>("storeusername"))
                {
                    userData.SetProperty<string>("username", activity.Text);
                    userData.SetProperty<bool>("storeusername", false);
                    userData.SetProperty<bool>("storepassword", true);
                    Activity reply = activity.CreateReply($"Please enter your password.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else if (userData.GetProperty<bool>("storepassword"))
                {
                    userData.SetProperty<string>("password", activity.Text);
                    contosodb logininfo = new contosodb() {username = userData.GetProperty<string>("username"),
                    password = userData.GetProperty<string>("password")};
                    await AzureManager.AzureManagerInstance.getuserinfo(logininfo);
                    if (logininfo.username != userData.GetProperty<string>("username"))
                    {
                        userData.SetProperty<bool>("storeusername", true);
                        userData.SetProperty<bool>("storepassword", false);
                        Activity reply = activity.CreateReply($"Wrong credentials please try again.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        userData.SetProperty<bool>("storepassword", false);
                        userData.SetProperty<bool>("loggedin", true);
                        Activity reply = activity.CreateReply($"You have logged in successfully!");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    
                }
                else if (userData.GetProperty<bool>("updatinginfo"))
                {

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
                            userData.SetProperty<bool>("updatinginfo", true);
                            reply = activity.CreateReply($"Please enter your ");
                        }
                        else
                        {
                            userData.SetProperty<bool>("storeusername", true);
                            reply = activity.CreateReply($"Please enter your username.");
                        }
                        

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