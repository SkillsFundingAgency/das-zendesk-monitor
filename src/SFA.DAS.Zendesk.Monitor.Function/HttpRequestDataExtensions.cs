using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using LanguageExt.Common;
using Microsoft.Azure.Functions.Worker.Http;

namespace ZenWatchFunction
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<Result<TModel>> GetJsonBody<TModel, TValidator>(this HttpRequestData request) where TValidator : AbstractValidator<TModel>, new()
        {
            try
            {
                using var reader = new StreamReader(request.Body);
                var content = await reader.ReadToEndAsync();
                var model = JsonSerializer.Deserialize<TModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (model == null)
                {
                    return new Result<TModel>(new Exception("Deserialized model is null."));
                }

                var validator = new TValidator();
                var validationResult = validator.Validate(model);

                return validationResult.IsValid
                    ? model
                    : new Result<TModel>(new Exception(validationResult.ToString()));
            }
            catch (JsonException exception)
            {
                return new Result<TModel>(new Exception(exception.Message));
            }
        }

        public static HttpResponseData BadRequest(this HttpRequestData request, Exception exception) 
            => request.CreateErrorResponse(HttpStatusCode.BadRequest, exception.Message);

        public static HttpResponseData CreateErrorResponse(this HttpRequestData request, HttpStatusCode statusCode, string message)
        {
            var response = request.CreateResponse(statusCode);
            response.WriteString(message);
            return response;
        }
    }
}