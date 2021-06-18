using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;

namespace Nextended.Core.Extensions
{
    public static class SerializationHelper
	{

		public static T XmlDeserialize<T>(string filename)
		{
			T result;
			if (TryXmlDeserialize(filename, out result))
				return result;
			return default(T);
		}

		public static T XmlDeserialize<T>(Stream stream)
		{
			T result;
			if (TryXmlDeserialize(stream, out result))
				return result;
			return default(T);
		}

		public static bool TryXmlDeserialize<T>(string filename, out T result)
		{
			result = default(T);
			if (File.Exists(filename))
			{
				var fileStream = new FileStream(filename, FileMode.Open);
				try
				{
					return TryXmlDeserialize(fileStream, out result);
				}
				finally
				{
					fileStream.Close();
				}
			}
			return false;
		}

		public static bool TryXmlDeserialize<T>(Stream stream, out T result)
		{
			stream.Seek(0, SeekOrigin.Begin);
			var serializer = new XmlSerializer(typeof(T));
			result = (T)serializer.Deserialize(stream);
			return true;
		}

		public static bool TryBinaryDeserialize<T>(Stream stream, out T result)
		{
			stream.Seek(0, SeekOrigin.Begin);
			var serializer = new BinaryFormatter();
			result = (T)serializer.Deserialize(stream);
			return true;
		}

		public static Stream XmlSerialize<T>(this T content, Stream stream = null)
		{
			if (stream == null)
				stream = new MemoryStream();
			TryXmlSerialize(content, stream);
			return stream;
		}

		public static bool TryXmlSerialize<T>(this T content, Stream stream)
		{

			stream.Seek(0, SeekOrigin.Begin);
			var serializer = new XmlSerializer(typeof(T));
			serializer.Serialize(stream, content);
			stream.Position = 0;
			return true;
		}

		public static bool XmlSerialize<T>(this T content, string filename)
		{
			return TryXmlSerialize(content, filename);
		}

		public static bool TryXmlSerialize<T>(this T content, string filename)
		{
			string dir = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(dir))
			{
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				var fileStream = new FileStream(filename, FileMode.Create);
				try
				{
					if (TryXmlSerialize(content, fileStream))
						return File.Exists(filename);
				}
				finally
				{
					fileStream.Close();
				}
			}
			return false;
		}

		public static bool TryJsonSerialize<T>(this T obj, out string s) where T : class
		{
			try
			{
				s = obj.JsonSerialize();
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message);
				s = null;
				return false;
			}
			return !string.IsNullOrEmpty(s);
		}

		public static string JsonSerialize<T>(this T obj, string fileName = "") where T : class
		{
			var serializer = new DataContractJsonSerializer(typeof(T));
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, obj);
            string res = Encoding.UTF8.GetString(stream.ToArray());
            if (!string.IsNullOrEmpty(fileName))
            {
                File.WriteAllText(fileName, res);
            }
            return res;
        }

		public static T JsonDeserialize<T>(string json)
		{
			var obj = Activator.CreateInstance<T>();
			var ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
			var serializer = new DataContractJsonSerializer(obj.GetType());
			obj = (T)serializer.ReadObject(ms);
			ms.Close();
			ms.Dispose();
			return obj;
		}

		public static bool TryJsonDeserialize<T>(string json, out T obj)
			where T : class
		{
			try
			{
				obj = JsonDeserialize<T>(json);
				return obj != null;
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message);
				obj = default(T);
				return false;
			}
		}

		public static T BinaryDeserialize<T>(string filename)
		{
			T result;
			if (TryBinaryDeserialize(filename, out result))
				return result;
			return default(T);
		}


		public static bool TryBinaryDeserialize<T>(string filename, out T result)
		{
			result = default(T);
			if (File.Exists(filename))
			{
				var fileStream = new FileStream(filename, FileMode.Open);
				try
				{
					try
					{
						var serializer = new BinaryFormatter();
						result = (T)serializer.Deserialize(fileStream);
					}
					catch (Exception e)
					{
						Trace.TraceError(e.Message);
						return false;
					}
					return true;
				}
				finally
				{
					fileStream.Close();
				}
			}
			return false;
		}

		public static T BinaryDeserialize<T>(byte[] content)
        {
            return TryBinaryDeserialize(content, out T result) ? result : default;
        }


		public static bool TryBinaryDeserialize<T>(byte[] content, out T result)
		{
			result = default;

			var fileStream = new MemoryStream(content);
			try
			{
				var serializer = new BinaryFormatter();
				result = (T)serializer.Deserialize(fileStream);
				return true;
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static Stream BinarySerialize<T>(this T content, Stream stream = null)
		{
            TryBinarySerialize(content, stream ??= new MemoryStream());
			return stream;
		}

		public static bool BinarySerialize<T>(this T content, string filename)
		{
			return TryBinarySerialize(content, filename);
		}

		public static bool TryBinarySerialize<T>(this T content, Stream stream)
		{
			try
			{
				var serializer = new BinaryFormatter();
				serializer.Serialize(stream, content);
				stream.Position = 0;
				return true;
			}
			catch (Exception e)
			{
				Trace.TraceError(e.Message);
				return false;
			}
		}

		public static bool TryBinarySerialize<T>(this T content, string filename)
		{
			string dir = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(dir))
			{
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
				var fileStream = new FileStream(filename, FileMode.Create);
				try
				{
					if (TryBinarySerialize(content, fileStream))
						return File.Exists(filename);
				}
				finally
				{
					fileStream.Close();
				}
			}
			return false;
		}

	}
}