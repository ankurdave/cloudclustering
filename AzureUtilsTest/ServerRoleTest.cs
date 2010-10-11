using AKMServerRole;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AzureUtils;
using System.Collections.Generic;
using System.Linq;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for ServerRoleTest and is intended
    ///to contain all ServerRoleTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ServerRoleTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for RegroupWorkers
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AKMServerRole.dll")]
        public void RegroupWorkersTest()
        {
            ServerRole_Accessor target = new ServerRole_Accessor();
            List<int> faultDomains = new List<int> { 1, 1, 2, 2, 2 };
            List<string> expectedBuddyGroups = new List<string> { "1", "1", "1", "2", "2" };

            IEnumerable<Worker> workers = faultDomains.Select(fd => new Worker(Guid.NewGuid().ToString(), null, fd));
            int currentBuddyGroup = 1;
            List<string> actualBuddyGroups = target.RegroupWorkers(workers, () => currentBuddyGroup++.ToString())
                .Select(worker => worker.RowKey).ToList();

            CollectionAssert.AreEqual(expectedBuddyGroups, actualBuddyGroups);
        }
    }
}
