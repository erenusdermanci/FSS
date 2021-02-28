using System.Collections;
using System.Collections.Generic;
using DataComponents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class TestSuite
{
    private Chunk[] dummyChunks;

    [SetUp]
    private void SetupDummyChunks()
    {
        Chunk.Size = 3;

        var chunk1 = new Chunk(Vector2.zero)
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk2 = new Chunk(new Vector2(-1, -1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk3 = new Chunk(new Vector2(0, -1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk4 = new Chunk(new Vector2(1, -1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk5 = new Chunk(new Vector2(-1, 0))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk6 = new Chunk(new Vector2(1, 0))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk7 = new Chunk(new Vector2(-1, 1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk8 = new Chunk(new Vector2(0, 1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        var chunk9 = new Chunk(new Vector2(1, 1))
        {
            BlockTypes = new int[]
              { 0, 0, 0,
                0, 0, 0,
                0, 0, 0 }
        };

        dummyChunks = new Chunk[] { chunk1, chunk2, chunk3, chunk4, chunk5, chunk6, chunk7, chunk8, chunk9};
    }

    [TearDown]
    private void DisposeDummyChunks()
    {
        foreach (var chunk in dummyChunks)
        {
            chunk.Dispose();
        }
    }

    // A Test behaves as an ordinary method
    [Test]
    public void TestSuiteSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator TestSuiteWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }

    [TestCase(0, 0, 0, 0)]
    public void TestGetBlockConsistency(int chunkIndex, int x, int y, bool current, int expectedType)
    {
        Assert.That(x, Is.EqualTo(expectedType));
    }

    [TestCase(0, 0, 0, 0, 0, 0)]
    public void TestUpdateCoordinatesConsistency(int x, int y, int chunkIndex, int expectedX, int expectedY, int expectedChunkIndex)
    {

    }

}
