namespace ProjectAFS.Core.Utility.Strings;

public static class StringExtension
{
	private sealed class TrieNode
	{
		public Dictionary<char, TrieNode>? Children;
		public string? Replacement;
	}

	private static TrieNode BuildTrie(IEnumerable<KeyValuePair<string, string>> replacements)
	{
		var root = new TrieNode();
		foreach ((string key, string value) in replacements)
		{
			if (string.IsNullOrEmpty(key)) continue;
			
			var node = root;
			foreach (char c in key)
			{
				node.Children ??= new Dictionary<char, TrieNode>(4);
				if (!node.Children.TryGetValue(c, out var next))
				{
					next = new TrieNode();
					node.Children[c] = next;
				}
				node = next;
			}
			node.Replacement = value;
		}
		return root;
	}
	
	extension(string source)
	{
		public string ReplaceAll(IEnumerable<string> oldValues, string newValue)
		{
			if (string.IsNullOrEmpty(source)) return source;
			ArgumentNullException.ThrowIfNull(oldValues);
			var dict = new Dictionary<string, string>();
			foreach (string key in oldValues)
			{
				if (!string.IsNullOrEmpty(key))
				{
					dict[key] = newValue;
				}
			}
			
			return source.ReplaceAll(dict);
		}
		
		public string ReplaceAll(IEnumerable<char> oldValues, char newValue)
		{
			if (string.IsNullOrEmpty(source)) return source;
			ArgumentNullException.ThrowIfNull(oldValues);
			var dict = new Dictionary<string, string>();
			foreach (char key in oldValues)
			{
				dict[key.ToString()] = newValue.ToString();
			}
			
			return source.ReplaceAll(dict);
		}

		public string ReplaceAll(params KeyValuePair<string, string>[] replacements)
		{
			if (string.IsNullOrEmpty(source) || replacements.Length == 0) return source;
			return source.ReplaceAll(replacements.ToDictionary(kv => kv.Key, kv => kv.Value));
		}
		
		public string ReplaceAll(params KeyValuePair<char, char>[] replacements)
		{
			if (string.IsNullOrEmpty(source) || replacements.Length == 0) return source;
			var dict = new Dictionary<string, string>();
			foreach (var kv in replacements)
			{
				dict[kv.Key.ToString()] = kv.Value.ToString();
			}
			return source.ReplaceAll(dict);
		}

		public string ReplaceAll(IReadOnlyDictionary<string, string> replacements)
		{
			if (string.IsNullOrEmpty(source) || replacements.Count == 0) return source;

			unsafe
			{
				var root = BuildTrie(replacements);
				int capacity = source.Length + 32;
				char[] buffer = new char[capacity];
				int pos = 0;

				fixed (char* src = source)
				{
					int i = 0;
					while (i < source.Length)
					{
						var node = root;
						int j = i;
						TrieNode? lastMatch = null;
						int lastMatchIndex = -1;
						while (j < source.Length && node.Children != null && node.Children.TryGetValue(src[j], out node))
						{
							j++;
							if (node.Replacement != null)
							{
								lastMatch = node;
								lastMatchIndex = j;
							}
						}

						if (lastMatch != null)
						{
							string repl = lastMatch.Replacement!;
							int needed = pos + repl.Length;
							if (needed > buffer.Length)
							{
								Array.Resize(ref buffer, Math.Max(needed, buffer.Length * 2));
							}
							
							repl.AsSpan().CopyTo(buffer.AsSpan(pos));
							pos += repl.Length;
							i = lastMatchIndex;
						}
						else
						{
							if (pos >= buffer.Length)
							{
								Array.Resize(ref buffer, buffer.Length * 2);
							}

							buffer[pos++] = src[i];
							i++;
						}
					}
				}
				
				return new string(buffer, 0, pos);
			}
		}
	}
}