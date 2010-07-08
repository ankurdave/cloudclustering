using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.WindowsAzure.StorageClient;
using System.Collections.Generic;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for KMeansJobTest and is intended
    ///to contain all KMeansJobTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KMeansJobTest
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
        ///A test for InitializeStorage
        ///</summary>
        [TestMethod()]
        public void InitializeStorageTest()
        {
            KMeansJobData jobData = new KMeansJobData
            {
                JobID = Guid.NewGuid(),
                K = 2,
                M = 4,
                N = 6
            };
            KMeansJob target = new KMeansJob(jobData);
            target.InitializeStorage();
            
            // Verify that the created containers and blobs actually exist
            CloudBlobClient client = AzureHelper.StorageAccount.CreateCloudBlobClient();

            CloudBlobContainer c = null;
            try
            {
                c = client.GetContainerReference(jobData.JobID.ToString());
                c.FetchAttributes();
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Assert.Fail();
                }
                else
                {
                    throw;
                }
            }

            CloudBlob points, centroids;
            try
            {
                points = c.GetBlobReference("points");
                points.FetchAttributes();
                centroids = c.GetBlobReference("centroids");
                centroids.FetchAttributes();
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    Assert.Fail();
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
