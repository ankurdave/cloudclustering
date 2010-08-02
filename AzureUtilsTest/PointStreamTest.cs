using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for PointStreamTest and is intended
    ///to contain all PointStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PointStreamTest
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
        ///A test for GetEnumerator (on ObjectStreamReader)
        ///</summary>
        [TestMethod()]
        public void EnumeratorTest()
        {
            ClusterPoint p = new ClusterPoint(1, 2, Guid.NewGuid());
            MemoryStream stream = new MemoryStream();
            const int NumElements = 20;
            for (int i = 0; i < NumElements; i++)
            {
                stream.Write(p.ToByteArray(), 0, ClusterPoint.Size);
            }

            ObjectStreamReader<ClusterPoint> pointStream = new ObjectStreamReader<ClusterPoint>(new MemoryStream(stream.ToArray()), ClusterPoint.FromByteArray, ClusterPoint.Size);
            Assert.AreEqual(p.CentroidID, pointStream.First().CentroidID);

            DateTime serialStart = DateTime.Now;
            int[] serialOutput = pointStream.Select(point =>
            {
                System.Threading.Thread.Sleep(1000);
                return 1;
            }).ToArray();
            DateTime serialEnd = DateTime.Now;
            Assert.AreEqual(NumElements, serialOutput.Length);

            DateTime parallelStart = DateTime.Now;
            int[] parallelOutput = pointStream.AsParallel().Select(point =>
            {
                System.Threading.Thread.Sleep(1000);
                return 1;
            }).ToArray();
            DateTime parallelEnd = DateTime.Now;
            Assert.AreEqual(NumElements, parallelOutput.Length);

            System.Diagnostics.Trace.WriteLine(string.Format("serial: {0}, parallel: {1}",
                (serialEnd - serialStart).TotalSeconds,
                (parallelEnd - parallelStart).TotalSeconds));
        }
    }
}
