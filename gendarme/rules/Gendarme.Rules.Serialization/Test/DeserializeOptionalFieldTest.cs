// 
// Unit tests for DeserializeOptionalFieldRule
//
// Authors:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Reflection;
using System.Runtime.Serialization;

using Gendarme.Framework;
using Gendarme.Rules.Serialization;

using Mono.Cecil;
using NUnit.Framework;
using Test.Rules.Helpers;

namespace Test.Rules.Serialization {

	[Serializable]
	public class ClassWithOptionalFieldAndBothDeserializationAttributes {
		[OptionalField]
		private int optional = 1;

		[OnDeserialized]
		private void Deserialized (StreamingContext context)
		{
			optional = 0;
		}

		[OnDeserializing]
		private void OnDeserializing (StreamingContext context)
		{
			optional = 0;
		}
	}

	[Serializable]
	public class ClassWithOptionalFieldAndOnDeserializingAttributes {
		[OptionalField]
		private int optional = 1;

		[OnDeserializing]
		private void OnDeserializing (StreamingContext context)
		{
			optional = 0;
		}
	}

	[Serializable]
	public class ClassWithOptionalFieldAndOnDeserializedAttributes {
		[OptionalField]
		private int optional = 1;

		[OnDeserialized]
		private void OnDeserialized (StreamingContext context)
		{
			optional = 0;
		}
	}

	[Serializable]
	public class ClassWithOptionalField {
		[OptionalField]
		private int optional;
	}

	[Serializable]
	public class ClassWithoutOptionalField {
		private int optional;
	}

	// we should warn that the type is *not* [Serializable]
	public class NonSerializableClassWithOptionalField {
		[OptionalField]
		private int optional;
	}

	[TestFixture]
	public class DeserializeOptionalFieldTest {

		private ITypeRule rule;
		private TestRunner runner;
		private AssemblyDefinition assembly;

		[TestFixtureSetUp]
		public void FixtureSetUp ()
		{
			string unit = Assembly.GetExecutingAssembly ().Location;
			assembly = AssemblyFactory.GetAssembly (unit);
			rule = new DeserializeOptionalFieldRule ();
			runner = new TestRunner (rule);
		}

		private TypeDefinition GetTest (string name)
		{
			string fullname = "Test.Rules.Serialization." + name;
			return assembly.MainModule.Types [fullname];
		}

		[Test]
		public void Success ()
		{
			TypeDefinition type = GetTest ("ClassWithOptionalFieldAndBothDeserializationAttributes");
			Assert.AreEqual (RuleResult.Success, runner.CheckType (type), "ClassWithOptionalFieldAndBothDeserializationAttributes");
			Assert.AreEqual (0, runner.Defects.Count, "ClassWithOptionalFieldAndBothDeserializationAttributes-Count");

			type = GetTest ("ClassWithoutOptionalField");
			Assert.AreEqual (RuleResult.Success, runner.CheckType (type), "ClassWithoutOptionalField");
			Assert.AreEqual (0, runner.Defects.Count, "ClassWithoutOptionalField-Count");
		}

		[Test]
		public void Failure ()
		{
			TypeDefinition type = GetTest ("ClassWithOptionalField");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "ClassWithOptionalField");
			Assert.AreEqual (1, runner.Defects.Count, "ClassWithOptionalField-Count");

			type = GetTest ("ClassWithOptionalFieldAndOnDeserializedAttributes");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "ClassWithOptionalFieldAndOnDeserializedAttributes");
			Assert.AreEqual (1, runner.Defects.Count, "ClassWithOptionalFieldAndOnDeserializedAttributes-Count");

			type = GetTest ("ClassWithOptionalFieldAndOnDeserializingAttributes");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "ClassWithOptionalFieldAndOnDeserializingAttributes");
			Assert.AreEqual (1, runner.Defects.Count, "ClassWithOptionalFieldAndOnDeserializingAttributes-Count");

			type = GetTest ("NonSerializableClassWithOptionalField");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "NonSerializableClassWithOptionalField");
			Assert.AreEqual (1, runner.Defects.Count, "NonSerializableClassWithOptionalField-Count");
		}
	}
}
