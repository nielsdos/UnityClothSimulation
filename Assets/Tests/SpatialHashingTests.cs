using System.Linq;
using NUnit.Framework;
using SoftBody.DataStructures;
using UnityEngine;

namespace Tests
{
    public class SpatialHashingTests
    {
        public class TestItem : ISpatialHashable<TestItem>
        {
            public Vector3 Centroid { get; set; }
            public Vector3 Size { get; }
            public SpatialHashingItemTracker<TestItem> ShItemTracker { get; }
            public string Name { get; }

            public TestItem(string name, Vector3 centroid, Vector3 size)
            {
                Name = name;
                Centroid = centroid;
                Size = size;
                ShItemTracker = new SpatialHashingItemTracker<TestItem>(this);
            }
        }

        [Test]
        public void SimpleAdding()
        {
            var sh = new SpatialHasher<TestItem>(10f);

            var zero = new TestItem("zero", Vector3.zero, Vector3.one);
            var i222 = new TestItem("i222", Vector3.one * 2f, Vector3.one);
            sh.Insert(zero);
            sh.Insert(i222);
            var border = new TestItem("border", new Vector3(10.25f, 10.25f, 10.25f), Vector3.one);
            sh.Insert(border);
            var outside = new TestItem("outside", new Vector3(15f, 15f, 15f), Vector3.one);
            sh.Insert(outside);
            var one = new TestItem("one", Vector3.one, Vector3.one / 2f);
            sh.Insert(one);
            var negOne = new TestItem("negOne", -Vector3.one, Vector3.one);
            sh.Insert(negOne);

            foreach (var test in sh.EnumerateNear(zero))
            {
                Assert.IsTrue(test == zero || test == negOne || test == one || test == i222 || test == border);
            }

            foreach (var test in sh.EnumerateNear(border))
            {
                Assert.IsTrue(test == zero || test == negOne || test == one || test == i222 || test == border ||
                              test == outside);
            }

            foreach (var test in sh.EnumerateNear(outside))
            {
                Assert.IsTrue(test == outside || test == border);
            }

            foreach (var test in sh.EnumerateNear(negOne))
            {
                Assert.IsTrue(test == negOne || test == zero);
            }
        }

        [Test]
        public void SimpleRemoving()
        {
            var sh = new SpatialHasher<TestItem>(10f);

            var zero = new TestItem("zero", Vector3.zero, Vector3.one);
            var i222 = new TestItem("i222", Vector3.one * 2f, Vector3.one);
            sh.Insert(zero);
            sh.Insert(i222);
            var border = new TestItem("border", new Vector3(10.25f, 10.25f, 10.25f), Vector3.one);
            sh.Insert(border);
            var outside = new TestItem("outside", new Vector3(15f, 15f, 15f), Vector3.one);
            sh.Insert(outside);

            sh.Remove(zero);

            foreach (var test in sh.EnumerateNear(zero))
            {
                Assert.IsTrue(test == i222 || test == border);
            }

            foreach (var test in sh.EnumerateNear(border))
            {
                Assert.IsTrue(test == i222 || test == border || test == outside);
            }

            foreach (var test in sh.EnumerateNear(outside))
            {
                Assert.IsTrue(test == outside || test == border);
            }

            sh.Remove(border);

            foreach (var test in sh.EnumerateNear(zero))
            {
                Assert.IsTrue(test == i222);
            }

            foreach (var test in sh.EnumerateNear(outside))
            {
                Assert.IsTrue(test == outside);
            }
        }

        [Test]
        public void SimpleMoving()
        {
            var sh = new SpatialHasher<TestItem>(5f);

            var one = new TestItem("one", Vector3.one, Vector3.one);
            sh.Insert(one);

            var zero = new TestItem("zero", Vector3.zero, Vector3.one);
            sh.Insert(zero);

            var negTen = new TestItem("negTen", -Vector3.one * 10f, Vector3.one);
            sh.Insert(negTen);

            foreach (var test in sh.EnumerateNear(one).Concat(sh.EnumerateNear(zero)))
            {
                Assert.IsTrue(test == zero || test == one);
            }

            zero.Centroid -= Vector3.one * 5f;
            sh.Update(zero);

            foreach (var test in sh.EnumerateNear(zero))
            {
                Assert.IsTrue(test == zero || test == negTen);
            }

            foreach (var test in sh.EnumerateNear(one))
            {
                Assert.IsTrue(test == one);
            }

            zero.Centroid += Vector3.one * 0.1f;
            sh.Update(zero);

            foreach (var test in sh.EnumerateNear(zero))
            {
                Assert.IsTrue(test == zero || test == negTen);
            }

            foreach (var test in sh.EnumerateNear(one))
            {
                Assert.IsTrue(test == one);
            }
        }
    }
}