//
// Gendarme.Rules.Naming.UsePluralNameInEnumFlagsRule class
//
// Authors:
//	Néstor Salceda <nestor.salceda@gmail.com>
//
// 	(C) 2007 Néstor Salceda
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Globalization;

using Mono.Cecil;

using Gendarme.Framework;
using Gendarme.Framework.Rocks;

namespace Gendarme.Rules.Naming {

	[Problem ("This type is an enumeration and, by convention, enums should have a singular name.")]
	[Solution ("Convert this enumeration type name from plural to singular.")]
	public class UsePluralNameInEnumFlagsRule : Rule, ITypeRule {

		private static bool IsPlural (string typeName)
		{
			return String.Compare (typeName, typeName.Length - 1, "s", 0, 1, true, CultureInfo.CurrentCulture) == 0;
		}

		public RuleResult CheckType (TypeDefinition type)
		{
			// rule applies only to enums with [Flags] attribute
			if (!type.IsFlags ())
				return RuleResult.DoesNotApply;

			// rule applies

			if (IsPlural (type.Name))
				return RuleResult.Success;

			// Confidence == Normal because valid names may end with 's'
			Runner.Report (type, Severity.Low, Confidence.Normal, String.Empty);
			return RuleResult.Failure;
		}
	}
}
