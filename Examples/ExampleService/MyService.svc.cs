using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Xml;

namespace ExampleService
{
    public class MyService : IMyService
    {
        public ProductVo[] GetAllProducts()
        {
            return new[]
            {
                new ProductVo { Id = 1 },
                new ProductVo { Id = 2 },
                new ProductVo { Id = 3 }
            };
        }

        public User GetUserDataContract(int id)
        {
            return new User
                       {
                           Id = id,
                           IsActive = true,
                           name = "User #" + id,
                           Products = new[]
                           {
                               new ProductVo { Id = 1 },
                               new ProductVo { Id = 2 },
                               new ProductVo { Id = 3 }
                           }
                       };
        }

        public int AddUser(User user)
        {
            return user.Id;
        }

        public Content SendContent(Content content)
        {
            content.Data = content.Data.Reverse().ToArray();
            content.Xml = new XmlDocument();
            content.Xml.LoadXml("<root><node /></root>");

            return content;
        }

        public User[] SendGraph(User[] users)
        {
            return users;
        }

        public void DoStuff()
        {
            Thread.Sleep(1000);
        }

        public void DoFault()
        {
            throw new FaultException<CustomFault>(new CustomFault
                                                      {
                                                          Message = "Custom fault description.",
                                                          Date = DateTime.UtcNow
                                                      }, new FaultReason("Custom fault reason text."));
        }
    }
}
