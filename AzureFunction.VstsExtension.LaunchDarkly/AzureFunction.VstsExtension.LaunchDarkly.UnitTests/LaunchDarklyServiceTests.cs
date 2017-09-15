using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureFunction.VstsExtension.LaunchDarkly;


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
            string account = "mikaeltest";

            //act
            string userkey = LaunchDarklyServices.FormatUserKey(userid, account);
            
            //assert
            Assert.IsTrue(userkey == "84ea4845-57ba-4096-909f-zt67y8:mikaeltest");
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
    }
}
