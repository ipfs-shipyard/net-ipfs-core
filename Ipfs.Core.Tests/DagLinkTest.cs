﻿using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Google.Protobuf;

namespace Ipfs
{
    [TestClass]
    public class DagLinkTest
    {
        [TestMethod]
        public void Creating()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            Assert.AreEqual("abc", link.Name);
            Assert.AreEqual("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string)link.Id);
            Assert.AreEqual(5, link.Size);
        }

        [TestMethod]
        public void Cloning()
        {
            var link = new DagLink("abc", "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", 5);
            var clone = new DagLink(link);

            Assert.AreEqual("abc", clone.Name);
            Assert.AreEqual("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string)clone.Id);
            Assert.AreEqual(5, clone.Size);
        }

        [TestMethod]
        public void Encoding()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51201611803";
            var link = new DagLink("a", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.AreEqual(encoded, link.ToArray().ToHexString());
        }

        [TestMethod]
        public void Encoding_EmptyName()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd512001803";
            var link = new DagLink("", "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.AreEqual(encoded, link.ToArray().ToHexString());
        }

        [TestMethod]
        public void Encoding_NullName()
        {
            var encoded = "0a22122023dca2a7429612378554b0bb5b85012dec00a17cc2c673f17d2b76a50b839cd51803";
            var link = new DagLink(null, "QmQke7LGtfu3GjFP3AnrP8vpEepQ6C5aJSALKAq653bkRi", 3);
            var x = link.ToArray();
            Assert.AreEqual(encoded, link.ToArray().ToHexString());
        }

        [TestMethod]
        public void Null_Stream()
        {
            ExceptionAssert.Throws(() => new DagLink((CodedInputStream)null));
            ExceptionAssert.Throws(() => new DagLink((Stream)null));
        }

        [TestMethod]
        public void Cid_V1()
        {
            var link = new DagLink("hello", "zB7NCdng5WffuNCgHu4PhDj7nbtuVrhPc2pMhanNxYKRsECdjX9nd44g6CRu2xNrj2bG2NNaTsveL5zDGWhbfiug3VekW", 11);
            Assert.AreEqual("hello", link.Name);
            Assert.AreEqual(1, link.Id.Version);
            Assert.AreEqual("raw", link.Id.ContentType);
            Assert.AreEqual("sha2-512", link.Id.Hash.Algorithm.Name);
            Assert.AreEqual(11, link.Size);
        }


    }
}
