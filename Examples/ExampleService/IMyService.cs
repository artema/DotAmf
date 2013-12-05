using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml;
using System.Xml.Linq;

namespace ExampleService
{
    [ServiceContract]
    public interface IMyService
    {
        [OperationContract]
        ProductVo[] GetAllProducts();

        [OperationContract(Name = "GetUser")] //Custom procedure name
        User GetUserDataContract(int id);

        [OperationContract]
        int AddUser(User user);

        [OperationContract]
        Content SendContent(Content content);

        [OperationContract]
        User[] SendGraph(User[] users);

        [OperationContract]
        void DoStuff();

        [OperationContract]
        [FaultContract(typeof(CustomFault))]
        void DoFault();
    }

    [DataContract] //Will have the "ExampleService.User" alias
    public class User
    {
        [DataMember(Name = "id")] //Custom field name
        public int Id { get; set; }

        [DataMember(Name = "is_active")]
        public bool IsActive { get; set; }

        [DataMember] //Use explicit name
        public string name { get; set; }

        [DataMember(Name = "products")]
        public ProductVo[] Products { get; set; }
    }

    [DataContract(Name = "Product")] //Custom alias
    public class ProductVo
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
    }

    [DataContract]
    public class CustomFault
    {
        [DataMember(Name = "date")]
        public DateTime Date { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }

    [DataContract]
    public class Content
    {
        [DataMember(Name = "data")]
        public byte[] Data { get; set; }

        [DataMember(Name = "xml")]
        public XDocument Xml { get; set; }
    }
}
