namespace MyBGList_ApiVersion.DTO.v1
{
    public class RestDTO<T>
    {
        public List<LinkDTO> Links { get; set; } = new List<LinkDTO>();
        public T Data { get; set; }

        public RestDTO() { }
    }
}
