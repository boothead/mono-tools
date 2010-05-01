//
// Gendarme.Rules.Correctness.ProvideValidXPathExpressionRule class
//
// Authors:
//	Cedric Vivier <cedricv@neonux.com>
//
// Copyright (C) 2009 Cedric Vivier
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
using System.Xml;
using System.Xml.XPath;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;
using Gendarme.Framework.Engines;
using Gendarme.Framework.Helpers;

using System.Text.RegularExpressions;

namespace Gendarme.Rules.Correctness {

	/// <summary>
	/// This rule verifies that valid XPath expression strings are passed as arguments.
	/// </summary>
	/// <example>
	/// Bad example (node selection):
	/// <code>
	/// XmlNodeList nodes = document.SelectNodes ("/book[@npages == 100]/@title");
	/// </code>
	/// </example>
	/// <example>
	/// Good example (node selection):
	/// <code>
	/// XmlNodeList nodes = document.SelectNodes ("/book[@npages = 100]/@title");
	/// </code>
	/// </example>
	/// <example>
	/// Bad example (expression compilation):
	/// <code>
	/// var xpath = XPathExpression.Compile ("/book[@npages == 100]/@title");
	/// </code>
	/// </example>
	/// <example>
	/// Good example (expression compilation):
	/// <code>
	/// var xpath = XPathExpression.Compile ("/book[@npages = 100]/@title");
	/// </code>
	/// </example>
	/// <remarks>This rule is available since Gendarme 2.6</remarks>

	[Problem ("An invalid XPath expression string is provided to a method.")]
	[Solution ("Fix the invalid XPath expression.")]
	[EngineDependency (typeof (OpCodeEngine))]
	public sealed class ProvideValidXPathExpressionRule : Rule, IMethodRule {

		MethodDefinition method;

		const string XmlNodeClass = "System.Xml.XmlNode";
		const string XPathNavigatorClass = "System.Xml.XPath.XPathNavigator";
		const string XPathExpressionClass = "System.Xml.XPath.XPathExpression";

		public override void Initialize (IRunner runner)
		{
			base.Initialize (runner);

			Runner.AnalyzeModule += delegate (object o, RunnerEventArgs e) {
				foreach (AssemblyNameReference name in e.CurrentModule.AssemblyReferences) {
					if (name.Name == "System.Xml") {
						Active = true;
						return;
					}
				}
				Active = false; //no System.Xml assembly reference has been found
			};
		}

		void CheckString (Instruction ins, int argumentOffset)
		{
			Instruction ld = ins.TraceBack (method, argumentOffset);
			if (null == ld)
				return;

			switch (ld.OpCode.Code) {
			case Code.Ldstr:
				CheckString (ins, (string) ld.Operand);
				break;
			case Code.Ldsfld:
				FieldReference f = (FieldReference) ld.Operand;
				if (f.Name == "Empty" && f.DeclaringType.FullName == "System.String")
					CheckString (ins, null);
				break;
			case Code.Ldnull:
				CheckString (ins, null);
				break;
			}
		}

		void CheckString (Instruction ins, string expression)
		{
			if (string.IsNullOrEmpty (expression)) {
				Runner.Report (method, ins, Severity.High, Confidence.Total, "Expression is null or empty.");
				return;
			}

			try {
				XPathExpression.Compile (expression);
			} catch (XPathException e) {
				string msg = string.Format ("Expression '{0}' is invalid. Details: {1}", expression, e.Message);
				Runner.Report (method, ins, Severity.High, Confidence.High, msg);
			}
		}

		void CheckCall (Instruction ins, MethodReference mref)
		{
			if (null == mref || !mref.HasParameters)
				return;

			switch (mref.Name) {
			case "Compile":
				if (mref.DeclaringType.FullName == XPathExpressionClass
					|| mref.DeclaringType.Inherits (XPathNavigatorClass))
					CheckString (ins, GetFirstArgumentOffset (mref));
				break;
			case "SelectNodes":
				if (mref.DeclaringType.FullName == XmlNodeClass)
					CheckString (ins, -1);
				break;
			case "Evaluate":
			case "Select":
				CheckXPathNavigatorString (ins, mref);
				break;
			case "SelectSingleNode":
				CheckXPathNavigatorString (ins, mref);
				if (mref.DeclaringType.FullName == XmlNodeClass)
					CheckString (ins, -1);
				break;
			}
		}

		void CheckXPathNavigatorString (Instruction ins, MethodReference mref)
		{
			if (mref.Parameters [0].ParameterType.FullName == "System.String") {
				if (mref.DeclaringType.Inherits (XPathNavigatorClass))
					CheckString (ins, -1);
			}
		}

		public RuleResult CheckMethod (MethodDefinition method)
		{
			if (!method.HasBody)
				return RuleResult.DoesNotApply;

			this.method = method;

			//is there any interesting opcode in the method?
			if (!OpCodeBitmask.Calls.Intersect (OpCodeEngine.GetBitmask (method)))
				return RuleResult.DoesNotApply;

			foreach (Instruction ins in method.Body.Instructions) {
				if (!OpCodeBitmask.Calls.Get (ins.OpCode.Code))
					continue;

				CheckCall (ins, (MethodReference) ins.Operand);
			}

			return Runner.CurrentRuleResult;
		}

		static int GetFirstArgumentOffset (IMethodSignature mref)
		{
			return (mref.HasThis ? -1 : 0);
		}
	}
}
