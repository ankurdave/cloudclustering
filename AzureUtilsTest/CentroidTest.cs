using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for CentroidTest and is intended
    ///to contain all CentroidTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CentroidTest
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
        ///A test for Centroid Constructor
        ///</summary>
        [TestMethod()]
        public void CentroidConstructorTest()
        {
            Guid id = Guid.NewGuid();
            Point p = new Point
            {
                X = 1,
                Y = 2
            };
            Centroid target = new Centroid(id, p);

            Assert.AreEqual(target.X, p.X);
            Assert.AreEqual(target.Y, p.Y);
            Assert.AreEqual(target.ID, id);
        }

        /// <summary>
        ///A test for FromByteArray
        ///</summary>
        [TestMethod()]
        public void FromByteArrayTest()
        {
            Guid id = Guid.NewGuid();

            MemoryStream stream = new MemoryStream();
            stream.Write(id.ToByteArray(), 0, 16);
            stream.Write(BitConverter.GetBytes(2.53F), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(4.56F), 0, sizeof(float));
            byte[] bytes = stream.ToArray();

            Centroid expected = new Centroid
            {
                ID = id,
                X = 2.53F,
                Y = 4.56F
            };
            Centroid actual;
            actual = Centroid.FromByteArray(bytes);

            Assert.AreEqual(expected.ID, actual.ID);
            Assert.AreEqual(expected.X, actual.X);
            Assert.AreEqual(expected.Y, actual.Y);
        }

        /// <summary>
        ///A test for ToByteArray
        ///</summary>
        [TestMethod()]
        public void ToByteArrayTest()
        {
            Guid id = Guid.NewGuid();
            Centroid target = new Centroid
            {
                ID = id,
                X = 1.23F,
                Y = 3.45F
            };

            MemoryStream stream = new MemoryStream();
            stream.Write(id.ToByteArray(), 0, 16);
            stream.Write(BitConverter.GetBytes(1.23F), 0, sizeof(float));
            stream.Write(BitConverter.GetBytes(3.45F), 0, sizeof(float));
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
