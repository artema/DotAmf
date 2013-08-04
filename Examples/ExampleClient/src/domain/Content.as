package domain
{
	import flash.utils.ByteArray;

	[RemoteClass(alias="ExampleService.Content")]
	public class Content
	{
		public var data:ByteArray;
		
		public var xml:XML;
	}
}