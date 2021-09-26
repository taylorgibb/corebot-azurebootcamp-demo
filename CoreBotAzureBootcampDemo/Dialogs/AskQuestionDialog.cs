using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Knowledge;
using Microsoft.Bot.Builder.AI.QnA;
using System.Net.Http;
using CoreBotAzureBootcampDemo.Services;

namespace CoreBotAzureBootcampDemo.Dialogs
{
    public class AskQuestionDialog : ComponentDialog
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IQnAMakerService _qnaMakerService;
        public AskQuestionDialog(IConfiguration configuration, 
            IHttpClientFactory httpClientFactory,
            IQnAMakerService qnaMakerService) : base(nameof(AskQuestionDialog))
        {
            _qnaMakerService = qnaMakerService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;

            var steps = new WaterfallStep[]
            {
                QuestionStepAsync,
                AnswerStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> QuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("How can i help you today?")

            }, cancellationToken);
        }
        private async Task<DialogTurnResult> AnswerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = await _qnaMakerService.GetQuestionResults(_httpClientFactory.CreateClient(), stepContext.Context);
            if (response != null && response.Length > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No answers were found for that question."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
