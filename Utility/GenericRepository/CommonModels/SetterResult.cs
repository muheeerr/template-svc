namespace Utility.GenericRepository.CommonModels
{

    public class SetterResult
    {
        public bool Result { get; set; }
        public bool IsException { get; set; }
        public string Message { get; set; }

    }
    public class SetterWithDataResult : SetterResult
    {

        public Object Data { get; set; }


    }
}
