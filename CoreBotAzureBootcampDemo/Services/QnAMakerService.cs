using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreBotAzureBootcampDemo.Services
{
    public interface IQnAMakerService
    {
        Task<QueryResult[]> GetQuestionResults(HttpClient httpClient, ITurnContext turnContext);
        Task CreateQuestion(string question, string answer);
    }

    public class QnAMakerService : IQnAMakerService
    {
        public readonly IConfiguration _configuration;

        public QnAMakerService(IConfiguration configuration)
        {
            ;
            _configuration = configuration;
        }
        public async Task<QueryResult[]> GetQuestionResults(HttpClient httpClient, ITurnContext turnContext)
        {
            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAEndpointKey"],
                Host = _configuration["QnAEndpointHostName"]
            }, null, httpClient);

            var options = new QnAMakerOptions { Top = 1 };
            return await qnaMaker.GetAnswersAsync(turnContext, options);
        }

        public async Task CreateQuestion(string question, string answer)
        {
            var url = $"https://{_configuration["QnAEndpointHostName"]}.cognitiveservices.azure.com";
            var client = new QnAMakerClient(new ApiKeyServiceClientCredentials(_configuration["QnAEndpointKey"])) { Endpoint = url };
            var update = await client.Knowledgebase.UpdateAsync(_configuration["QnAKnowledgebaseId"], new UpdateKbOperationDTO
            {
                Add = new UpdateKbOperationDTOAdd
                {
                    QnaList = new List<QnADTO> {
                        new QnADTO {
                            Questions = new List<string> { question },
                            Answer = answer,
                        }
                    },
                },
                Update = null,
                Delete = null
            }); ;

            await MonitorOperation(client, update);
            await client.Knowledgebase.PublishAsync(_configuration["QnAKnowledgebaseId"]);
        }

        private static async Task<Operation> MonitorOperation(QnAMakerClient client, Operation operation)
        {
            // Loop while operation is success
            for (int i = 0; i < 20 && (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running); i++)
            {
                Console.WriteLine("Waiting for operation: {0} to complete.", operation.OperationId);
                await Task.Delay(5000);
                operation = await client.Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
            {
                throw new Exception($"Operation {operation.OperationId} failed to completed.");
            }
            return operation;
        }
    }
}

