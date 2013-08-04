package domain
{
	[RemoteClass(alias="ExampleService.User")]
	public class User
	{
		public var id:int;
		
		public var is_active:Boolean;
		
		public var name:String;
		
		[ArrayElementType("domain.ProductDto")]
		public var products:Array;
		
		public function User(id:int = 0)
		{
			this.id = id;
		}
	}
}