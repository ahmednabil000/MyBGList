namespace MyBGList.DTO
{
	public class RestDTO<T>
	{
		public List<LinkDTO> Links { get; set; } = new List<LinkDTO>();
		public T Data { get; set; }
		public int? PageIndex { get; set; }
		public int? PageSize { get; set; }
		public int? RecordCount { get; set; }
		public RestDTO() { }
	}
}
