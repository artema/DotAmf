NuGet packages: `DotAmf` and `DotAmf.Amf`

# About #

.NET serializer and WCF bindings for AMF with full Flex compatibility.

> Action Message Format (AMF) is a binary format used to serialize object graphs such as ActionScript objects and XML, or send messages between an Adobe Flash client and a remote service, usually a Flash Media Server or third party alternatives. The Actionscript 3 language provides classes for encoding and decoding from the AMF format.
>
> [http://en.wikipedia.org/wiki/Action\_Message\_Format](http://en.wikipedia.org/wiki/Action_Message_Format)

# Configuration #

Consider the following service contracts:

        using System;
        using System.Runtime.Serialization;
        using System.ServiceModel;
        using System.Xml;

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
                public XmlDocument Xml { get; set; }
            }
        }

To add an AMF support to a service, you only need to update your configuration:

    <?xml version="1.0"?>
    <configuration>
      <system.web>
        <compilation debug="true" targetFramework="4.0" />
      </system.web>
      <system.serviceModel>
        <extensions>
          <behaviorExtensions>
            <add name="amfBehaviorExtension" type="DotAmf.ServiceModel.Configuration.AmfBehaviorExtensionElement, DotAmf.Wcf"/>
          </behaviorExtensions>
          <bindingElementExtensions>
            <add name="amfBindingExtension" type="DotAmf.ServiceModel.Configuration.AmfBindingExtensionElement, DotAmf.Wcf"/>
          </bindingElementExtensions>
        </extensions>
        <behaviors>
          <endpointBehaviors>
            <behavior name="amfEndpoint">
              <amfBehaviorExtension/>
            </behavior>
          </endpointBehaviors>
          <serviceBehaviors>
            <behavior name="amfServiceBehavior">
              <serviceMetadata httpGetEnabled="false"/>
              <serviceDebug includeExceptionDetailInFaults="true" httpHelpPageEnabled="false"/>
            </behavior>
          </serviceBehaviors>
        </behaviors>
        <bindings>
          <customBinding>
            <binding name="amfBinding">
              <amfBindingExtension/>
              <httpTransport/>
            </binding>
          </customBinding>
        </bindings>
        <services>
          <service name="ExampleService.MyService" behaviorConfiguration="amfServiceBehavior">
            <endpoint address="" contract="ExampleService.IMyService" binding="customBinding" bindingConfiguration="amfBinding" behaviorConfiguration="amfEndpoint"/>
          </service>
        </services>
        <serviceHostingEnvironment multipleSiteBindingsEnabled="true"/>
      </system.serviceModel>
     <system.webServer>
        <modules runAllManagedModulesForAllRequests="true"/>
        <directoryBrowse enabled="false"/>
      </system.webServer>
    </configuration>

See the `Examples` folder for a complete service and client implementations.
