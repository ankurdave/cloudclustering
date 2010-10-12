using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Threading;
using Microsoft.WindowsAzure;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        ///A test for EnqueueMessage and PollForMessage
        ///</summary>
        [TestMethod()]
        public void EnqueueMessagePollForMessageTest()
        {
            string queueName = Guid.NewGuid().ToString();
            KMeansJobData message = new KMeansJobData(Guid.NewGuid(), 1, null, 2, 10, DateTime.Now);
            bool async = false;

            AzureHelper.EnqueueMessage(queueName, message, async);

            KMeansJobData foundMessage = null;
            AzureHelper.ExponentialBackoff(() =>
                AzureHelper.PollForMessage<KMeansJobData>(queueName, msg =>
                {
                    foundMessage = msg;
                    return true;
                }),
                firstDelayMilliseconds:100,
                backoffFactor:2,
                maxDelay:1000,
                retryLimit:5
            );

            Assert.AreNotEqual(null, foundMessage);

            Assert.AreEqual(message.JobID, foundMessage.JobID);
            Assert.AreEqual(message.K, foundMessage.K);
        }

        [TestMethod()]
        public void CreateBlobGetBlobTest()
        {
            string containerName = Guid.NewGuid().ToString();
            string blobName = Guid.NewGuid().ToString();
            
            CloudBlockBlob blob = AzureHelper.CreateBlob(containerName, blobName);

            byte[] bytes = new byte[] { 1, 2, 3 };
            using (Stream stream = blob.OpenWrite())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            CloudBlockBlob foundBlob = AzureHelper.GetBlob(blob.Uri);
            using (Stream stream = foundBlob.OpenRead())
            {
                foreach (int b in bytes)
                {
                    Assert.AreEqual(b, stream.ReadByte());
                }
            }
        }

        [TestMethod()]
        public void AzureMessageEnqueueDequeueTest()
        {
            CloudQueue queue = AzureHelper.StorageAccount.CreateCloudQueueClient().GetQueueReference(AzureHelper.ServerRequestQueue);
            queue.CreateIfNotExist();
            queue.Clear();

            AzureMessage message = new KMeansJobData(Guid.NewGuid(), 1, null, 2, 10, DateTime.Now);
            queue.AddMessage(new CloudQueueMessage(message.ToBinary()));
            Thread.Sleep(2000);
            AzureMessage received = KMeansJobData.FromMessage(queue.GetMessage());

            KMeansJobData messageCast = message as KMeansJobData,
                receivedCast = received as KMeansJobData;
            Assert.AreEqual(messageCast.JobID, receivedCast.JobID);
            Assert.AreEqual(messageCast.K, receivedCast.K);
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

        [TestMethod()]
        public void PartitionLengthTest()
        {
            int numElements = 5, numPartitions = 2, expectedPartitionLength = 3;
            Assert.AreEqual(expectedPartitionLength,
                AzureHelper.PartitionLength(numElements, numPartitions));
        }

        [TestMethod()]
        public void SliceTest()
        {
            List<int> sequence = new List<int> { 1, 2, 3, 4, 5 };
            int maxItemsPerSlice = 2;
            IEnumerable<IEnumerable<int>> expected = new List<List<int>> {
                new List<int> { 1, 2 },
                new List<int> { 3, 4 },
                new List<int> { 5 }
            };
            var actual = sequence.Slice(maxItemsPerSlice).ToList();

            Assert.AreEqual(actual[0].Count(), 2);
            Assert.AreEqual(actual[1].Count(), 2);
            Assert.AreEqual(actual[2].Count(), 1);
        }

        [TestMethod()]
        public void SliceIntoTest()
        {
            List<int> sequence = new List<int> { 1, 2, 3, 4, 5 };
            int numSlices = 2;
            var expected = new List<List<int>> {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5 }
            };
            var actual = sequence.SliceInto(numSlices).ToList();

            Assert.AreEqual(actual[0].Count(), 3);
            Assert.AreEqual(actual[1].Count(), 2);
        }

        [TestMethod()]
        public void Flatten1Test()
        {
            List<List<int>> sequence = new List<List<int>> {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5 }
            };

            IEnumerable<int> expected = new List<int> { 1, 2, 3, 4, 5 };
            IEnumerable<int> actual = sequence.Flatten1();
            
            CollectionAssert.AreEqual(expected.ToList(), actual.ToList());
        }

        [TestMethod()]
        public void ZipNTest()
        {
            List<List<int>> sequence = new List<List<int>> {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5, 6 },
                new List<int> { 7, 8, 9 }
            };

            IEnumerable<int> expected = new List<int> { 1, 4, 7, 2, 5, 8, 3, 6, 9 };
            var actual = sequence.ZipN(sources => sources);

            CollectionAssert.AreEqual(expected.ToList(), actual.Flatten1().ToList());
        }
    }
}
