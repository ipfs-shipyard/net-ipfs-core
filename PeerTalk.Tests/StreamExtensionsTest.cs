using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PeerTalk.Tests
{
    [TestClass]
    public class StreamExtensionsTest
    {
        [TestMethod]
        public async Task ReadAsync()
        {
            var expected = new byte[] { 1, 2, 3, 4 };
            using (var ms = new MemoryStream(expected))
            {
                var actual = new byte[expected.Length];
                await ms.ReadExactAsync(actual, 0, actual.Length);
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void ReadAsync_EOS()
        {
            var expected = new byte[] { 1, 2, 3, 4 };
            var actual = new byte[expected.Length + 1];

            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<EndOfStreamException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length).Wait();
                });
            }

            var cancel = new CancellationTokenSource();
            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<EndOfStreamException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token).Wait();
                });
            }
        }

        [TestMethod]
        public async Task ReadAsync_Cancel()
        {
            var expected = new byte[] { 1, 2, 3, 4 };
            var actual = new byte[expected.Length];
            var cancel = new CancellationTokenSource();
            using (var ms = new MemoryStream(expected))
            {
                await ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token);
                CollectionAssert.AreEqual(expected, actual);
            }

            cancel.Cancel();
            using (var ms = new MemoryStream(expected))
            {
                ExceptionAssert.Throws<TaskCanceledException>(() =>
                {
                    ms.ReadExactAsync(actual, 0, actual.Length, cancel.Token).Wait();
                });
            }
        }
    }
}
