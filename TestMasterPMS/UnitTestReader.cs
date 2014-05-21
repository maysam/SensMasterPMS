using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SensMaster
{
    /// <summary>
    /// Summary description for UnitTestReader
    /// </summary>
    [TestClass]
    public class UnitTestReader
    {
     //   Reader reader;
        public UnitTestReader()
        {
//            reader = new Reader();
        }

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
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestParseUserMemory()
        {
            byte[] data_array = new byte[40];
            string data_string = "B|12ABC34|";
            Buffer.BlockCopy(new byte[] { 0x02, 0x03, 0x05, 0x08, 0x0D, 0x15, 0x22, 0x37 }, 0, data_array, 0 , 8);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(data_string),0, data_array, 8, data_string.Length);
            Reader reader = new Reader("id", "ip", 22, "location", ReaderType.SINGLETAG, 1000, 1500, null, null);
            Tag tag = reader.ParseUserMemory(data_array);
            Assert.IsInstanceOfType(tag, typeof(Body));
            Assert.AreEqual(tag.PunchBody, "12ABC34");
        }

        [TestMethod]
        public void TestPoll()
        {
//            Reader r = new Reader();
        }
    }
}
