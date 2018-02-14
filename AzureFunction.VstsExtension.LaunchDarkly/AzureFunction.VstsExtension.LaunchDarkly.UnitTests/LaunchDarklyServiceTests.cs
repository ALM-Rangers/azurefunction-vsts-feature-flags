using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AzureFunction.VstsExtension.LaunchDarkly.UnitTests
{
    [TestClass]
    public class LaunchDarklyServiceTests
    {
        [TestMethod]
        public void CanHashTheUserKey()
        {
            //arrange
            string userkey = "84ea4845-57ba-4096-909f-zt67y8:mikaeltest";
            Environment.SetEnvironmentVariable("LaunchDarkly_SDK_Key", "sdk-59baef5c-3851-4fef");
            //act
            string hashkey = LaunchDarklyServices.GetHashKey(userkey);

            //assert
            Assert.IsTrue(hashkey == "44745db8726d4df864787ce328edf1dd25bfbe92234c5e0d753c8e13e429cdd6");
        }

        [TestMethod]
        public void CanFormatTheUserKey()
        {
            //arrange
            string userid = "84ea4845-57ba-4096-909f-zt67y8";
            string account = "84ea4845-57ba-4096-909f-zt67y8";

            //act
            string userkey = LaunchDarklyServices.FormatUserKey(userid, account);

            //assert
            Assert.IsTrue(userkey == "84ea4845-57ba-4096-909f-zt67y8:84ea4845-57ba-4096-909f-zt67y8");
        }

        [TestMethod]
        public void CanGetEnvironmentVariable()
        {
            //arrange
            Environment.SetEnvironmentVariable("LaunchDarkly_SDK_Key", "sdk-59baef5c-3851-4fef");
            //act
            string environVar = Helpers.GetEnvironmentVariable("LaunchDarkly_SDK_Key");
            //assert
            Assert.IsTrue(environVar == "sdk-59baef5c-3851-4fef");
        }


        [TestMethod]
        public void CanGetTheApiVersionInHeader()
        {
            HttpRequestMessage req = new HttpRequestMessage();
            req.Headers.Add("api-version", "2");

            int apiversion = Helpers.GetHeaderValue(req, "api-version");

            Assert.IsTrue(apiversion == 2);
        }

        [TestMethod]
        public void CanGetExtCertificatEnvName()
        {
            string certnamev1 = Helpers.GetExtCertificatEnvName("myCertExtname", 1);
            string certnamev2 = Helpers.GetExtCertificatEnvName("myCertExtname", 2);

            string certnamevmin = Helpers.GetExtCertificatEnvName("myCertExtname", 2, 1);
            string certnamevmin2 = Helpers.GetExtCertificatEnvName("myCertExtname", 2, 3);


            Assert.IsTrue(certnamev1 == "RollUpBoard_ExtensionCertificate");
            Assert.IsTrue(certnamev2 == "myCertExtname");
            Assert.IsTrue(certnamevmin == "myCertExtname");
            Assert.IsTrue(certnamevmin2 == "RollUpBoard_ExtensionCertificate");
        }
    }
}
