using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Nextended.Core.Helper
{
	/// <summary>
	///     EncodingDetector
	/// </summary>
	public static class EncodingDetector
	{
		private const long _defaultHeuristicSampleSize = 0x10000;
		//completely arbitrary - inappropriate for high numbers of files / high speed requirements

		/// <summary>
		///     Detects Encoding for filename
		/// </summary>
		public static Encoding DetectTextFileEncoding(string inputFilename)
		{
			using (FileStream textfileStream = File.OpenRead(inputFilename))
			{
				return DetectTextFileEncoding(textfileStream, _defaultHeuristicSampleSize);
			}
		}

		/// <summary>
		///     Detects Encoding for file stream
		/// </summary>
		public static Encoding DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize)
		{
			bool uselessBool;
			return DetectTextFileEncoding(inputFileStream, _defaultHeuristicSampleSize, out uselessBool);
		}

		/// <summary>
		///     Detects Encoding for file stream
		/// </summary>
		public static Encoding DetectTextFileEncoding(FileStream inputFileStream, long heuristicSampleSize, out bool hasBom)
		{
			if (inputFileStream == null)
				throw new ArgumentNullException("Must provide a valid Filestream!", "InputFileStream");

			if (!inputFileStream.CanRead)
				throw new ArgumentException("Provided file stream is not readable!", "InputFileStream");

			if (!inputFileStream.CanSeek)
				throw new ArgumentException("Provided file stream cannot seek!", "InputFileStream");

			long originalPos = inputFileStream.Position;

			inputFileStream.Position = 0;


			//First read only what we need for BOM detection
			var bomBytes = new byte[inputFileStream.Length > 4 ? 4 : inputFileStream.Length];
			inputFileStream.Read(bomBytes, 0, bomBytes.Length);

			Encoding encodingFound = DetectBOMBytes(bomBytes);

			if (encodingFound != null)
			{
				inputFileStream.Position = originalPos;
				hasBom = true;
				return encodingFound;
			}


			//BOM Detection failed, going for heuristics now.
			//  create sample byte array and populate it
			var sampleBytes =
				new byte[heuristicSampleSize > inputFileStream.Length ? inputFileStream.Length : heuristicSampleSize];
			Array.Copy(bomBytes, sampleBytes, bomBytes.Length);
			if (inputFileStream.Length > bomBytes.Length)
				inputFileStream.Read(sampleBytes, bomBytes.Length, sampleBytes.Length - bomBytes.Length);
			inputFileStream.Position = originalPos;

			//test byte array content
			encodingFound = DetectUnicodeInByteSampleByHeuristics(sampleBytes);

			hasBom = false;
			return encodingFound;
		}

		/// <summary>
		///     Detects Encoding for byte array
		/// </summary>
		public static Encoding DetectTextByteArrayEncoding(byte[] textData)
		{
			bool uselessBool;
			return DetectTextByteArrayEncoding(textData, out uselessBool);
		}

		/// <summary>
		///     Detects Encoding for byte array with bom info
		/// </summary>
		public static Encoding DetectTextByteArrayEncoding(byte[] textData, out bool hasBom)
		{
			if (textData == null)
				throw new ArgumentNullException("Must provide a valid text data byte array!", "TextData");

			Encoding encodingFound = DetectBOMBytes(textData);

			if (encodingFound != null)
			{
				hasBom = true;
				return encodingFound;
			}
			//test byte array content
			encodingFound = DetectUnicodeInByteSampleByHeuristics(textData);

			hasBom = false;
			return encodingFound;
		}

		/// <summary>
		///     /// string aus einem byte array im richtigem encoding liedern
		/// </summary>
		public static string GetStringFromByteArray(byte[] textData, Encoding defaultEncoding)
		{
			return GetStringFromByteArray(textData, defaultEncoding, _defaultHeuristicSampleSize);
		}

		/// <summary>
		///     string aus einem byte array im richtigem encoding liedern
		/// </summary>
		public static string GetStringFromByteArray(byte[] textData, Encoding defaultEncoding, long maxHeuristicSampleSize)
		{
			if (textData == null)
				throw new ArgumentNullException("Must provide a valid text data byte array!", "TextData");

			Encoding encodingFound = DetectBOMBytes(textData);

			if (encodingFound != null)
			{
				//For some reason, the default encodings don't detect/swallow their own preambles!!
				return encodingFound.GetString(textData, encodingFound.GetPreamble().Length,
					textData.Length - encodingFound.GetPreamble().Length);
			}
			if (textData.Length > maxHeuristicSampleSize)
			{
				byte[] heuristicSample = new byte[maxHeuristicSampleSize];
				Array.Copy(textData, heuristicSample, maxHeuristicSampleSize);
			}

			encodingFound = DetectUnicodeInByteSampleByHeuristics(textData) ?? defaultEncoding;
			return encodingFound.GetString(textData);
		}

		/// <summary>
		///     Get BOM Bytes
		/// </summary>
		public static Encoding DetectBOMBytes(byte[] bomBytes)
		{
			if (bomBytes == null)
				throw new ArgumentNullException("Must provide a valid BOM byte array!", "BOMBytes");

			if (bomBytes.Length < 2)
				return null;

			if (bomBytes[0] == 0xff
				&& bomBytes[1] == 0xfe
				&& (bomBytes.Length < 4
					|| bomBytes[2] != 0
					|| bomBytes[3] != 0
					)
				)
				return Encoding.Unicode;

			if (bomBytes[0] == 0xfe
				&& bomBytes[1] == 0xff
				)
				return Encoding.BigEndianUnicode;

			if (bomBytes.Length < 3)
				return null;

			if (bomBytes[0] == 0xef && bomBytes[1] == 0xbb && bomBytes[2] == 0xbf)
				return Encoding.UTF8;

			if (bomBytes[0] == 0x2b && bomBytes[1] == 0x2f && bomBytes[2] == 0x76)
				return Encoding.UTF7;

			if (bomBytes.Length < 4)
				return null;

			if (bomBytes[0] == 0xff && bomBytes[1] == 0xfe && bomBytes[2] == 0 && bomBytes[3] == 0)
				return Encoding.UTF32;

			if (bomBytes[0] == 0 && bomBytes[1] == 0 && bomBytes[2] == 0xfe && bomBytes[3] == 0xff)
				return Encoding.GetEncoding(12001);

			return null;
		}

		/// <summary>
		///     Detects the unicode in byte sample by heuristics.
		/// </summary>
		public static Encoding DetectUnicodeInByteSampleByHeuristics(byte[] sampleBytes)
		{
			long oddBinaryNullsInSample = 0;
			long evenBinaryNullsInSample = 0;
			long suspiciousUTF8SequenceCount = 0;
			long suspiciousUTF8BytesTotal = 0;
			long likelyUSASCIIBytesInSample = 0;

			//Cycle through, keeping count of binary null positions, possible UTF-8 
			//  sequences from upper ranges of Windows-1252, and probable US-ASCII 
			//  character counts.

			long currentPos = 0;
			int skipUTF8Bytes = 0;

			while (currentPos < sampleBytes.Length)
			{
				//binary null distribution
				if (sampleBytes[currentPos] == 0)
				{
					if (currentPos % 2 == 0)
						evenBinaryNullsInSample++;
					else
						oddBinaryNullsInSample++;
				}

				//likely US-ASCII characters
				if (IsCommonUSASCIIByte(sampleBytes[currentPos]))
					likelyUSASCIIBytesInSample++;

				//suspicious sequences (look like UTF-8)
				if (skipUTF8Bytes == 0)
				{
					int lengthFound = DetectSuspiciousUTF8SequenceLength(sampleBytes, currentPos);

					if (lengthFound > 0)
					{
						suspiciousUTF8SequenceCount++;
						suspiciousUTF8BytesTotal += lengthFound;
						skipUTF8Bytes = lengthFound - 1;
					}
				}
				else
				{
					skipUTF8Bytes--;
				}

				currentPos++;
			}

			//1: UTF-16 LE - in english / european environments, this is usually characterized by a 
			//  high proportion of odd binary nulls (starting at 0), with (as this is text) a low 
			//  proportion of even binary nulls.
			//  The thresholds here used (less than 20% nulls where you expect non-nulls, and more than
			//  60% nulls where you do expect nulls) are completely arbitrary.

			if (((evenBinaryNullsInSample * 2.0) / sampleBytes.Length) < 0.2
				&& ((oddBinaryNullsInSample * 2.0) / sampleBytes.Length) > 0.6
				)
				return Encoding.Unicode;


			//2: UTF-16 BE - in english / european environments, this is usually characterized by a 
			//  high proportion of even binary nulls (starting at 0), with (as this is text) a low 
			//  proportion of odd binary nulls.
			//  The thresholds here used (less than 20% nulls where you expect non-nulls, and more than
			//  60% nulls where you do expect nulls) are completely arbitrary.

			if (((oddBinaryNullsInSample * 2.0) / sampleBytes.Length) < 0.2
				&& ((evenBinaryNullsInSample * 2.0) / sampleBytes.Length) > 0.6
				)
				return Encoding.BigEndianUnicode;


			//3: UTF-8 - Martin Dürst outlines a method for detecting whether something CAN be UTF-8 content 
			//  using regexp, in his w3c.org unicode FAQ entry: 
			//  http://www.w3.org/International/questions/qa-forms-utf-8
			//  adapted here for C#.
			string potentiallyMangledString = Encoding.ASCII.GetString(sampleBytes);
			var UTF8Validator = new Regex(@"\A("
										+ @"[\x09\x0A\x0D\x20-\x7E]"
										+ @"|[\xC2-\xDF][\x80-\xBF]"
										+ @"|\xE0[\xA0-\xBF][\x80-\xBF]"
										+ @"|[\xE1-\xEC\xEE\xEF][\x80-\xBF]{2}"
										+ @"|\xED[\x80-\x9F][\x80-\xBF]"
										+ @"|\xF0[\x90-\xBF][\x80-\xBF]{2}"
										+ @"|[\xF1-\xF3][\x80-\xBF]{3}"
										+ @"|\xF4[\x80-\x8F][\x80-\xBF]{2}"
										+ @")*\z");
            if (UTF8Validator.IsMatch(potentiallyMangledString))
			{
				//Unfortunately, just the fact that it CAN be UTF-8 doesn't tell you much about probabilities.
				//If all the characters are in the 0-127 range, no harm done, most western charsets are same as UTF-8 in these ranges.
				//If some of the characters were in the upper range (western accented characters), however, they would likely be mangled to 2-byte by the UTF-8 encoding process.
				// So, we need to play stats.

				// The "Random" likelihood of any pair of randomly generated characters being one 
				//   of these "suspicious" character sequences is:
				//     128 / (256 * 256) = 0.2%.
				//
				// In western text data, that is SIGNIFICANTLY reduced - most text data stays in the <127 
				//   character range, so we assume that more than 1 in 500,000 of these character 
				//   sequences indicates UTF-8. The number 500,000 is completely arbitrary - so sue me.
				//
				// We can only assume these character sequences will be rare if we ALSO assume that this
				//   IS in fact western text - in which case the bulk of the UTF-8 encoded data (that is 
				//   not already suspicious sequences) should be plain US-ASCII bytes. This, I 
				//   arbitrarily decided, should be 80% (a random distribution, eg binary data, would yield 
				//   approx 40%, so the chances of hitting this threshold by accident in random data are 
				//   VERY low). 

				if ((suspiciousUTF8SequenceCount * 500000.0 / sampleBytes.Length >= 1) //suspicious sequences
					&& (
					//all suspicious, so cannot evaluate proportion of US-Ascii
						sampleBytes.Length - suspiciousUTF8BytesTotal == 0
						||
						likelyUSASCIIBytesInSample * 1.0 / (sampleBytes.Length - suspiciousUTF8BytesTotal) >= 0.8
						)
					)
					return Encoding.UTF8;
			}

			return null;
		}

		private static bool IsCommonUSASCIIByte(byte testByte)
		{
			if (testByte == 0x0A //lf
				|| testByte == 0x0D //cr
				|| testByte == 0x09 //tab
				|| (testByte >= 0x20 && testByte <= 0x2F) //common punctuation
				|| (testByte >= 0x30 && testByte <= 0x39) //digits
				|| (testByte >= 0x3A && testByte <= 0x40) //common punctuation
				|| (testByte >= 0x41 && testByte <= 0x5A) //capital letters
				|| (testByte >= 0x5B && testByte <= 0x60) //common punctuation
				|| (testByte >= 0x61 && testByte <= 0x7A) //lowercase letters
				|| (testByte >= 0x7B && testByte <= 0x7E) //common punctuation
				)
				return true;
			return false;
		}

		private static int DetectSuspiciousUTF8SequenceLength(byte[] sampleBytes, long currentPos)
		{
			int lengthFound = 0;

			if (sampleBytes.Length >= currentPos + 1
				&& sampleBytes[currentPos] == 0xC2
				)
			{
				if (sampleBytes[currentPos + 1] == 0x81
					|| sampleBytes[currentPos + 1] == 0x8D
					|| sampleBytes[currentPos + 1] == 0x8F
					)
					lengthFound = 2;
				else if (sampleBytes[currentPos + 1] == 0x90
						|| sampleBytes[currentPos + 1] == 0x9D
					)
					lengthFound = 2;
				else if (sampleBytes[currentPos + 1] >= 0xA0
						&& sampleBytes[currentPos + 1] <= 0xBF
					)
					lengthFound = 2;
			}
			else if (sampleBytes.Length >= currentPos + 1
					&& sampleBytes[currentPos] == 0xC3
				)
			{
				if (sampleBytes[currentPos + 1] >= 0x80
					&& sampleBytes[currentPos + 1] <= 0xBF
					)
					lengthFound = 2;
			}
			else if (sampleBytes.Length >= currentPos + 1
					&& sampleBytes[currentPos] == 0xC5
				)
			{
				if (sampleBytes[currentPos + 1] == 0x92
					|| sampleBytes[currentPos + 1] == 0x93
					)
					lengthFound = 2;
				else if (sampleBytes[currentPos + 1] == 0xA0
						|| sampleBytes[currentPos + 1] == 0xA1
					)
					lengthFound = 2;
				else if (sampleBytes[currentPos + 1] == 0xB8
						|| sampleBytes[currentPos + 1] == 0xBD
						|| sampleBytes[currentPos + 1] == 0xBE
					)
					lengthFound = 2;
			}
			else if (sampleBytes.Length >= currentPos + 1
					&& sampleBytes[currentPos] == 0xC6
				)
			{
				if (sampleBytes[currentPos + 1] == 0x92)
					lengthFound = 2;
			}
			else if (sampleBytes.Length >= currentPos + 1
					&& sampleBytes[currentPos] == 0xCB
				)
			{
				if (sampleBytes[currentPos + 1] == 0x86
					|| sampleBytes[currentPos + 1] == 0x9C
					)
					lengthFound = 2;
			}
			else if (sampleBytes.Length >= currentPos + 2
					&& sampleBytes[currentPos] == 0xE2
				)
			{
				if (sampleBytes[currentPos + 1] == 0x80)
				{
					if (sampleBytes[currentPos + 2] == 0x93
						|| sampleBytes[currentPos + 2] == 0x94
						)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0x98
						|| sampleBytes[currentPos + 2] == 0x99
						|| sampleBytes[currentPos + 2] == 0x9A
						)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0x9C
						|| sampleBytes[currentPos + 2] == 0x9D
						|| sampleBytes[currentPos + 2] == 0x9E
						)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0xA0
						|| sampleBytes[currentPos + 2] == 0xA1
						|| sampleBytes[currentPos + 2] == 0xA2
						)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0xA6)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0xB0)
						lengthFound = 3;
					if (sampleBytes[currentPos + 2] == 0xB9
						|| sampleBytes[currentPos + 2] == 0xBA
						)
						lengthFound = 3;
				}
				else if (sampleBytes[currentPos + 1] == 0x82
						&& sampleBytes[currentPos + 2] == 0xAC
					)
					lengthFound = 3;
				else if (sampleBytes[currentPos + 1] == 0x84
						&& sampleBytes[currentPos + 2] == 0xA2
					)
					lengthFound = 3;
			}

			return lengthFound;
		}
	}
}