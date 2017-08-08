﻿using NSubstitute;
using NUnit.Framework;
using SqlServerReportRunner.Models;
using SqlServerReportRunner.Reporting.Executors;
using SqlServerReportRunner.Reporting.Writers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.SqlServerReportRunner.Reporting.Writers
{
    [TestFixture]
    public class CsvReportWriterTest
    {
        private IReportWriter _reportWriter;
        private string _testRootFolder = String.Empty;
        private string _filePath = String.Empty;

        [SetUp]
        public void CsvReportWriterTest_SetUp()
        {
            // create the root folder
            _testRootFolder = Path.Combine(Environment.CurrentDirectory, "DelimitedReportWriterTest");
            Directory.CreateDirectory(_testRootFolder);

            _filePath = Path.Combine(_testRootFolder, Path.GetRandomFileName());
            _reportWriter = new CsvReportWriter(_filePath);

        }

        [TearDown]
        public void CsvReportWriterTest_TearDown()
        {
            Directory.Delete(_testRootFolder, true);
        }

        [Test]
        public void CsvReportWriterTest_CheckFileContents()
        {
            const string delimiter = ",";
            // set up the data reader to return columns
            ColumnMetaData[] headers = {
                new ColumnMetaData("Name", "varchar", 100),
                new ColumnMetaData("Surname", "varchar", 100),
                new ColumnMetaData("Age", "varchar", 32),
            };
            object[][] data =
            {
                new object[] { "Matt", "Salmon", 41 },
                new object[] { "John", "Doe", 61 },
                new object[] { "Jane", "Smith", 25 }
            };

            IDataReader reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(3);
            reader.Read().Returns(true, true, true, false);
            reader.GetValue(0).Returns(data[0][0], data[1][0], data[2][0]);
            reader.GetValue(1).Returns(data[0][1], data[1][1], data[2][1]);
            reader.GetValue(2).Returns(data[0][2], data[1][2], data[2][2]);

            // execute
            _reportWriter.WriteHeader(headers.Select(x => x.Name), delimiter);
            foreach (object[] line in data)
            {
                _reportWriter.WriteLine(reader, headers, delimiter);
            }
            _reportWriter.Dispose();

            // assert
            Assert.IsTrue(File.Exists(_filePath));
            List<string> lines = File.ReadLines(_filePath).ToList();

            // check that the header is correct
            string expectedHeader = String.Join(delimiter, headers.Select(x => x.Name));
            Assert.AreEqual(expectedHeader, lines[0]);

            // make sure each of the lines is written correctly
            for (int i=0; i<data.Length; i++)
            {
                object[] line = data[i];
                string expectedLine = String.Join(delimiter, data[i]);
                string actualLine = lines[i + 1];
                Assert.AreEqual(expectedLine, actualLine);

            }
        }

        [Test]
        public void CsvReportWriterTest_DataContainsLineBreaks_LineBreaksRemoved()
        {
            const string delimiter = ",";

            // set up the data reader to return columns
            ColumnMetaData[] headers = {
                new ColumnMetaData("Name", "varchar", 100),
                new ColumnMetaData("Surname", "varchar", 100)
            };
            object[][] data =
            {
                new object[] { "NL", "new\nline" },
                new object[] { "CR", "carriage\rreturn" },
                new object[] { "CRNL", "carriage\rreturn_new\nline"  }
            };

            IDataReader reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(2);
            reader.Read().Returns(true, true, true, false);
            reader.GetValue(0).Returns(data[0][0], data[1][0], data[2][0]);
            reader.GetValue(1).Returns(data[0][1], data[1][1], data[2][1]);

            // execute
            foreach (object[] line in data)
            {
                _reportWriter.WriteLine(reader, headers, delimiter);
            }
            _reportWriter.Dispose();

            // assert
            Assert.IsTrue(File.Exists(_filePath));

            List<string> lines = File.ReadLines(_filePath).ToList();
            Assert.AreEqual("NL,newline", lines[0]);
            Assert.AreEqual("CR,carriagereturn", lines[1]);
            Assert.AreEqual("CRNL,carriagereturn_newline", lines[2]);
        }

        [Test]
        public void CsvReportWriterTest_DataContainsNulls_EmptyStringReturned()
        {
            const string delimiter = ",";

            // set up the data reader to return columns
            ColumnMetaData[] headers = {
                new ColumnMetaData("Name", "varchar", 100),
                new ColumnMetaData("Surname", "varchar", 100)
            };
            object[][] data =
            {
                new object[] { "Valid", "valid" },
                new object[] { "DbNull", DBNull.Value },
                new object[] { "null", null  }
            };

            IDataReader reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(2);
            reader.Read().Returns(true, true, true, false);
            reader.GetValue(0).Returns(data[0][0], data[1][0], data[2][0]);
            reader.GetValue(1).Returns(data[0][1], data[1][1], data[2][1]);

            // execute
            foreach (object[] line in data)
            {
                _reportWriter.WriteLine(reader, headers, delimiter);
            }
            _reportWriter.Dispose();

            // assert
            Assert.IsTrue(File.Exists(_filePath));

            List<string> lines = File.ReadLines(_filePath).ToList();
            Assert.AreEqual("Valid,valid", lines[0]);
            Assert.AreEqual("DbNull,", lines[1]);
            Assert.AreEqual("null,", lines[2]);
        }

        [Test]
        public void CsvReportWriterTest_DataContainsQuotes_QuotesAreEscaped()
        {
            const string delimiter = ",";

            // set up the data reader to return columns
            ColumnMetaData[] headers = {
                new ColumnMetaData("Name", "varchar", 100),
                new ColumnMetaData("Surname", "varchar", 100)
            };
            object[][] data =
            {
                new object[] { "Matt", "Salm\"on" },
                new object[] { "Jo\"hn", "Thomas" },
            };

            IDataReader reader = Substitute.For<IDataReader>();
            reader.FieldCount.Returns(2);
            reader.Read().Returns(true, true, true, false);
            reader.GetValue(0).Returns(data[0][0], data[1][0]);
            reader.GetValue(1).Returns(data[0][1], data[1][1]);

            // execute
            foreach (object[] line in data)
            {
                _reportWriter.WriteLine(reader, headers, delimiter);
            }
            _reportWriter.Dispose();

            // assert
            Assert.IsTrue(File.Exists(_filePath));

            List<string> lines = File.ReadLines(_filePath).ToList();
            Assert.AreEqual("Matt,\"Salm\"\"on\"", lines[0]);
            Assert.AreEqual("\"Jo\"\"hn\",Thomas", lines[1]);
        }
    }
}