using CoreBotAzureBootcampDemo.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBotAzureBootcampDemo.Dialogs
{
    public class AddQuestionDialog : ComponentDialog
    {
        private readonly IConfiguration _configuration;
        private readonly IQnAMakerService _qnaMakerService;

        public AddQuestionDialog(IConfiguration configuration, IQnAMakerService qnaMakerService) : base(nameof(AddQuestionDialog))
        {
            _qnaMakerService = qnaMakerService;
            _configuration = configuration;

            var steps = new WaterfallStep[]
            {
                QuestionStepAsync,
                AnswerStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> QuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the question.")
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> AnswerStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Question"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the answer.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Answer"] = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The following data will be added to your knowledge base. \n\n" +
                $"**Question**:  {stepContext.Values["Question"]} \n\n" +
                $"**Answer**: {stepContext.Values["Answer"]}"), cancellationToken);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Is everything correct?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await _qnaMakerService.CreateQuestion(stepContext.Values["Question"].ToString(), stepContext.Values["Answer"].ToString());

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The following data has been added to your knowledge base. \n\n" +
                    $"**Question**:  {stepContext.Values["Question"]} \n\n" +
                    $"**Answer**: {stepContext.Values["Answer"]}"), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
    }
}
