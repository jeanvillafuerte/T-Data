namespace Thomas.Database.Tests
{
    [TestFixture]
    public class HashHelperTests
    {
        [Test]
        public void Equal_GenerateUniqueStringHashTest()
        {
            var hash1 = HashHelper.GenerateUniqueStringHash("Long string to test unique hash from same input");
            var hash2 = HashHelper.GenerateUniqueStringHash("Long string to test unique hash from same input");
            Assert.AreEqual(hash2, hash1);
        }

        [Test]
        public void Not_Equal_GenerateUniqueStringHashTest()
        {
            var hash1 = HashHelper.GenerateUniqueStringHash("Long string to test unique hash from same input");
            var hash2 = HashHelper.GenerateUniqueStringHash("Long string to test unique hash from same input2");
            Assert.AreNotEqual(hash2, hash1);
        }

        [Test]
        public void Equal_GenerateUniqueHashTest()
        {
            var hash1 = HashHelper.GenerateUniqueHash("Long string to test unique hash from same input", "signature", null, TypeCacheObject.ScriptDefinition);
            var hash2 = HashHelper.GenerateUniqueHash("Long string to test unique hash from same input", "signature", null, TypeCacheObject.ScriptDefinition);
            Assert.AreEqual(hash2, hash1);
        }

        [Test]
        public void Not_Equal_GenerateUniqueHashTest()
        {
            var hash1 = HashHelper.GenerateUniqueHash("Long string to test unique hash from same input", "signature", null, TypeCacheObject.ScriptDefinition);
            var hash2 = HashHelper.GenerateUniqueHash("Long string to test unique hash from same input2", "signature", null, TypeCacheObject.ScriptDefinition);
            Assert.AreNotEqual(hash2, hash1);
        }
    }
}