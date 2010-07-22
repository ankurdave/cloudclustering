﻿using AzureUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.StorageClient;
using System.Linq;

namespace AzureUtilsTest
{
    
    
    /// <summary>
    ///This is a test class for KMeansTaskProcessorTest and is intended
    ///to contain all KMeansTaskProcessorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KMeansTaskProcessorTest
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
        ///A test for AssignClusterPointToNearestCentroid
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void AssignClusterPointToNearestCentroidTest()
        {
            KMeansTaskData task = new KMeansTaskData(Guid.NewGuid(), Guid.NewGuid(), 1, 2, 3, 10, null, null, DateTime.Now, DateTime.Now, 0);
            KMeansTaskProcessor_Accessor target = new KMeansTaskProcessor_Accessor(task);

            target.centroids = new List<Centroid>();
            target.centroids.Add(new Centroid
            {
                ID = Guid.NewGuid(),
                X = 0.0F,
                Y = -1.0F
            });
            target.centroids.Add(new Centroid
            {
                ID = Guid.NewGuid(),
                X = 10.0F,
                Y = 10.0F
            });

            ClusterPoint clusterPoint = new ClusterPoint
            {
                CentroidID = Guid.Empty,
                X = 1.0F,
                Y = 2.0F
            };

            ClusterPoint expected = new ClusterPoint
            {
                CentroidID = target.centroids[0].ID,
                X = 1.0F,
                Y = 2.0F
            };
            ClusterPointProcessingResult_Accessor actual;
            actual = target.AssignClusterPointToNearestCentroid(clusterPoint);

            Assert.AreEqual(expected.CentroidID, actual.Point.CentroidID);
        }

        /// <summary>
        ///A test for ProcessPoints
        ///</summary>
        [TestMethod()]
        [DeploymentItem("AzureHelper.dll")]
        public void ProcessPointsTest()
        {
            CloudBlobContainer container = AzureHelper.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
            container.CreateIfNotExist();
            CloudBlob points = container.GetBlobReference(Guid.NewGuid().ToString());
            CloudBlob centroids = container.GetBlobReference(Guid.NewGuid().ToString());
            const int NumPoints = 10000, NumCentroids = 10;

            using (PointStream<ClusterPoint> pointStream = new PointStream<ClusterPoint>(points, ClusterPoint.FromByteArray, ClusterPoint.Size, false))
            {
                for (int i = 0; i < NumPoints; i++)
                {
                    pointStream.Write(new ClusterPoint(1, 2, Guid.Empty));
                }
            }

            Guid centroidID = Guid.NewGuid();
            using (PointStream<Centroid> stream = new PointStream<Centroid>(centroids, Centroid.FromByteArray, Centroid.Size, false))
            {
                stream.Write(new Centroid(centroidID, 3, 4));

                for (int i = 0; i < NumCentroids - 1; i++)
                {
                    stream.Write(new Centroid(Guid.NewGuid(), 1000, 1000));
                }
            }

            KMeansTaskProcessor_Accessor target = new KMeansTaskProcessor_Accessor(new KMeansTaskData(Guid.NewGuid(), Guid.NewGuid(), NumPoints, NumCentroids, 1, 0, points.Uri, centroids.Uri, DateTime.UtcNow, DateTime.UtcNow, 0));

            System.Diagnostics.Trace.WriteLine("Entering InitializeCentroids");
            target.InitializeCentroids();

            System.Diagnostics.Trace.WriteLine("Entering ProcessPoints");
            System.Diagnostics.Trace.WriteLine("ProcessPoints took " + AzureHelper.Time(() =>
            {
                target.ProcessPoints();
            }).TotalSeconds + " seconds");

            using (PointStream<ClusterPoint> stream = new PointStream<ClusterPoint>(AzureHelper.GetBlob(target.TaskResult.Points).OpenRead(), ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                foreach (ClusterPoint p in stream)
                {
                    Assert.AreEqual(centroidID, p.CentroidID);
                }
            }

            Assert.AreEqual(NumPoints, target.TaskResult.NumPointsChanged);
            Assert.IsTrue(target.TaskResult.PointsProcessedDataByCentroid.ContainsKey(centroidID));
            Assert.AreEqual(NumPoints, target.TaskResult.PointsProcessedDataByCentroid[centroidID].NumPointsProcessed);

            const double Epsilon = 0.0001;
            Assert.IsTrue(Math.Abs((1 * NumPoints) - target.TaskResult.PointsProcessedDataByCentroid[centroidID].PartialPointSum.X) < Epsilon);
            Assert.IsTrue(Math.Abs((2 * NumPoints) - target.TaskResult.PointsProcessedDataByCentroid[centroidID].PartialPointSum.Y) < Epsilon);
        }
    }
}
