using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for ObjectStreamReaderTest and is intended
    ///to contain all ObjectStreamReaderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ObjectStreamReaderTest
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
        ///A test for CalculateReadBoundaries
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void CalculateReadBoundariesTest()
        {
            Range<long> readRange = ObjectStreamReader_Accessor<ClusterPoint>.CalculateReadBoundaries(
                streamLength: 100,
                objectSize: 2,
                partitionNumber: 1,
                totalPartitions: 10,
                subPartitionNumber: 2,
                subTotalPartitions: 3);

            Assert.AreEqual(new Range<long>(18, 20), readRange);
        }
    }
}
