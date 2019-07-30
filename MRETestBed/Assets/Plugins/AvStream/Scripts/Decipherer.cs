using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AvStreamPlugin
{
    public static class Decipherer
    {
        private static List<Regex> entryRegexes = new List<Regex>
		{
            // This should be the catch-all regex that hunts for the cipher function itself.
            new Regex(@"\b([a-zA-Z0-9$]+)=function\(a\){a=a\.split\(""""\);[^}]+?;return a\.join\(""""\)}"),
            
            // These look for specific versions of the code that call the cipher function. They tend not to be as durable.
            new Regex(@"\bc\s*&&\s*d\.set\([^,]+,\s*[^(]+\(([a-zA-Z0-9$]+)\("),
            new Regex(@"\bc\s*&&\s*d\.set\([^,]+,\s*\([^)]*\)\s*\(\s*([a-zA-Z0-9$]+)\("),
			new Regex(@"\""signature"",\s?([a-zA-Z0-9\$]+)\("),
			new Regex(@"\.sig\s*\|\|([a-zA-Z0-9\$]+)\("),
        };

        enum OperationType
        {
            Reverse,
            Swap,
            Splice
        }

        private struct Operation
        {
            public OperationType Type;
            public int Index;
        }

        private static Dictionary<string, List<Operation>> cipherOperationCache = new Dictionary<string, List<Operation>>();

		private static object javascriptDictionaryLock = new object();

		public static string Decipher(string playerUrl, string cipheredSignature, bool log = false)
        {
            if (log) UnityEngine.Debug.LogFormat("Deciphering '{0}' from '{1}'", cipheredSignature, playerUrl);

            // Get the cipher operation list. This is a very heavy and repetitive operation. Make
            // sure we're caching the result.
            List<Operation> cipherOperationList;
			lock (javascriptDictionaryLock)
			{
				if (!cipherOperationCache.TryGetValue(playerUrl, out cipherOperationList))
				{
					string js = Http.DownloadString(playerUrl);

					string cipherFunctionName = FindCipherFunctionName(js);
					if (log) UnityEngine.Debug.LogFormat("Found cipher function: {0}", cipherFunctionName);

					string[] cipherFunctionLines = ExtractCipherFunctionLines(js, cipherFunctionName);
					if (log) UnityEngine.Debug.LogFormat("Found cipher function body: {0}", string.Join("; ", cipherFunctionLines));

					cipherOperationList = BuildCipherOperationList(js, cipherFunctionLines);

					cipherOperationCache.Add(playerUrl, cipherOperationList);
				}
			}

            if (log) UnityEngine.Debug.LogFormat("Found cipher operations: {0}", string.Join("; ", cipherOperationList.ConvertAll((o) => string.Format("{0}:{1}", o.Type, o.Index)).ToArray()));

            string decipheredSignature = ApplyCipherOperations(cipherOperationList, cipheredSignature);
            if (log) UnityEngine.Debug.LogFormat("Deciphered signature: {0}", decipheredSignature);

            return decipheredSignature;
        }

        private static string FindCipherFunctionName(string js)
        {
            // There are a few potential patterns for finding the entry function. Try them until we get something.
            for (int i = 0; i < entryRegexes.Count; ++i)
            {
                Match m = entryRegexes[i].Match(js);
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
            throw new Exception("Couldn't find cipher entry point name.");
        }

        private static string[] ExtractCipherFunctionLines(string js, string cipherFunctionName)
        {
            // Find the function and read out its body.
            string cipherBodyPattern = Regex.Escape(cipherFunctionName) + @"=function\(\w+\)\{(.*?)\}[,;}]";
            Match cipherMatch = Regex.Match(js, cipherBodyPattern, RegexOptions.Singleline);
            if (!cipherMatch.Success)
            {
                throw new Exception("Couldn't find the cipher function body.");
            }

            // Break out the individual lines of code in the function.
            string[] allCipherLines = cipherMatch.Groups[1].Value.Split(';');

            // The first and last lines are split/join. Drop these before returning.
            string[] cipherLines = new string[allCipherLines.Length - 2];
            Array.Copy(allCipherLines, 1, cipherLines, 0, cipherLines.Length);
            return cipherLines;
        }

        private static OperationType DetermineOperationType(string js, string functionName)
        {
            Match m = Regex.Match(js, Regex.Escape(functionName) + @":function\(\w+(,\w+)?\)\{(.*?)\}[,;}]");
            if (!m.Success)
            {
                throw new Exception(string.Format("Failed to find function named '{0}' in js.", functionName));
            }

            if (!m.Groups[1].Success)
            {
                // Reverse only takes one parameter.
                return OperationType.Reverse;
            }

            // Splice and Swap each require a second parameter.
            return m.Groups[2].Value.Contains("splice")
                ? OperationType.Splice
                : OperationType.Swap;
        }

        private static List<Operation> BuildCipherOperationList(string js, string[] cipherFunctionLines)
        {
            List<Operation> cipherOperations = new List<Operation>();
            Dictionary<string, OperationType> operationMap = new Dictionary<string, OperationType>();
            Regex lineMatchRegex = new Regex(@"[\w\d]+\.([\w\d]+)\([\w\d]+,(\d+)\)");
            for (int i = 0; i < cipherFunctionLines.Length; ++i)
            {
                Match m = lineMatchRegex.Match(cipherFunctionLines[i]);
                if (!m.Success)
                {
                    throw new Exception(string.Format("Unrecognized cipher line: {0}", cipherFunctionLines[i]));
                }

                int index = Convert.ToInt32(m.Groups[2].Value);

                OperationType type;
                string functionName = m.Groups[1].Value;
                if (!operationMap.TryGetValue(functionName, out type))
                {
                    type = DetermineOperationType(js, functionName);
                    operationMap.Add(functionName, type);
                }

                Operation o = new Operation();
                o.Index = index;
                o.Type = type;
                cipherOperations.Add(o);
            }

            // We expect to have found 1-3 distinct operations while processing the function lines. No duplicates should be encountered.
            if (operationMap.Values.Distinct().Count() != operationMap.Count || operationMap.Count > 3 || operationMap.Count == 0)
            {
                throw new Exception(string.Format("Unexpected cipher operation map: {0}", operationMap.ToString())); // operationMap.ToStringFull
            }

            return cipherOperations;
        }

        private static string ApplyCipherOperation(string cipher, Operation op)
        {
            switch (op.Type)
            {
                case OperationType.Reverse:
                    return new string(cipher.ToCharArray().Reverse().ToArray());

                case OperationType.Swap:
                    var builder = new StringBuilder(cipher);
                    builder[0] = cipher[op.Index];
                    builder[op.Index] = cipher[0];
                    return builder.ToString();

                case OperationType.Splice:
                    return cipher.Substring(op.Index);

                default:
                    throw new NotImplementedException("Couldn't find cipher operation.");
            }
        }

        private static string ApplyCipherOperations(List<Operation> cipherOperationList, string cipher)
        {
            return cipherOperationList.Aggregate(cipher, ApplyCipherOperation);
        }
    }
}
