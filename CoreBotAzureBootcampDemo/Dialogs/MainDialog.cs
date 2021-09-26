// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.14.0

using AdaptiveCards;
using CoreBotAzureBootcampDemo.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBotAzureBootcampDemo.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        public MainDialog(ILogger<MainDialog> logger, 
                          IQnAMakerService qnaMakerService,
                          IHttpClientFactory httpClientFactory,
                          IConfiguration configuration) : base(nameof(MainDialog))
        {
            _configuration = configuration;
            _logger = logger;

            AddDialog(new AskQuestionDialog(_configuration, httpClientFactory, qnaMakerService));
            AddDialog(new AddQuestionDialog(_configuration, qnaMakerService));

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext?.Options;
            if (options == null) {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("What would you like to do today?"), cancellationToken);
            }

            var operationList = new List<string> { "Ask a question", "Contribute a question" };
           
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Actions = operationList.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  
                }).ToList<AdaptiveAction>(),
            };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JObject.FromObject(card),
                }),
                Choices = ChoiceFactory.ToChoices(operationList),
                Style = ListStyle.None,
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Operation"] = ((FoundChoice)stepContext.Result).Value;
            string operation = (string)stepContext.Values["Operation"];

            if (operation.Equals("Ask a question"))
            {
                return await stepContext.BeginDialogAsync(nameof(AskQuestionDialog), null, cancellationToken);
            }
            else if (operation.Equals("Contribute a question"))
            {
                return await stepContext.BeginDialogAsync(nameof(AddQuestionDialog), null, cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var text = "Is there anything else we can do for you?";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(text), cancellationToken);

            return await stepContext.ReplaceDialogAsync(nameof(MainDialog), new { }, cancellationToken);
        }
    }

}
