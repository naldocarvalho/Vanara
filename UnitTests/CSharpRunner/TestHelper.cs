﻿using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.InteropServices;

namespace Vanara.PInvoke.Tests
{
	public static class TestHelper
	{
		public static IList<string> GetNestedStructSizes(this Type type, params string[] filters)
		{
			var output = new List<string>();
			var attr = System.Reflection.TypeAttributes.SequentialLayout | System.Reflection.TypeAttributes.ExplicitLayout;
			foreach (var t in typeof(AdvApi32).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).
				Where(t => t.IsValueType && !t.IsEnum && (t.Attributes & attr) != 0 && ((filters?.Length ?? 0) == 0 || filters.Any(s => t.Name.Contains(s)))))
				output.Add($"{t.Name} = {Marshal.SizeOf(t)}");
			output.Sort();
			return output;
		}

		public static void RunForEach<TEnum>(Type lib, string name, Func<TEnum, object[]> makeParam, Action<TEnum, object, object[]> action = null, Action<Exception> error = null) where TEnum : Enum =>
					RunForEach(lib, name, makeParam, (e, ex) => error?.Invoke(ex), action);

		public static void RunForEach<TEnum>(Type lib, string name, Func<TEnum, object[]> makeParam, Action<TEnum, Exception> error = null, Action<TEnum, object, object[]> action = null, CorrespondingAction? filter = null) where TEnum : Enum
		{
			var mi = lib.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Where(m => m.IsGenericMethod && m.Name == name).First();
			if (mi is null) throw new ArgumentException("Unable to find method.");
			foreach (var e in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
			{
				var type = (filter.HasValue ? CorrespondingTypeAttribute.GetCorrespondingTypes(e, filter.Value) : CorrespondingTypeAttribute.GetCorrespondingTypes(e)).FirstOrDefault();
				if (type is null)
				{
					TestContext.WriteLine($"No corresponding type found for {e}.");
					continue;
				}
				var gmi = mi.MakeGenericMethod(type);
				var param = makeParam(e);
				try
				{
					var ret = gmi.Invoke(null, param);
					action?.Invoke(e, ret, param);
				}
				catch (Exception ex)
				{
					error?.Invoke(e, ex.InnerException);
				}
			}
		}

		public static void WriteValues(this object value)
		{
			var json = JsonConvert.SerializeObject(value, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter(), new SizeTConverter());
			if (value != null) TestContext.WriteLine(value.GetType().Name);
			TestContext.WriteLine(json);
		}

		private class SizeTConverter : JsonConverter<SizeT>
		{
			public override SizeT ReadJson(JsonReader reader, Type objectType, SizeT existingValue, bool hasExistingValue, JsonSerializer serializer) => reader.Value is ulong ul ? new SizeT(ul) : new SizeT(0);

			public override void WriteJson(JsonWriter writer, SizeT value, JsonSerializer serializer) => writer.WriteValue(value.Value);
		}
	}
}