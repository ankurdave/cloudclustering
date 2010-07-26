using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;
using Microsoft.WindowsAzure;
using System.IO;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for AzureHelperTest and is intended
    ///to contain all AzureHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class AzureHelperTest
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
        ///A test for EnqueueMessage
        ///</summary>
        [TestMethod()]
        public void EnqueueWaitForMessageTest()
        {
            CloudQueue queue = AzureHelper.StorageAccount.CreateCloudQueueClient().GetQueueReference(AzureHelper.ServerRequestQueue);
            queue.CreateIfNotExist();
            queue.Clear();

            KMeansJobData message = new KMeansJobData(Guid.Empty, 1, null, 2, 3, 10, DateTime.Now);
            AzureHelper.EnqueueMessage(AzureHelper.ServerRequestQueue, message);

            Thread.Sleep(2000);

            KMeansJobData responseMessage = null;
            AzureHelper.WaitForMessage(AzureHelper.ServerRequestQueue, msg => true, msg =>
            {
                responseMessage = (KMeansJobData)msg;
                return true;
            }, 100, 100);

            Assert.AreEqual(message.JobID, responseMessage.JobID);
            Assert.AreEqual(message.K, responseMessage.K);
        }

        [TestMethod()]
        public void SimpleEnqueueDequeueTest()
        {
            CloudQueue queue = AzureHelper.StorageAccount.CreateCloudQueueClient().GetQueueReference(AzureHelper.ServerRequestQueue);
            queue.CreateIfNotExist();
            queue.Clear();

            Guid message = Guid.NewGuid();
            queue.AddMessage(new CloudQueueMessage(message.ToByteArray()));
            Thread.Sleep(2000);
            Guid received = new Guid(queue.GetMessage().AsBytes);

            Assert.AreEqual(message, received);
        }

        [TestMethod()]
        public void AzureMessageEnqueueDequeueTest()
        {
            CloudQueue queue = AzureHelper.StorageAccount.CreateCloudQueueClient().GetQueueReference(AzureHelper.ServerRequestQueue);
            queue.CreateIfNotExist();
            queue.Clear();

            AzureMessage message = new KMeansJobData(Guid.NewGuid(), 1, null, 2, 3, 10, DateTime.Now);
            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
            Thread.Sleep(2000);
            AzureMessage received = KMeansJobData.FromMessage<KMeansJobData>(queue.GetMessage());

            KMeansJobData messageCast = message as KMeansJobData,
                receivedCast = received as KMeansJobData;
            Assert.AreEqual(messageCast.JobID, receivedCast.JobID);
            Assert.AreEqual(messageCast.K, receivedCast.K);
            Assert.AreEqual(messageCast.M, receivedCast.M);
            Assert.AreEqual(messageCast.N, receivedCast.N);
        }

        [TestMethod()]
        public void GetBlobTest()
        {
            string containerName = "foo";
            string blobName = "bar";
            CloudBlobContainer container = AzureHelper.StorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            container.CreateIfNotExist();
            CloudBlob blob = container.GetBlobReference(blobName);
            using (BlobStream stream = blob.OpenWrite())
            {
                byte[] bytes = new System.Text.UTF8Encoding().GetBytes("hello world");
                stream.Write(bytes, 0, bytes.Length);
            }

            Assert.AreEqual(blob.Properties.Length, AzureHelper.GetBlob(blob.Uri).Properties.Length);
        }

        /// <summary>
        ///A test for CopyStreamUpToLimit where maxBytesToCopy is less than input.Length
        ///</summary>
        [TestMethod()]
        public void CopyStreamUpToLimitTest()
        {
            byte[] bytes = new byte[8];
            new Random().NextBytes(bytes);
            MemoryStream input = new MemoryStream(bytes);
            MemoryStream output = new MemoryStream();
            int maxBytesToCopy = 5;
            byte[] copyBuffer = new byte[2];
            AzureHelper_Accessor.CopyStreamUpToLimit(input, output, maxBytesToCopy, copyBuffer);

            int expectedOutputLength = Math.Min(maxBytesToCopy, bytes.Length);
            Assert.AreEqual(expectedOutputLength, output.Length);

            output.Position = 0;
            for (int i = 0; i < output.Length; i++)
            {
                Assert.AreEqual(bytes[i], output.ReadByte());
            }
        }

        /// <summary>
        ///A test for CopyStreamUpToLimit where maxBytesToCopy is greater than input.Length
        ///</summary>
        [TestMethod()]
        public void CopyStreamUpToLimitTest2()
        {
            byte[] bytes = new byte[8];
            new Random().NextBytes(bytes);
            MemoryStream input = new MemoryStream(bytes);
            MemoryStream output = new MemoryStream();
            int maxBytesToCopy = 100;
            byte[] copyBuffer = new byte[2];
            AzureHelper_Accessor.CopyStreamUpToLimit(input, output, maxBytesToCopy, copyBuffer);

            int expectedOutputLength = Math.Min(maxBytesToCopy, bytes.Length);
            Assert.AreEqual(expectedOutputLength, output.Length);

            output.Position = 0;
            for (int i = 0; i < output.Length; i++)
            {
                Assert.AreEqual(bytes[i], output.ReadByte());
            }
        }
    }
}
