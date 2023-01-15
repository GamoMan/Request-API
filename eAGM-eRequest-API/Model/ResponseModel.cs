using Models.Constants;

namespace Models.JsonResponse
{
    public class ResponseModel<TResult>
    {
        public string? Result { get; set; } = null;
        public string? Code { get; set; } = null;
        public string? Description { get; set; } = null;

        public TResult? Data { get; set; } = default(TResult);

        public ResponseModel(string? result, TResult? data = default(TResult), string? code = null, string? description = null)
        {
            this.Result = result;
            this.Data = data;
            this.Code = code;
            this.Description = description;
        }

        public ResponseModel(ResponseStatus result, TResult? data = default(TResult), string? code = null, string? description = null)
            : this(result.ToString(), data, code, description) { }

        public static ResponseModel<TResult> Success(TResult? data = default(TResult), string? code = null, string? description = null)
        {
            return new ResponseModel<TResult>(ResponseStatus.Success, data, code, description);
        }

        public static ResponseModel<TResult> Error(string code = "Error", string? description = null)
        {
            return new ResponseModel<TResult>(ResponseStatus.Error, code: code, description: description);
        }

    }
}
