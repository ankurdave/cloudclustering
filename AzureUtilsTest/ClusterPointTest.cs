using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for ClusterPointTest and is intended
    ///to contain all ClusterPointTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ClusterPointTest
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
        ///A test for ClusterPoint Constructor
        ///</summary>
        [TestMethod()]
        public void ClusterPointConstructorTest()
        {
            Point p = new Point
            {
                X = 3.25F,
                Y = 4.12F
            };
            Guid centroidID = Guid.NewGuid();
            
            ClusterPoint target = new ClusterPoint(p, centroidID);

            Assert.AreEqual(p.X, target.X);
            Assert.AreEqual(p.Y, target.Y);
            Assert.AreEqual(centroidID, target.CentroidID);
        }

        /// <summary>
        ///A test for FromByteArray
        ///</summary>
        [TestMethod()]
        public void FromByteArrayTest()
        {
            Guid centroidID = Guid.NewGuid();

            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(2.53), 0, sizeof(double));
            stream.Write(BitConverter.GetBytes(4.56), 0, sizeof(double));
            stream.Write(centroidID.ToByteArray(), 0, 16);
            byte[] bytes = stream.ToArray();

            ClusterPoint expected = new ClusterPoint
            {
                X = 2.53F,
                Y = 4.56F,
                CentroidID = centroidID
            };
            ClusterPoint actual;
            actual = ClusterPoint.FromByteArray(bytes);

            const double Epsilon = 0.0001;
            Assert.IsTrue(Math.Abs(expected.X - actual.X) < Epsilon);
            Assert.IsTrue(Math.Abs(expected.Y - actual.Y) < Epsilon);
            Assert.AreEqual(expected.CentroidID, actual.CentroidID);
        }

        /// <summary>
        ///A test for ToByteArray
        ///</summary>
        [TestMethod()]
        public void ToByteArrayTest()
        {
            Guid centroidID = Guid.NewGuid();
            ClusterPoint target = new ClusterPoint
            {
                X = 1.23,
                Y = 3.45,
                CentroidID = centroidID
            };

            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(1.23), 0, sizeof(double));
            stream.Write(BitConverter.GetBytes(3.45), 0, sizeof(double));
            stream.Write(centroidID.ToByteArray(), 0, 16);
            byte[] expected = stream.ToArray();

            byte[] actual;
            actual = target.ToByteArray();
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
