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
using System.Globalization;
using System.Collections.Generic;
using Contoso_Bot.DataModels;
using Contoso_Bot.Models;
using System.Reflection;
using System.Threading;
using System.Security.Cryptography;

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
                //generalhelp
                if (activity.Text.ToLower() == "help")
                {
                    Activity reply = activity.CreateReply($"I can tell you about the current exchange rates, accounts, cards, kiwisaver, loans, mortgages and term deposits. \n I can also do this.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //cancelapplicationreset *editrequired
                else if (activity.Text.ToLower() == "cancel")
                {
                    if (!userData.GetProperty<bool>("loggedin"))
                    {
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    }
                    else
                    {
                        contosodb tempstore = userData.GetProperty<contosodb>("userinfo");
                        await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                        userData.SetProperty<contosodb>("userinfo", tempstore);
                        userData.SetProperty<bool>("loggedin", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }
                    Activity reply = activity.CreateReply($"Application Cancelled.");
                    await connector.Conversations.ReplyToActivityAsync(reply);

                }
                //logout (userdatawipe)
                else if (activity.Text.ToLower() =="logout" || activity.Text.ToLower() == "log out")
                {
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    Activity reply = activity.CreateReply($"You have logged out successfully.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquire Username (login step 1)
                else if (userData.GetProperty<bool>("takeusername"))
                {
                    userData.SetProperty<string>("username", activity.Text);
                    userData.SetProperty<bool>("takeusername", false);
                    userData.SetProperty<bool>("takepassword", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Please enter your password.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquire Password (login step2)
                else if (userData.GetProperty<bool>("takepassword"))
                {
                    userData.SetProperty<string>("password", activity.Text);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    contosodb logininfo = new contosodb()
                    {
                        username = userData.GetProperty<string>("username"),
                        password = userData.GetProperty<string>("password")
                    };
                    logininfo = await AzureManager.AzureManagerInstance.getuserinfo(logininfo);

                    if (logininfo.username != userData.GetProperty<string>("username"))
                    {
                        userData.SetProperty<bool>("takeusername", true);
                        userData.SetProperty<bool>("takepassword", false);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"Wrong credentials please try again.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        userData.SetProperty<contosodb>("userinfo", logininfo);
                        userData.SetProperty<string>("name", logininfo.name);
                        userData.SetProperty<string>("username", "");
                        userData.SetProperty<string>("password", "");
                        userData.SetProperty<bool>("takepassword", false);
                        userData.SetProperty<bool>("loggedin", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"Congratulations {userData.GetProperty<string>("name")}, you have logged in successfully!");
                        await connector.Conversations.SendToConversationAsync(reply);
                        //continue updateinfo dialog
                        if (userData.GetProperty<bool>("updatinginfo"))
                        {
                            List<intent.Entity> entities = userData.GetProperty<List<intent.Entity>>("updateinfolist");
                            if (entities.Count() == 2)
                            {
                                string infocategory = entities[0].type.Substring(9);
                                contosodb edit = userData.GetProperty<contosodb>("userinfo");
                                bool error = false;
                                if (infocategory == "address")
                                {
                                    edit.address = entities[0].entity;
                                }
                                else if (infocategory == "email")
                                {
                                    edit.email = entities[0].entity;
                                }
                                else if (infocategory == "phone")
                                {
                                    edit.phone = entities[0].entity;
                                }
                                else if (infocategory == "password")
                                {
                                    edit.password = entities[0].entity;
                                }
                                else
                                {
                                    error = true;
                                    reply = activity.CreateReply($"Error: infocategory issue please contact dev for bugfix.");
                                }
                                if (!error)
                                {
                                    await AzureManager.AzureManagerInstance.updateuserinfo(edit);
                                    userData.SetProperty<contosodb>("userinfo", edit);
                                    userData.SetProperty<bool>("updatinginfo", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    reply = activity.CreateReply($"Your {entities[1].entity} has been successfully changed to {entities[0].entity}!");
                                }
                            }
                            else
                            {
                                string detail = entities[0].entity;
                                reply = activity.CreateReply($"Please include your new {detail} in your request. i.e. I want to change my {detail} to [new {detail}]");
                            }
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }

                }
                //new user confirmation
                else if (userData.GetProperty<bool>("signupconf"))
                {
                    if (activity.Text.ToLower() == "yes" || activity.Text.ToLower() == "yeah" || activity.Text.ToLower() == "yup")
                    {
                        userData.SetProperty<bool>("register1", true);
                        userData.SetProperty<bool>("signupconf", false);
                        userData.SetProperty<bool>("opennewaccount", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"Thank you for choosing Contoso Bank. \n\nI will now begin filling your application form by asking you a series of questions. \n\n*Please remember you may type _'cancel'_ at any stage should you change your mind.* \n\nTo begin, do you know which type of account you want to apply for?");
                        await connector.Conversations.ReplyToActivityAsync(reply);

                    }
                    else if (activity.Text.ToLower() == "no" || activity.Text.ToLower() == "nah" || activity.Text.ToLower() == "nope")
                    {
                        userData.SetProperty<bool>("signupconf", false);
                        userData.SetProperty<bool>("storeusername", true);
                        userData.SetProperty<bool>("opennewaccount", true);

                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"Then please provide your username.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        Activity reply = activity.CreateReply($"Sorry I didn't understand that. Valid Answers are yes or no.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }
                //Choosing Account Type
                else if (userData.GetProperty<bool>("register1"))
                {
                    if (activity.Text.ToLower() == "no" || activity.Text.ToLower() == "nah" || activity.Text.ToLower() == "nope" || activity.Text.ToLower() == "i don't know")
                    {
                        userData.SetProperty<bool>("register1", false);
                        userData.SetProperty<bool>("register1.5", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity replyToConversation = activity.CreateReply();
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();
                        replyToConversation.AttachmentLayout = "carousel";
                        //first card
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "https://i.gyazo.com/9ea9995e5673b28d64d3b2215e99cb51.png"));
                        List<CardAction> cardButtons = new List<CardAction>();
                        CardAction plButton = new CardAction()
                        {
                            Value = "http://Contoso.com",
                            Type = "openUrl",
                            Title = "More Details"
                        };
                        cardButtons.Add(plButton);
                        CardAction pickButton = new CardAction()
                        {
                            Value = "Simple Banking Account",
                            Type = "imBack",
                            Title = "Select"
                        };
                        cardButtons.Add(pickButton);
                        ThumbnailCard plCard = new ThumbnailCard()
                        {
                            Title = "Contoso Simple Banking Account",
                            Subtitle = "This is a transactional account with the lowest fees guaranteed.",
                            Images = cardImages,
                            Buttons = cardButtons
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                        //Second card
                        List<CardImage> cardImages2 = new List<CardImage>();
                        cardImages2.Add(new CardImage(url: "https://i.gyazo.com/93d38f7b620c125057c561420dda5f25.png"));
                        List<CardAction> cardButtons2 = new List<CardAction>();
                        CardAction plButton2 = new CardAction()
                        {
                            Value = "http://Contoso.com",
                            Type = "openUrl",
                            Title = "More Details"
                        };
                        cardButtons2.Add(plButton);
                        CardAction pickButton2 = new CardAction()
                        {
                            Value = "Simple Saving Account",
                            Type = "imBack",
                            Title = "Select"
                        };
                        cardButtons2.Add(pickButton2);
                        ThumbnailCard plCard2 = new ThumbnailCard()
                        {
                            Title = "Contoso Simple Saving Account",
                            Subtitle = "This savings account provides great interest for your savings!",
                            Images = cardImages2,
                            Buttons = cardButtons2
                        };
                        Attachment plAttachment2 = plCard2.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment2);
                        await connector.Conversations.SendToConversationAsync(replyToConversation);
                    }
                    else if (activity.Text.ToLower() == "yes" || activity.Text.ToLower() == "yeah" || activity.Text.ToLower() == "yup")
                    {
                        userData.SetProperty<bool>("register1", false);
                        userData.SetProperty<bool>("register1.5", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"Please enter the name of your desired account type.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else if (activity.Text.ToLower().Contains("savings") ^ activity.Text.ToLower().Contains("savings"))
                    {
                        userData.SetProperty<string>("account", string.Concat(activity.Text));
                        userData.SetProperty<bool>("register1", false);
                        userData.SetProperty<bool>("register2", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"May I please have your name?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        Activity reply = activity.CreateReply($"Sorry I didn't understand that, please tell me 'no' if you don't know which account type, or type in the account name if you do.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }

                }
                //Account type second stage
                else if (userData.GetProperty<bool>("register1.5"))
                {
                    if (activity.Text.ToLower().Contains("saving") ^ activity.Text.ToLower().Contains("banking"))
                    {
                        userData.SetProperty<string>("account", string.Concat(activity.Text.ToLower() + ", (0)"));
                        userData.SetProperty<bool>("register1.5", false);
                        userData.SetProperty<bool>("register2", true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        Activity reply = activity.CreateReply($"May I please have your name?");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        Activity reply = activity.CreateReply($"Sorry I didn't understand that, please choose between either banking or saving.");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }

                //Acquiring name *NEED TO ADD REGISTER1 
                else if (userData.GetProperty<bool>("register2"))
                {
                    CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                    TextInfo textInfo = cultureInfo.TextInfo;
                    userData.SetProperty<string>("name", textInfo.ToTitleCase(activity.Text));
                    userData.SetProperty<bool>("register2", false);
                    userData.SetProperty<bool>("register3", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Thank you, {userData.GetProperty<string>("name")}. Now please type in your contact phone number.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquiring phone
                else if (userData.GetProperty<bool>("register3"))
                {
                    userData.SetProperty<string>("phonenum", activity.Text);
                    userData.SetProperty<bool>("register3", false);
                    userData.SetProperty<bool>("register4", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Your current address in the format of street, suburb, town is?");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquiring address
                else if (userData.GetProperty<bool>("register4"))
                {
                    userData.SetProperty<string>("address", activity.Text);
                    userData.SetProperty<bool>("register4", false);
                    userData.SetProperty<bool>("register5", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Next I'll need your email address please.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquiring email
                else if (userData.GetProperty<bool>("register5"))
                {
                    userData.SetProperty<string>("email", activity.Text);
                    userData.SetProperty<bool>("register5", false);
                    userData.SetProperty<bool>("register6", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Lastly, please choose a password.");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                //Acquiring password + proccessing form
                else if (userData.GetProperty<bool>("register6"))
                {
                    string password = activity.Text;

                    //USERNAME GENERATION
                    string username = security.genuser();
                    username = await AzureManager.AzureManagerInstance.uniqueusernamegenerate(username);
                    //SALT GENERATION
                    string salt = security.gensalt();
                    //HASH GENERATION
                    string hash = security.genhash(password, salt);

                    //dataentry
                    contosodb newentry = new contosodb()
                    {
                        username = username,
                        password = hash,
                        salt = salt,
                        name = userData.GetProperty<string>("name"),
                        phone = userData.GetProperty<string>("phonenum"),
                        email = userData.GetProperty<string>("email"),
                        address = userData.GetProperty<string>("address"),
                        account = userData.GetProperty<string>("account")
                    };
                    await AzureManager.AzureManagerInstance.adduser(newentry);
                    contosodb updated = await AzureManager.AzureManagerInstance.getuserinfo(newentry);
                    userData.SetProperty<bool>("register6", false);
                    userData.SetProperty<bool>("loggedin", true);
                    userData.SetProperty<contosodb>("userinfo", updated);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    Activity reply = activity.CreateReply($"Congratulations, your account has been set up! Your unique user ID is {username}, you'll need it to log in to your account so please make sure you remember it.");
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
                            string basecurrency = "";
                            string tocurrency = "";
                            for (int i = 0; i < entities.Count(); i++)
                            {
                                if (entities[i].type == "exchangerate::fromcurrency")
                                {
                                    basecurrency = entities[i].entity;
                                }
                                if (entities[i].type == "exchangerate::tocurrency")
                                {
                                    tocurrency = entities[i].entity;
                                }
                            }
                            exchangerate.RootObject exchangerateObject;
                            HttpClient exchangerateclient = new HttpClient();
                            string rate = await exchangerateclient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + basecurrency));
                            exchangerateObject = JsonConvert.DeserializeObject<exchangerate.RootObject>(rate);
                            string tocurrencyrate = "";

                            foreach (var prop in exchangerateObject.rates.GetType().GetProperties())
                            {
                                if (prop.Name.ToLower() == tocurrency.ToLower())
                                {
                                    tocurrencyrate = prop.GetValue(exchangerateObject.rates, null).ToString();

                                }
                            }
                            reply = activity.CreateReply($"The current exchange rate for {basecurrency} to {tocurrency} is {tocurrencyrate}.");
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
                        List<intent.Entity> entities = rootObject.entities;
                        if (entities[0].type == "account")
                        {
                            if (!userData.GetProperty<bool>("loggedin"))
                            {
                                userData.SetProperty<bool>("signupconf", true);
                                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                reply = activity.CreateReply($"Is this your first account with Contoso?");
                            }
                        }
                    }

                    //intent: updateinfo
                    if (intent == "updateinfo")
                    {
                        if (userData.GetProperty<bool>("loggedin"))
                        {
                            List<intent.Entity> entities = rootObject.entities;
                            if (entities.Count() == 2)
                            {
                                string infocategory = entities[0].type.Substring(9);
                                contosodb edit = userData.GetProperty<contosodb>("userinfo");
                                bool error = false;
                                if (infocategory == "address")
                                {
                                    edit.address = entities[0].entity;
                                }
                                else if (infocategory == "email")
                                {
                                    edit.email = entities[0].entity;
                                }
                                else if (infocategory == "phone")
                                {
                                    edit.phone = entities[0].entity;
                                }
                                else if (infocategory == "password")
                                {
                                    edit.password = entities[0].entity;
                                }
                                else
                                {
                                    error = true;
                                    reply = activity.CreateReply($"Error: infocategory issue please contact dev for bugfix.");
                                }
                                if (!error)
                                {
                                    await AzureManager.AzureManagerInstance.updateuserinfo(edit);
                                    userData.SetProperty<contosodb>("userinfo", edit);
                                    userData.SetProperty<bool>("updatinginfo", false);
                                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                                    reply = activity.CreateReply($"Your {entities[1].entity} has been successfully changed to {entities[0].entity}!");
                                }
                            }
                            else
                            {
                                string detail = entities[0].entity;
                                reply = activity.CreateReply($"Please include your new {detail} in your request. i.e. I want to change my {detail} to [new {detail}]");

                            }

                        }
                        else
                        {
                            userData.SetProperty<bool>("updatinginfo", true);
                            List<intent.Entity> entities = rootObject.entities;
                            userData.SetProperty<List<intent.Entity>>("updateinfolist", entities);
                            userData.SetProperty<bool>("takeusername", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            reply = activity.CreateReply($"Please enter your username.");
                        }


                    }

                    //intent: disablecard
                    if (intent == "disablecard")
                    {
                        if (userData.GetProperty<bool>("loggedin"))
                        {
                            userData.SetProperty<bool>("disablingcard", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            reply = activity.CreateReply($"Please enter your ");
                        }
                        else
                        {
                            userData.SetProperty<bool>("storeusername", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            reply = activity.CreateReply($"Please enter your username.");
                        }
                    }

                    //intent: close
                    if (intent == "close")
                    {
                        if (userData.GetProperty<bool>("loggedin"))
                        {
                            userData.SetProperty<bool>("closing", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            reply = activity.CreateReply($"Please enter your ");
                        }
                        else
                        {
                            userData.SetProperty<bool>("storeusername", true);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            reply = activity.CreateReply($"Please enter your username.");
                        }
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