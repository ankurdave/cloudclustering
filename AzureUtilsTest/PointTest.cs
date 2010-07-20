using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for PointTest and is intended
    ///to contain all PointTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PointTest
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
        ///A test for op_Division
        ///</summary>
        [TestMethod()]
        public void op_DivisionTest()
        {
            Point p = new Point
            {
                X = 10.92,
                Y = 7.35
            };
            double a = 2.1;
            Point expected = new Point
            {
                X = 10.92 / 2.1,
                Y = 7.35 / 2.1
            };
            Point actual;
            actual = (p / a);
            const double Epsilon = 0.0001;
            Assert.IsTrue(Math.Abs(expected.X - actual.X) < Epsilon);
            Assert.IsTrue(Math.Abs(expected.Y - actual.Y) < Epsilon);
        }

        /// <summary>
        ///A test for op_Addition
        ///</summary>
        [TestMethod()]
        public void op_AdditionTest()
        {
            const double Epsilon = 0.0001;

            Point p1 = new Point
            {
                X = 10.92,
                Y = 7.35
            };
            Point p2 = new Point
            {
                X = 4.35,
                Y = 1.12
            };
            Point expected = new Point
            {
                X = 15.27,
                Y = 8.47
            };
            Point actual;
            actual = (p1 + p2);
            Assert.IsTrue(Math.Abs(expected.X - actual.X) < Epsilon);
            Assert.IsTrue(Math.Abs(expected.Y - actual.Y) < Epsilon);
        }

        /// <summary>
        ///A test for FromByteArray
        ///</summary>
        [TestMethod()]
        public void FromByteArrayTest()
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(2.53), 0, sizeof(double));
            stream.Write(BitConverter.GetBytes(4.56), 0, sizeof(double));
            byte[] bytes = stream.ToArray();

            Point expected = new Point
            {
                X = 2.53,
                Y = 4.56
            };
            Point actual;
            actual = Point.FromByteArray(bytes);

            const double Epsilon = 0.0001;
            Assert.IsTrue(Math.Abs(expected.X - actual.X) < Epsilon);
            Assert.IsTrue(Math.Abs(expected.Y - actual.Y) < Epsilon);
        }

        /// <summary>
        ///A test for ToByteArray
        ///</summary>
        [TestMethod()]
        public void ToByteArrayTest()
        {
            Point target = new Point
            {
                X = 1.23,
                Y = 2.34
            };
            
            byte[] actual;
            actual = target.ToByteArray();

            MemoryStream stream = new MemoryStream();
            stream.Write(BitConverter.GetBytes(1.23), 0, sizeof(double));
            stream.Write(BitConverter.GetBytes(2.34), 0, sizeof(double));
            byte[] expected = stream.ToArray();

            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
