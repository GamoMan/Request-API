namespace Models.Constants
{
    public static class ResponseDesc
    {
        //////////////////////////////////////////////
        //1002	access token is empty or invalid


        public const string RES_CODE_SUCCESS = "1000";
        public const string RES_DESC_SUCCESS = "success";

        public const string RES_CODE_CAPTCHA_EMPTY = "1001";
        public const string RES_DESC_CAPTCHA_EMPTY = "captcha is empty or invalid";

        public const string RES_CODE_CAPTCHACODE_EMPTY = "1002";
        public const string RES_DESC_CAPTCHACODE_EMPTY = "captcha or session is empty or invalid";

        public const string RES_CODE_TOKEN_EMPTY = "1003";
        public const string RES_DESC_TOKEN_EMPTY = "token is empty or invalid";

        public const string RES_CODE_GENERAL_ERROR = "2000";
        public const string RES_DESC_GENERAL_ERROR = "general error";

        public const string RES_CODE_NOT_GET_DATA_DB = "2001";
        public const string RES_DESC_NOT_GET_DATA_DB = "can not reteive data from database";

        public const string RES_CODE_DATA_NOT_SAVE_DB = "2002";
        public const string RES_DESC_DATA_NOT_SAVE_DB = "data is not save to database";

        public const string RES_CODE_MODEL_INVALID = "2003";
        public const string RES_DESC_MODEL_INVALID = "model invalid";

        public const string RES_CODE_REQUEST_INVALID = "2004";
        public const string RES_DESC_REQUEST_INVALID = "model request invalid";

        public const string RES_CODE_EX_ERROR = "8001";
        public const string RES_DESC_EX_ERROR = "external api return error";
    }

}
