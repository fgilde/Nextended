using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace Nextended.Core.Extensions
{
	/// <summary>
	/// Extends IEnumerable
	/// </summary>
	public static class EnumerableExtensions
	{
        public static TU Get<T, TU>(this Dictionary<T, TU> dict, T key)
            where TU : class
        {
            dict.TryGetValue(key, out var val);
            return val;
        }

		/// <summary>
		/// Works as "Where", but with recursion
		/// </summary>
		public static IEnumerable<TSource> Recursive<TSource>(this IEnumerable<TSource> children, Func<TSource, IEnumerable<TSource>> childDelegate)
		{
			return Recursive(children, childDelegate, source => true);
		}

		/// <summary>
        /// Works as "Where", but with recursion
		/// </summary>
		public static IEnumerable<TSource> Recursive<TSource>(this IEnumerable<TSource> children, Func<TSource, IEnumerable<TSource>> childDelegate, Func<TSource, bool> predicate)
		{
			foreach (var source in children)
			{
				var grandchildren = childDelegate(source);
				foreach (var grandchild in Recursive(grandchildren, childDelegate, predicate))
					yield return grandchild;
				if (predicate(source))
					yield return source;
			}
		}

		/// <summary>
		/// ToObservableCollection
		/// </summary>
		public static ObservableCollection<TSource> ToObservableCollection<TSource>(this IEnumerable<TSource> source)
		{
			return new ObservableCollection<TSource>(source);
		}

		/// <summary>
		/// Add range.
		/// </summary>
		public static TDictionary AddRange<TDictionary, TKey, TSource>(this TDictionary source, IDictionary<TKey, TSource> collection)
		  where TDictionary : IDictionary<TKey, TSource>
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			foreach (var (key, value) in collection)
			{
				if (!source.ContainsKey(key))
					source.Add(key, value);
				else
					source[key] = value;
			}
			return source;
		}

		/// <summary>
		/// Add Range 
		/// </summary>
		public static TCollection AddRange<TCollection, TSource>(this TCollection source, IEnumerable<TSource> itemsToAdd)
            where TCollection : ICollection<TSource>
        {
            foreach (var item in itemsToAdd)
				source.Add(item);
			return source;
		}


        /// <summary>
        /// Add Range 
        /// </summary>
        public static ConcurrentBag<TSource> AddRange<TSource>(this ConcurrentBag<TSource> source, IEnumerable<TSource> itemsToAdd)
        {
            foreach (var item in itemsToAdd)
                source.Add(item);
            return source;
        }

        public static TCollection RemoveRange<TCollection, TSource>(this TCollection source, IEnumerable<TSource> itemsToRemove)
            where TCollection : ICollection<TSource>
        {
            foreach (var item in itemsToRemove)
                source.Remove(item);
            return source;
        }

		/// <summary>
		/// Add Range 
		/// </summary>
		public static TCollection RemoveAll<TCollection, TSource>(this TCollection source, Func<TSource, bool> predicate = null)
            where TCollection : ICollection<TSource>
        {
			if (predicate == null)
				source.Clear();
            else
            {
                foreach (var item in source.Where(predicate).ToList())
                    source.Remove(item);
            }

            return source;
		}

		/// <summary>
		/// Applies the specified action to each item in the collection.
		/// </summary>
		public static IEnumerable<T> Apply<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
            var items = enumerable.ToSafeEnumeration();
			if (items.IsNull())
                return Enumerable.Empty<T>();
            foreach (var item in items)
                action(item);
            return items;
		}
		
        public static IEnumerable<T> Apply<T>(this IEnumerable<T> enumerable, Action<int, T> action)
        {
            var items = enumerable.ToSafeEnumeration();
            if (items.IsNull())
				return Enumerable.Empty<T>(); ;

            for (var i = 0; i < items.Length; i++)
                action(i, items[i]);

            return items;
        }

        public static IEnumerable Apply(this IEnumerable enumerable, Action<object> action)
        {
            enumerable.Cast<object>().Apply(action);
            return enumerable;
        }

        public static IEnumerable Apply(this IEnumerable enumerable, Action<int, object> action)
        {
            return enumerable.Cast<object>().Apply(action);
        }

		public static T[] ToSafeEnumeration<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable.IsNull())
            {
                return null;
            }

            return enumerable as T[] ?? enumerable.ToArray();
        }

		public static DataTable AsDataTable<T>(this IEnumerable<T> source)
		{
			var dataTableResult = new DataTable();
			var propertyInfos = typeof(T).GetProperties();
			foreach (var propertyInfo in propertyInfos)
			{
				dataTableResult.Columns.Add(new DataColumn(propertyInfo.Name, propertyInfo.PropertyType));
			}
			foreach (var entry in source)
			{
				var newRow = dataTableResult.NewRow();
				foreach (var propertyInfo in propertyInfos)
				{
					var value = propertyInfo.GetValue(entry, new object[0]);
					newRow[propertyInfo.Name] = value;
				}
				dataTableResult.Rows.Add(newRow);
			}
			return dataTableResult;
		}

		/// <summary>
		/// Determines whether a sequence contains any elements.
		/// </summary>
		/// <returns>
		/// false if the enumerable sequence contains any elements; otherwise, true.
		/// </returns>
		public static bool None<T>(this IEnumerable<T> enumerable)
		{
			return enumerable == null || !enumerable.Any();
		}

		/// <summary>
		/// Determines whether any element of a sequence satisfies a condition.
		/// </summary>
		/// 
		/// <returns>
		/// false if any elements in the enumerable sequence pass the test in the specified predicate; otherwise, true.
		/// </returns>
		public static bool None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
		{
			return enumerable == null || !enumerable.Any(predicate);
		}
		
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> collection)
		{
			return collection ?? Enumerable.Empty<T>();
		}

		/// <param name="dictionary">The dictionary to check for 'null' or emptiness</param>
		/// <typeparam name="TK"></typeparam>
		/// <typeparam name="TV"></typeparam>
		/// <returns>'null' if <paramref name="dictionary"/> is null or empty</returns>
		public static IDictionary<TK, TV> NullIfEmpty<TK, TV>(this IDictionary<TK, TV> dictionary)
		{
			return dictionary?.Any() == true ? dictionary : null;
		}

		/// <param name="collection">The collection to check for 'null' or emptiness</param>
		/// <typeparam name="T"></typeparam>
		/// <returns>'null' if <paramref name="collection"/> is null or empty</returns>
		public static ICollection<T> NullIfEmpty<T>(this ICollection<T> collection)
		{
			return collection?.Any() == true ? collection : null;
		}

		public static IList<T> NullIfEmpty<T>(this IList<T> collection)
		{
			return (IList<T>)NullIfEmpty((ICollection<T>)collection);
		}

		public static IEnumerable<T> ThrowIfNullOrEmpty<T>(this IEnumerable<T> items, string paramName)
		{
			if (items.IsNullOrEmpty())
			{
				throw new ArgumentException("Argument '{0}' is null or empty and shouldn't be.".FormatWith(paramName),
					paramName);
			}

            return items;
        }

		/// <summary>
		/// Concatenates strings with delimiter <paramref name="separator"/>
		/// </summary>
		/// <param name="list">Enumerable strings to concatenate</param>
		/// <param name="separator">optional separator, defaults to ","</param>
		/// <returns>Concatenated string of list items or empty string if list is null</returns>
		public static string ToConcatenatedString(this IEnumerable<string> list, string separator = ",")
		{
			return list == null ? string.Empty : string.Join(separator, list);
		}

		/// <summary>
		/// Concatenates strings with delimiter <paramref name="separator"/>
		/// </summary>
		/// <param name="list">Enumerable strings to concatenate</param>
		/// <param name="keySelector">Key selector</param>
		/// <param name="separator">optional separator, defaults to ","</param>
		/// <returns>Concatenated string of list items or empty string if list is null</returns>
		public static string ToConcatenatedString<T>(this IEnumerable<T> list, Func<T, string> keySelector, string separator = ",")
		{
            return ToConcatenatedString(list?.Select(keySelector), separator);
		}


		/// <summary>
		/// Returns all distinct elements of the given <paramref name="source"/>.
		/// </summary>
		/// <param name="source">Source items</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <returns>A sequence consisting of distinct elements from the source</returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.GroupBy(keySelector).Select(x => x.First());
		}

		/// <summary>
		/// Returns all distinct elements of the given <paramref name="source"/> with a custom <paramref name="comparer"/>.
		/// </summary>
		/// <param name="source">Source items</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
		/// <returns>A sequence consisting of distinct elements from the source</returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer)
		{
			return source.GroupBy(keySelector, comparer).Select(x => x.First());
		}

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

		public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
		{
			return collection == null || collection.None();
		}

		public static bool HasValues<T>(this IEnumerable<T> enumerable)
		{
			return !enumerable.IsNullOrEmpty();
		}

		public static bool ContainsAll<T>(this IEnumerable<T> collection, IEnumerable<T> otherCollection)
		{
			return collection != null && collection.ContainsAll(otherCollection.ToArray());
		}

		public static bool ContainsAll<T>(this IEnumerable<T> collection, params T[] otherCollection)
		{
			return collection != null && otherCollection.All(collection.Contains);
		}

		public static bool ContainsNone<T>(this IEnumerable<T> collection, IEnumerable<T> otherCollection)
		{
			return otherCollection == null || collection.ContainsNone(otherCollection.ToArray());
		}

		public static bool ContainsNone<T>(this IEnumerable<T> collection, params T[] otherCollection)
		{
			return collection != null && otherCollection.None(collection.Contains);
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		public static IEnumerable<T> GetDuplicates<T>(this IEnumerable<T> collection)
		{
			return collection.EmptyIfNull().GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key);
		}

		public static IEnumerable<IGrouping<TKey, T>> GetDuplicates<T, TKey>(this IEnumerable<T> collection,
			Func<T, TKey> keySelector)
		{
			return collection.EmptyIfNull().GroupBy(keySelector).Where(x => x.Count() > 1);
		}

        public static bool ContainsDuplicate<T>(this IEnumerable<T> items) where T : IEquatable<T>
        {
            var knownKeys = new HashSet<T>();
            return items.Any(item => !knownKeys.Add(item)); 
        }

        public static bool ContainsDuplicate<T>(this IEnumerable<T> items, IEqualityComparer<T> equalityComparer)
        {
            var knownKeys = new HashSet<T>(equalityComparer);
            return items.Any(item => !knownKeys.Add(item));
        }

		public static IEnumerable<List<T>> ChunkBy<T>(this IEnumerable<T> collection, int chunkSize)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var list = new List<T>(collection);
            for (var i = 0; i < list.Count; i += chunkSize)
            {
                yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
            }
        }

		/// <summary>
		/// Projects each element of a <paramref name="source"/> sequence into a element/index tuple, incorporating the element's index.
		/// </summary>
		/// <param name="source"></param>
		/// <typeparam name="T"></typeparam>
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
		{
			return source.Select((item, index) => (item, index));
		}

		/// <summary>
		/// Performs a Full Group Join between the <paramref name="first"/> and <paramref name="second"/> sequences.
		/// </summary>
		/// <remarks>
		/// This operator uses deferred execution and streams the results.
		/// </remarks>
		/// <typeparam name="TFirst">The type of the elements in the first input sequence</typeparam>
		/// <typeparam name="TSecond">The type of the elements in the first input sequence</typeparam>
		/// <typeparam name="TKey">The type of the key to use to join</typeparam>
		/// <typeparam name="TResult">The type of the elements of the resulting sequence</typeparam>
		/// <param name="first">First sequence</param>
		/// <param name="second">Second sequence</param>
		/// <param name="firstKeySelector">The mapping from first sequence to key</param>
		/// <param name="secondKeySelector">The mapping from second sequence to key</param>
		/// <param name="resultSelector">Function to apply to each pair of elements plus the key</param>
		/// <param name="comparer">
		/// The equality comparer to use to determine whether or not keys are equal.
		/// If null, the default equality comparer for <c>TKey</c> is used.
		/// </param>
		/// <returns>A sequence of elements joined from <paramref name="first"/> and <paramref name="second"/>. </returns>
		public static IEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
			this IEnumerable<TFirst> first,
			IEnumerable<TSecond> second,
			Func<TFirst, TKey> firstKeySelector,
			Func<TSecond, TKey> secondKeySelector,
			Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
			IEqualityComparer<TKey> comparer = null)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));
			if (firstKeySelector == null) throw new ArgumentNullException(nameof(firstKeySelector));
			if (secondKeySelector == null) throw new ArgumentNullException(nameof(secondKeySelector));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
			comparer ??= EqualityComparer<TKey>.Default;

			var firstLookup = first.ToLookup(firstKeySelector, comparer);
			var secondLookup = second.ToLookup(secondKeySelector, comparer);
			var keys = firstLookup.Select(p => p.Key).Union(secondLookup.Select(p => p.Key), comparer);

			var join = keys.Select(key => resultSelector(key, firstLookup[key], secondLookup[key]));

			foreach (var item in join)
			{
				yield return item;
			}
		}
	}
}