using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;
using PegaTrade.Tests.Helpers;

namespace PegaTrade.Tests.Tests
{
    [TestClass]
    public class AccountsTests
    {
        public AccountsTests()
        {
            AppSettingsTestInit.Initialize();
        }

        [TestMethod]
        public void CreateUserTest()
        {
            ResultsItem deleteResult = AuthorizationLogic.DeletePegaUser("Test1234").Result;

            PegaUser user = new PegaUser
            {
                Username = "Test1234",
                Password = "Test1234",
                Email = "Test@gmail.com",
                IsSubscribeNewsLetter = true
            };
            ResultsPair<PegaUser> result = AuthorizationLogic.CreateNewUser(user).Result;
            Assert.IsTrue(result.Result.ResultType == Types.ResultsType.Successful);
        }
    }
}
