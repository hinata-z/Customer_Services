namespace Customer.WebApi
{
    public class BaseResponse
    {
        public static int SuccessCode = 200;
        private static int ErrorCode = 500;
        public BaseResponse(int code, object data, string msg)
        {
            this.data = data;
            this.code = code;
            this.msg = msg;
            this.otherData = null;
        }
        public int code { set; get; }
        public string? msg { set; get; }
        public object data { set; get; }

        public object otherData { get; set; }

        public int totalPage { get; set; }


        public int SetTotalPage(int totalCount, int pageSize)
        {
            totalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            return totalPage;
        }
        public static BaseResponse Success()
        {
            return new BaseResponse(SuccessCode, null, null);
        }
        public static BaseResponse Success(Object data)
        {
            return new BaseResponse(SuccessCode, data, null);
        }
        public static BaseResponse Success(string msg, Object data)
        {
            return new BaseResponse(SuccessCode, data, msg);
        }

        public static BaseResponse Error()
        {
            return new BaseResponse(ErrorCode, null, null);
        }
        public static BaseResponse Error(int ErrorCode, string msg)
        {
            return new BaseResponse(ErrorCode, null, msg);
        }

        public static BaseResponse Error(string msg)
        {
            return new BaseResponse(ErrorCode, null, msg);
        }
    }
}
