namespace Utility.GenericRepository.CommonModels
{
    public class GetterResult<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; } = String.Empty;
        public T Data { get; set; }
    }
    public class GetterResultPaginated<T> : GetterResult<T>
    {
        public PaginatedResponse Page {  get; set; }   
    }

}
