﻿using NSubstitute;
using NUnit.Framework;
using SqlServerReportRunner.Reporting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.SqlServerReportRunner.Reporting
{
    [TestFixture]
    public class ConcurrencyCoordinatorTest
    {
        private IConcurrencyCoordinator _concurrencyCoordinator;
        private IReportLocationProvider _reportLocationProvider;
        private string _testRootFolder;

        [SetUp]
        public void ConcurrencyCoordinatorTest_SetUp()
        {
            _testRootFolder = Path.Combine(Environment.CurrentDirectory, "ConcurrencyCoordinatorTest");
            Directory.CreateDirectory(_testRootFolder);

            _reportLocationProvider = Substitute.For<IReportLocationProvider>();

            _concurrencyCoordinator = new ConcurrencyCoordinator(_reportLocationProvider);
        }

        [TearDown]
        public void ConcurrencyCoordinatorTest_TearDown()
        {
            if (Directory.Exists(_testRootFolder))
            {
                Directory.Delete(_testRootFolder, true);
            }
        }


        [Test]
        public void GetRunningReports_NoFiles_ReturnsZero()
        {
            // setup
            string connectionName = Path.GetRandomFileName();
            string processingFolder = Path.Combine(_testRootFolder, connectionName);
            _reportLocationProvider.GetProcessingFolder(connectionName).Returns(processingFolder);

            // execute
            int[] result = _concurrencyCoordinator.GetRunningReports(connectionName);

            // assert
            Assert.AreEqual(0, result.Length);

        }

        [Test]
        public void GetRunningReports_ContainsFiles_ReturnsCorrectCount()
        {
            // setup
            string connectionName = Path.GetRandomFileName();
            string processingFolder = Path.Combine(_testRootFolder, connectionName);
            _reportLocationProvider.GetProcessingFolder(connectionName).Returns(processingFolder);

            Directory.CreateDirectory(processingFolder);
            int fileCount = new Random().Next(1, 7);
            for (int i=1; i<=fileCount; i++)
            {
                string filePath = Path.Combine(processingFolder, i.ToString());
                File.WriteAllText(filePath, "test data");
            }

            // execute
            int[] result = _concurrencyCoordinator.GetRunningReports(connectionName);

            // assert
            Assert.AreEqual(fileCount, result.Length);
        }

        [Test]
        public void LockReportJob_OnCall_CreatesTextFile()
        {
            // setup
            string connectionName = Path.GetRandomFileName();
            int jobId = new Random().Next(1, 100);
            string processingFolder = Path.Combine(_testRootFolder, connectionName);
            _reportLocationProvider.GetProcessingFolder(connectionName).Returns(processingFolder);
            Directory.CreateDirectory(processingFolder);

            // execute
            _concurrencyCoordinator.LockReportJob(connectionName, jobId);

            string expectedPath = Path.Combine(processingFolder, jobId.ToString());
            Assert.IsTrue(File.Exists(expectedPath));

            // clean up
            Directory.Delete(processingFolder, true);
        }

        [Test]
        public void UnlockReportJob_OnCall_CreatesTextFile()
        {
            // setup
            string connectionName = Path.GetRandomFileName();
            int jobId = new Random().Next(1, 100);
            string processingFolder = Path.Combine(_testRootFolder, connectionName);
            _reportLocationProvider.GetProcessingFolder(connectionName).Returns(processingFolder);
            string lockFilePath = Path.Combine(processingFolder, jobId.ToString());
            
            Directory.CreateDirectory(processingFolder);
            File.WriteAllText(lockFilePath, String.Empty);

            // execute
            _concurrencyCoordinator.UnlockReportJob(connectionName, jobId);

            Assert.IsFalse(File.Exists(lockFilePath));

            // clean up
            Directory.Delete(processingFolder, true);
        }

    }
}
