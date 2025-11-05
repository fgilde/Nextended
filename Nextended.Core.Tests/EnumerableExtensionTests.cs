using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;

namespace Nextended.Core.Tests
{
	[TestClass]
	public class EnumerableExtensionTests
	{
		[TestMethod]
		public void IndexOf_ElementExists_ReturnsCorrectIndex()
		{
			var array = new[] { 1, 2, 3, 4, 5 };
			var index = array.IndexOf(3);
			
			Assert.AreEqual(2, index);
		}

		[TestMethod]
		public void IndexOf_ElementDoesNotExist_ReturnsMinusOne()
		{
			var array = new[] { 1, 2, 3, 4, 5 };
			var index = array.IndexOf(10);
			
			Assert.AreEqual(-1, index);
		}

		[TestMethod]
		public void None_EmptyCollection_ReturnsTrue()
		{
			var list = new List<int>();
			
			Assert.IsTrue(list.None());
		}

		[TestMethod]
		public void None_NonEmptyCollection_ReturnsFalse()
		{
			var list = new List<int> { 1, 2, 3 };
			
			Assert.IsFalse(list.None());
		}

		[TestMethod]
		public void None_WithPredicate_NoMatch_ReturnsTrue()
		{
			var list = new List<int> { 1, 2, 3 };
			
			Assert.IsTrue(list.None(x => x > 10));
		}

		[TestMethod]
		public void None_WithPredicate_HasMatch_ReturnsFalse()
		{
			var list = new List<int> { 1, 2, 3 };
			
			Assert.IsFalse(list.None(x => x > 2));
		}

		[TestMethod]
		public void EmptyIfNull_NullCollection_ReturnsEmpty()
		{
			List<int> nullList = null;
			var result = nullList.EmptyIfNull();
			
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public void EmptyIfNull_NonNullCollection_ReturnsSameCollection()
		{
			var list = new List<int> { 1, 2, 3 };
			var result = list.EmptyIfNull();
			
			Assert.AreSame(list, result);
		}

		[TestMethod]
		public void AddRange_AddsItemsToCollection()
		{
			var collection = new List<int> { 1, 2, 3 };
			var itemsToAdd = new[] { 4, 5, 6 };
			
			collection.AddRange(itemsToAdd);
			
			Assert.AreEqual(6, collection.Count);
			Assert.IsTrue(collection.Contains(4));
			Assert.IsTrue(collection.Contains(5));
			Assert.IsTrue(collection.Contains(6));
		}

		[TestMethod]
		public void AddRange_ConcurrentBag_AddsItems()
		{
			var bag = new ConcurrentBag<int> { 1, 2, 3 };
			var itemsToAdd = new[] { 4, 5, 6 };
			
			bag.AddRange(itemsToAdd);
			
			Assert.AreEqual(6, bag.Count);
		}

		[TestMethod]
		public void RemoveRange_RemovesItemsFromCollection()
		{
			var collection = new List<int> { 1, 2, 3, 4, 5 };
			var itemsToRemove = new[] { 2, 4 };
			
			collection.RemoveRange(itemsToRemove);
			
			Assert.AreEqual(3, collection.Count);
			Assert.IsFalse(collection.Contains(2));
			Assert.IsFalse(collection.Contains(4));
		}

		[TestMethod]
		public void RemoveAll_WithPredicate_RemovesMatchingItems()
		{
			var collection = new List<int> { 1, 2, 3, 4, 5 };
			
			collection.RemoveAll(x => x % 2 == 0);
			
			Assert.AreEqual(3, collection.Count);
			Assert.IsTrue(collection.All(x => x % 2 != 0));
		}

		[TestMethod]
		public void RemoveAll_NoPredicate_RemovesAllItems()
		{
			var collection = new List<int> { 1, 2, 3, 4, 5 };
			
			collection.RemoveAll();
			
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void Apply_ExecutesActionOnEachElement()
		{
			var list = new List<int> { 1, 2, 3 };
			var result = new List<int>();
			
			list.Apply(x => result.Add(x * 2)).ToList();
			
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(2, result[0]);
			Assert.AreEqual(4, result[1]);
			Assert.AreEqual(6, result[2]);
		}

		[TestMethod]
		public void Apply_WithIndex_ExecutesActionWithIndex()
		{
			var list = new List<string> { "a", "b", "c" };
			var result = new List<string>();
			
			list.Apply((index, item) => result.Add($"{index}-{item}")).ToList();
			
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual("0-a", result[0]);
			Assert.AreEqual("1-b", result[1]);
			Assert.AreEqual("2-c", result[2]);
		}

		[TestMethod]
		public void ToSafeEnumeration_CreatesArray()
		{
			var enumerable = Enumerable.Range(1, 5);
			
			var result = enumerable.ToSafeEnumeration();
			
			Assert.IsInstanceOfType(result, typeof(int[]));
			Assert.AreEqual(5, result.Length);
		}

		[TestMethod]
		public void ToConcatenatedString_WithStrings_ConcatenatesCorrectly()
		{
			var list = new List<string> { "a", "b", "c" };
			
			var result = list.ToConcatenatedString();
			
			Assert.AreEqual("a,b,c", result);
		}

		[TestMethod]
		public void ToConcatenatedString_WithCustomSeparator_UsesCorrectSeparator()
		{
			var list = new List<string> { "a", "b", "c" };
			
			var result = list.ToConcatenatedString("|");
			
			Assert.AreEqual("a|b|c", result);
		}

		[TestMethod]
		public void ToConcatenatedString_WithSelector_AppliesSelector()
		{
			var list = new List<int> { 1, 2, 3 };
			
			var result = list.ToConcatenatedString(x => (x * 2).ToString(), "-");
			
			Assert.AreEqual("2-4-6", result);
		}

		[TestMethod]
		public void ThrowIfNullOrEmpty_NullCollection_ThrowsException()
		{
			List<int> nullList = null;
			
			ExceptionAssert.Throws<ArgumentNullException>(
				() => nullList.ThrowIfNullOrEmpty("testParam"),
				ex => ex.ParamName == "testParam");
		}

		[TestMethod]
		public void ThrowIfNullOrEmpty_EmptyCollection_ThrowsException()
		{
			var emptyList = new List<int>();
			
			ExceptionAssert.Throws<ArgumentException>(
				() => emptyList.ThrowIfNullOrEmpty("testParam"),
				ex => ex.Message.Contains("empty"));
		}

		[TestMethod]
		public void ThrowIfNullOrEmpty_NonEmptyCollection_ReturnsCollection()
		{
			var list = new List<int> { 1, 2, 3 };
			
			var result = list.ThrowIfNullOrEmpty("testParam");
			
			Assert.AreSame(list, result);
		}

		[TestMethod]
		public void NullIfEmpty_EmptyCollection_ReturnsNull()
		{
			var emptyList = new List<int>();
			
			var result = emptyList.NullIfEmpty();
			
			Assert.IsNull(result);
		}

		[TestMethod]
		public void NullIfEmpty_NonEmptyCollection_ReturnsSameCollection()
		{
			var list = new List<int> { 1, 2, 3 };
			
			var result = list.NullIfEmpty();
			
			Assert.AreSame(list, result);
		}

		[TestMethod]
		public void NullIfEmpty_Dictionary_EmptyDictionary_ReturnsNull()
		{
			var emptyDict = new Dictionary<string, int>();
			
			var result = emptyDict.NullIfEmpty();
			
			Assert.IsNull(result);
		}

		[TestMethod]
		public void AddOrUpdate_NewKey_AddsItem()
		{
			var dict = new Dictionary<string, int> { { "a", 1 } };
			
			dict.AddOrUpdate("b", 2);
			
			Assert.AreEqual(2, dict.Count);
			Assert.AreEqual(2, dict["b"]);
		}

		[TestMethod]
		public void AddOrUpdate_ExistingKey_UpdatesItem()
		{
			var dict = new Dictionary<string, int> { { "a", 1 } };
			
			dict.AddOrUpdate("a", 10);
			
			Assert.AreEqual(1, dict.Count);
			Assert.AreEqual(10, dict["a"]);
		}

		[TestMethod]
		public void MergeWith_CombinesDictionaries()
		{
			var dict1 = new Dictionary<string, int> { { "a", 1 } };
			var dict2 = new Dictionary<string, int> { { "b", 2 } };
			var dict3 = new Dictionary<string, int> { { "c", 3 } };
			
			var result = dict1.MergeWith(dict2, dict3);
			
			Assert.AreEqual(3, result.Count);
			Assert.AreEqual(1, result["a"]);
			Assert.AreEqual(2, result["b"]);
			Assert.AreEqual(3, result["c"]);
		}

		[TestMethod]
		public void Recursive_WithChildren_ReturnsAllDescendants()
		{
			var root = new TreeNode
			{
				Value = 1,
				Children = new List<TreeNode>
				{
					new TreeNode { Value = 2, Children = new List<TreeNode>() },
					new TreeNode
					{
						Value = 3,
						Children = new List<TreeNode>
						{
							new TreeNode { Value = 4, Children = new List<TreeNode>() }
						}
					}
				}
			};
			
			var allNodes = new[] { root }.Recursive(n => n.Children).ToList();
			
			Assert.AreEqual(4, allNodes.Count);
			Assert.IsTrue(allNodes.Any(n => n.Value == 1));
			Assert.IsTrue(allNodes.Any(n => n.Value == 2));
			Assert.IsTrue(allNodes.Any(n => n.Value == 3));
			Assert.IsTrue(allNodes.Any(n => n.Value == 4));
		}

		[TestMethod]
		public void Remove_ConcurrentBag_RemovesItem()
		{
			var bag = new ConcurrentBag<int> { 1, 2, 3, 4, 5 };
			
			bag.Remove(3);
			
			Assert.AreEqual(4, bag.Count);
			Assert.IsFalse(bag.Contains(3));
		}

		private class TreeNode
		{
			public int Value { get; set; }
			public List<TreeNode> Children { get; set; }
		}
	}
}
