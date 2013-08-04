package domain
{
	[RemoteClass(alias="Product")]
	public class ProductDto
	{
		public var id:int;
		
		public var identity:String = "lol";
		
		public function ProductDto(id:int = 0)
		{
			this.id = id;
		}
	}
}