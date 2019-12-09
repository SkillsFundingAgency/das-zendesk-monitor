using FluentValidation;
using LanguageExt.Common;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZenWatchFunction
{
    public static class HttpRequestMessageExtentions
    {
        public static async Task<Result<TModel>> GetJsonBody<TModel, TValidator>(this HttpRequestMessage request) where TValidator : AbstractValidator<TModel>, new()
        {
            try
            {
                var content = await request.Content.ReadAsStringAsync();
                var ticket = JsonConvert.DeserializeObject<TModel>(content);

                var validator = new TValidator();
                var validationResult = validator.Validate(ticket);

                return validationResult.IsValid 
                    ? ticket 
                    : new Result<TModel>(new Exception(validationResult.ToString()));
            }
            catch (JsonException exception)
            {
                return new Result<TModel>(new Exception(exception.Message));
            }
        }

        public static HttpResponseMessage BadRequest(this HttpRequestMessage request, Exception exception) 
            => request.CreateErrorResponse(HttpStatusCode.BadRequest, exception.Message);
    }
}